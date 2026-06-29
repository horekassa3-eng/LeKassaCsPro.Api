using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RapportGlobalController(AppDbContext context) : ControllerBase
{
    private const string ServiceOrangeMoney = "Commission Orange Money Sénégal";
    private const string ServiceWave = "Commission Wave Sénégal";
    private const string ServiceCopieImpression = "Copie / Impression";

    [HttpGet("resume")]
    public async Task<ActionResult<RapportGlobalResponse>> GetResumeAsync(
        [FromQuery] DateTime? debut,
        [FromQuery] DateTime? fin)
    {
        var today = DateTime.UtcNow.Date;
        var dateDebut = NormaliserDateUtc(debut?.Date ?? new DateTime(today.Year, today.Month, 1));
        var dateFin = NormaliserDateUtc(fin?.Date ?? today);

        if (dateDebut > dateFin)
            return BadRequest("La date début ne peut pas être supérieure à la date fin.");

        var finExclusive = dateFin.AddDays(1);

        var transfertsPayes = await context.Transferts
            .Where(t => t.IsActive
                        && t.DateTransfert >= dateDebut
                        && t.DateTransfert < finExclusive)
            .Where(t => t.Statut == "Payé" || t.Statut == "Paye")
            .ToListAsync();

        var commissionsTransferts = transfertsPayes.Sum(t => t.BeneficeFcfa);

        var totalDepenses = await context.Depenses
            .Where(d => d.IsActive
                        && d.DateDepense >= dateDebut
                        && d.DateDepense < finExclusive)
            .SumAsync(d => d.Montant);

        var recettesServices = await context.RecettesServices
            .Where(r => r.IsActive
                        && r.DateRecette >= dateDebut
                        && r.DateRecette < finExclusive)
            .ToListAsync();

        var commissionOrangeMoney = recettesServices
            .Where(r => EstService(r.TypeService, ServiceOrangeMoney))
            .Sum(r => r.Montant);

        var commissionWave = recettesServices
            .Where(r => EstService(r.TypeService, ServiceWave))
            .Sum(r => r.Montant);

        var copieImpression = recettesServices
            .Where(r => EstService(r.TypeService, ServiceCopieImpression))
            .Sum(r => r.Montant);

        var beneficeVentes = await CalculerBeneficeVentesAsync(dateDebut, finExclusive);

        var beneficeGlobal =
            commissionsTransferts
            + commissionOrangeMoney
            + commissionWave
            + copieImpression
            + beneficeVentes
            - totalDepenses;

        return Ok(new RapportGlobalResponse
        {
            DateDebut = dateDebut,
            DateFin = dateFin,
            CommissionsTransferts = commissionsTransferts,
            CommissionOrangeMoney = commissionOrangeMoney,
            CommissionWave = commissionWave,
            CopieImpression = copieImpression,
            BeneficeVentes = beneficeVentes,
            TotalDepenses = totalDepenses,
            BeneficeGlobal = beneficeGlobal
        });
    }

    private async Task<decimal> CalculerBeneficeVentesAsync(DateTime debut, DateTime finExclusive)
    {
        var ventes = await context.Ventes
            .Where(v => v.IsActive
                        && v.DateVente >= debut
                        && v.DateVente < finExclusive)
            .ToListAsync();

        if (ventes.Count == 0)
            return 0m;

        var venteIds = ventes.Select(v => v.Id).ToList();

        var details = await context.VenteDetails
            .Where(d => d.IsActive && venteIds.Contains(d.VenteId))
            .ToListAsync();

        if (details.Count == 0)
            return ventes.Sum(v => v.MontantTotal);

        var produitIds = details
            .Select(d => d.ProduitVenteId)
            .Distinct()
            .ToList();

        var prixAchatParProduit = await context.ProduitsVente
            .Where(p => produitIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PrixAchat);

        var detailsParVente = details
            .GroupBy(d => d.VenteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        decimal benefice = 0m;

        foreach (var vente in ventes)
        {
            var lignes = detailsParVente.TryGetValue(vente.Id, out var liste)
                ? liste
                : new List<AppVenteDetail>();

            decimal coutAchat = 0m;

            foreach (var detail in lignes)
            {
                prixAchatParProduit.TryGetValue(detail.ProduitVenteId, out var prixAchat);
                coutAchat += prixAchat * detail.Quantite;
            }

            benefice += vente.MontantTotal - coutAchat;
        }

        return benefice;
    }

    private static bool EstService(string? valeur, string service)
    {
        return string.Equals(valeur?.Trim(), service, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }
}

public class RapportGlobalResponse
{
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public decimal CommissionsTransferts { get; set; }
    public decimal CommissionOrangeMoney { get; set; }
    public decimal CommissionWave { get; set; }
    public decimal CopieImpression { get; set; }
    public decimal BeneficeVentes { get; set; }
    public decimal TotalDepenses { get; set; }
    public decimal BeneficeGlobal { get; set; }
}

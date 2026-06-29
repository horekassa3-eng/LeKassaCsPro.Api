using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Dtos;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private const string DeviseFcfa = "FCFA";
    private const string DeviseGnf = "GNF";

    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("resume")]
    public async Task<ActionResult<DashboardResumeResponse>> GetResumeAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var transfertsPayesJour = await _context.Transferts
            .Where(t => t.IsActive
                        && t.DateTransfert >= today
                        && t.DateTransfert < tomorrow
                        && (t.Statut == "Payé" || t.Statut == "Paye"))
            .ToListAsync();

        var totalEnvoyeJour =
            transfertsPayesJour
                .Where(t => EstSensSenegalVersGuinee(t.SensTransfert))
                .Sum(GetMontantEnvoyeFcfa)
            +
            transfertsPayesJour
                .Where(t => EstSensGuineeVersSenegal(t.SensTransfert))
                .Sum(t => t.MontantFcfaBeneficiaire);

        var soldeFcfa = await GetSoldeFournisseursTotalAsync(DeviseFcfa);
        var soldeGnf = await CalculerSoldeGnfDashboardAsync(soldeFcfa);

        return Ok(new DashboardResumeResponse
        {
            SoldeFcfa = soldeFcfa,
            SoldeGnf = soldeGnf,
            TransfertsJour = transfertsPayesJour.Count,
            TotalEnvoyeJourFcfa = totalEnvoyeJour
        });
    }

    private async Task<decimal> CalculerSoldeGnfDashboardAsync(decimal soldeFcfa)
    {
        var soldeGnfDirect = await GetSoldeFournisseursTotalAsync(DeviseGnf);

        if (soldeGnfDirect > 0)
            return soldeGnfDirect;

        var tauxActif = await _context.TauxChanges
            .Where(t => t.IsActive && t.IsActif && t.TauxGnfParFcfa > 0)
            .OrderByDescending(t => t.DateTaux)
            .FirstOrDefaultAsync();

        if (tauxActif == null || soldeFcfa <= 0)
            return 0m;

        return soldeFcfa * tauxActif.TauxGnfParFcfa;
    }

    private async Task<decimal> GetSoldeFournisseursTotalAsync(string devise)
    {
        var mouvements = await _context.FournisseurMouvements
            .Where(m => m.IsActive && m.Devise == devise)
            .ToListAsync();

        var entrees = mouvements
            .Where(m => EstEntreeSoldeFournisseur(m.TypeMouvement))
            .Sum(m => m.Montant);

        var sorties = mouvements
            .Where(m => EstSortieSoldeFournisseur(m.TypeMouvement))
            .Sum(m => m.Montant);

        return entrees - sorties;
    }

    private static decimal GetMontantEnvoyeFcfa(AppTransfert transfert)
    {
        if (transfert.FcfaPayeClient > 0)
            return transfert.FcfaPayeClient;

        if (transfert.TotalAPayerFcfa > 0)
            return transfert.TotalAPayerFcfa;

        if (transfert.FcfaDonneFournisseur > 0)
            return transfert.FcfaDonneFournisseur;

        return 0m;
    }

    private static bool EstEntreeSoldeFournisseur(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Approvisionnement", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Entree client Guinee", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Entrée client Guinée", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Réception", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Reception", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Retrait", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Retrait code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSortieSoldeFournisseur(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Utilisation transfert", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSensSenegalVersGuinee(string? sens)
    {
        if (string.IsNullOrWhiteSpace(sens))
            return false;

        return sens.Contains("Sénégal vers Guinée", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("Senegal vers Guinee", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("SN", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSensGuineeVersSenegal(string? sens)
    {
        if (string.IsNullOrWhiteSpace(sens))
            return false;

        return sens.Contains("Guinée vers Sénégal", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("Guinee vers Senegal", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("GN", StringComparison.OrdinalIgnoreCase);
    }
}

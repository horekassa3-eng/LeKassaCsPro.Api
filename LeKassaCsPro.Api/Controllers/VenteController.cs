using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenteController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppVente>>> GetAsync()
    {
        var ventes = await context.Ventes
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.DateVente)
            .ToListAsync();

        return Ok(ventes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppVente>> GetByIdAsync(int id)
    {
        var vente = await context.Ventes
            .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

        if (vente == null)
            return NotFound();

        return Ok(vente);
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<List<AppVenteDetail>>> GetDetailsAsync(int id)
    {
        var venteExiste = await context.Ventes
            .AnyAsync(v => v.Id == id && v.IsActive);

        if (!venteExiste)
            return NotFound();

        var details = await context.VenteDetails
            .Where(d => d.IsActive && d.VenteId == id)
            .OrderBy(d => d.Id)
            .ToListAsync();

        return Ok(details);
    }

    [HttpPost("avec-details")]
    public async Task<ActionResult<AppVente>> SaveAvecDetailsAsync([FromBody] VenteAvecDetailsRequest request)
    {
        if (request.Details.Count == 0)
            return BadRequest("Ajoutez au moins un produit dans la vente.");

        request.Vente.Id = 0;
        request.Vente.DateVente = NormaliserDateUtc(request.Vente.DateVente);
        request.Vente.DateCreation = DateTime.UtcNow;
        request.Vente.DateModification = DateTime.UtcNow;
        request.Vente.IsActive = true;

        var details = request.Details
            .Where(d => d.ProduitVenteId > 0 && d.Quantite > 0 && d.PrixUnitaire > 0)
            .ToList();

        if (details.Count == 0)
            return BadRequest("Les détails de vente sont invalides.");

        foreach (var groupe in details.GroupBy(d => d.ProduitVenteId))
        {
            var stock = await GetStockProduitAsync(groupe.Key);
            var quantiteDemandee = groupe.Sum(d => d.Quantite);

            if (quantiteDemandee > stock)
            {
                var produit = await context.ProduitsVente
                    .FirstOrDefaultAsync(p => p.Id == groupe.Key);

                var nomProduit = produit?.Nom ?? $"Produit #{groupe.Key}";
                return BadRequest($"Stock insuffisant pour {nomProduit}. Stock disponible : {stock:N0}.");
            }
        }

        var totalBrut = details.Sum(d => d.MontantTotal > 0 ? d.MontantTotal : d.Quantite * d.PrixUnitaire);
        request.Vente.MontantTotal = Math.Max(0m, totalBrut - request.Vente.Remise);

        if (request.Vente.MontantPaye <= 0)
            request.Vente.MontantPaye = request.Vente.MontantTotal;

        request.Vente.StatutPaiement = GetStatutPaiement(request.Vente.MontantTotal, request.Vente.MontantPaye);

        await using var transaction = await context.Database.BeginTransactionAsync();

        context.Ventes.Add(request.Vente);
        await context.SaveChangesAsync();

        foreach (var detail in details)
        {
            detail.Id = 0;
            detail.VenteId = request.Vente.Id;
            detail.MontantTotal = detail.Quantite * detail.PrixUnitaire;
            detail.DateCreation = DateTime.UtcNow;
            detail.DateModification = DateTime.UtcNow;
            detail.IsActive = true;
            detail.UtilisateurId = request.Vente.UtilisateurId;
            detail.UtilisateurNom = request.Vente.UtilisateurNom;
            detail.RoleUtilisateur = request.Vente.RoleUtilisateur;

            context.VenteDetails.Add(detail);

            context.StockMouvements.Add(new AppStockMouvement
            {
                ProduitVenteId = detail.ProduitVenteId,
                DateMouvement = request.Vente.DateVente,
                TypeMouvement = "Sortie",
                Quantite = detail.Quantite,
                PrixUnitaire = detail.PrixUnitaire,
                MontantTotal = detail.MontantTotal,
                Motif = "Vente",
                Observation = $"Sortie automatique vente #{request.Vente.Id}",
                SourceModule = "Vente",
                SourceId = request.Vente.Id,
                IsAutomatique = true,
                IsActive = true,
                UtilisateurId = request.Vente.UtilisateurId,
                UtilisateurNom = request.Vente.UtilisateurNom,
                RoleUtilisateur = request.Vente.RoleUtilisateur,
                DateCreation = DateTime.UtcNow,
                DateModification = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(request.Vente);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppVente>> UpdateAsync(int id, [FromBody] AppVente request)
    {
        var vente = await context.Ventes
            .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

        if (vente == null)
            return NotFound();

        vente.ClientId = request.ClientId;
        vente.NomClient = request.NomClient;
        vente.TelephoneClient = request.TelephoneClient;
        vente.ModePaiement = request.ModePaiement;
        vente.MontantTotal = request.MontantTotal;
        vente.MontantPaye = request.MontantPaye;
        vente.Remise = request.Remise;
        vente.Observation = request.Observation;
        vente.StatutPaiement = GetStatutPaiement(vente.MontantTotal, vente.MontantPaye);
        vente.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(vente);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var vente = await context.Ventes
            .FirstOrDefaultAsync(v => v.Id == id && v.IsActive);

        if (vente == null)
            return NotFound();

        await using var transaction = await context.Database.BeginTransactionAsync();

        vente.IsActive = false;
        vente.StatutPaiement = "Annulé";
        vente.DateModification = DateTime.UtcNow;

        var details = await context.VenteDetails
            .Where(d => d.IsActive && d.VenteId == id)
            .ToListAsync();

        foreach (var detail in details)
        {
            detail.IsActive = false;
            detail.DateModification = DateTime.UtcNow;
        }

        var mouvementsStock = await context.StockMouvements
            .Where(m => m.IsActive
                        && m.SourceModule == "Vente"
                        && m.SourceId == id)
            .ToListAsync();

        foreach (var mouvement in mouvementsStock)
        {
            mouvement.IsActive = false;
            mouvement.DateModification = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    private async Task<decimal> GetStockProduitAsync(int produitVenteId)
    {
        var mouvements = await context.StockMouvements
            .Where(m => m.IsActive && m.ProduitVenteId == produitVenteId)
            .ToListAsync();

        var entrees = mouvements
            .Where(m => EstEntreeStock(m.TypeMouvement))
            .Sum(m => m.Quantite);

        var sorties = mouvements
            .Where(m => EstSortieStock(m.TypeMouvement))
            .Sum(m => m.Quantite);

        return entrees - sorties;
    }

    private static bool EstEntreeStock(string? type)
    {
        return string.Equals(type, "Entree", StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "Entrée", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSortieStock(string? type)
    {
        return string.Equals(type, "Sortie", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStatutPaiement(decimal total, decimal montantPaye)
    {
        if (montantPaye <= 0)
            return "Impayé";

        if (montantPaye < total)
            return "Partiel";

        return "Payé";
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

public class VenteAvecDetailsRequest
{
    public AppVente Vente { get; set; } = new();
    public List<AppVenteDetail> Details { get; set; } = new();
}

using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockMouvementController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppStockMouvement>>> GetAllAsync()
    {
        var mouvements = await context.StockMouvements
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpPost]
    public async Task<ActionResult<AppStockMouvement>> SaveAsync([FromBody] AppStockMouvement request)
    {
        if (request.ProduitVenteId <= 0)
            return BadRequest("Le produit est obligatoire.");

        if (request.Quantite <= 0)
            return BadRequest("La quantité est obligatoire.");

        if (request.PrixUnitaire <= 0)
            return BadRequest("Le prix unitaire est obligatoire.");

        if (request.IsAutomatique)
            return BadRequest("Ce mouvement automatique ne peut pas être modifié ici.");

        request.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        request.MontantTotal = request.Quantite * request.PrixUnitaire;
        request.SourceModule = "Manuel";
        request.SourceId = 0;
        request.IsAutomatique = false;
        request.IsActive = true;

        var stockApres = await CalculerStockApresModificationAsync(
            request.Id,
            request.ProduitVenteId,
            request.TypeMouvement,
            request.Quantite);

        if (stockApres < 0)
            return BadRequest($"Cette modification rendrait le stock négatif. Stock après modification : {stockApres:N0}.");

        if (request.Id == 0)
        {
            request.DateCreation = DateTime.UtcNow;
            request.DateModification = DateTime.UtcNow;

            context.StockMouvements.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var mouvement = await context.StockMouvements
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        if (mouvement.IsAutomatique)
            return BadRequest("Ce mouvement vient d'une vente et ne peut pas être modifié ici.");

        mouvement.ProduitVenteId = request.ProduitVenteId;
        mouvement.DateMouvement = request.DateMouvement;
        mouvement.TypeMouvement = request.TypeMouvement;
        mouvement.Quantite = request.Quantite;
        mouvement.PrixUnitaire = request.PrixUnitaire;
        mouvement.MontantTotal = request.MontantTotal;
        mouvement.Motif = request.Motif;
        mouvement.Observation = request.Observation;
        mouvement.SourceModule = "Manuel";
        mouvement.SourceId = 0;
        mouvement.IsAutomatique = false;
        mouvement.IsActive = true;
        mouvement.UtilisateurId = request.UtilisateurId;
        mouvement.UtilisateurNom = request.UtilisateurNom;
        mouvement.RoleUtilisateur = request.RoleUtilisateur;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(mouvement);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var mouvement = await context.StockMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        if (mouvement.IsAutomatique)
            return BadRequest("Ce mouvement vient d'une vente. Supprimez la vente pour annuler cette sortie.");

        var stockApres = await CalculerStockApresSuppressionAsync(mouvement);

        if (stockApres < 0)
            return BadRequest($"Impossible de supprimer ce mouvement. Le stock deviendrait négatif : {stockApres:N0}.");

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<decimal> CalculerStockApresModificationAsync(
        int mouvementId,
        int nouveauProduitId,
        string nouveauType,
        decimal nouvelleQuantite)
    {
        var stockNouveauProduit = await GetStockProduitAsync(nouveauProduitId);

        if (mouvementId == 0)
        {
            return EstEntree(nouveauType)
                ? stockNouveauProduit + nouvelleQuantite
                : stockNouveauProduit - nouvelleQuantite;
        }

        var mouvementActuel = await context.StockMouvements
            .FirstOrDefaultAsync(m => m.Id == mouvementId && m.IsActive);

        if (mouvementActuel == null)
            return stockNouveauProduit;

        if (mouvementActuel.ProduitVenteId == nouveauProduitId)
        {
            stockNouveauProduit -= EstEntree(mouvementActuel.TypeMouvement)
                ? mouvementActuel.Quantite
                : -mouvementActuel.Quantite;

            stockNouveauProduit += EstEntree(nouveauType)
                ? nouvelleQuantite
                : -nouvelleQuantite;

            return stockNouveauProduit;
        }

        var stockAncienProduit = await CalculerStockApresSuppressionAsync(mouvementActuel);

        if (stockAncienProduit < 0)
            return stockAncienProduit;

        return EstEntree(nouveauType)
            ? stockNouveauProduit + nouvelleQuantite
            : stockNouveauProduit - nouvelleQuantite;
    }

    private async Task<decimal> CalculerStockApresSuppressionAsync(AppStockMouvement mouvement)
    {
        var stock = await GetStockProduitAsync(mouvement.ProduitVenteId);

        return EstEntree(mouvement.TypeMouvement)
            ? stock - mouvement.Quantite
            : stock + mouvement.Quantite;
    }

    private async Task<decimal> GetStockProduitAsync(int produitVenteId)
    {
        var mouvements = await context.StockMouvements
            .Where(m => m.IsActive && m.ProduitVenteId == produitVenteId)
            .ToListAsync();

        var entrees = mouvements
            .Where(m => EstEntree(m.TypeMouvement))
            .Sum(m => m.Quantite);

        var sorties = mouvements
            .Where(m => EstSortie(m.TypeMouvement))
            .Sum(m => m.Quantite);

        return entrees - sorties;
    }

    private static bool EstEntree(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Entrée", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Entree", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSortie(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Sortie", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime NormaliserDateUtc(DateTime? date)
    {
        if (date == null || date == default)
            return DateTime.UtcNow;

        return DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
    }
}

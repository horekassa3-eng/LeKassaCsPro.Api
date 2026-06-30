using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProduitVenteController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppProduitVente>>> GetAllAsync()
    {
        var produits = await context.ProduitsVente
            .Where(p => p.IsActive)
            .OrderBy(p => p.Nom)
            .ToListAsync();

        return Ok(produits);
    }

    [HttpGet("{id:int}/stock")]
    public async Task<ActionResult<decimal>> GetStockAsync(int id)
    {
        var stock = await GetStockProduitAsync(id);
        return Ok(stock);
    }

    [HttpPost]
    public async Task<ActionResult<AppProduitVente>> SaveAsync([FromBody] AppProduitVente produit)
    {
        produit.Id = 0;
        produit.Nom = produit.Nom?.Trim() ?? string.Empty;
        produit.Categorie = produit.Categorie?.Trim() ?? string.Empty;
        produit.CodeProduit = produit.CodeProduit?.Trim() ?? string.Empty;
        produit.Unite = string.IsNullOrWhiteSpace(produit.Unite)
            ? "Pièce"
            : produit.Unite.Trim();

        // IMPORTANT : enregistrer image nouveau produit
        produit.ImageBase64 = produit.ImageBase64 ?? string.Empty;

        produit.DateCreation = DateTime.UtcNow;
        produit.DateModification = DateTime.UtcNow;
        produit.IsActive = true;

        context.ProduitsVente.Add(produit);
        await context.SaveChangesAsync();

        return Ok(produit);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppProduitVente>> UpdateAsync(int id, [FromBody] AppProduitVente request)
    {
        var produit = await context.ProduitsVente
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (produit == null)
            return NotFound();

        produit.Nom = request.Nom?.Trim() ?? string.Empty;
        produit.Categorie = request.Categorie?.Trim() ?? string.Empty;
        produit.CodeProduit = request.CodeProduit?.Trim() ?? string.Empty;
        produit.Unite = string.IsNullOrWhiteSpace(request.Unite)
            ? "Pièce"
            : request.Unite.Trim();

        produit.PrixAchat = request.PrixAchat;
        produit.PrixVente = request.PrixVente;
        produit.StockAlerte = request.StockAlerte;

        // IMPORTANT : correction changement image après modification
        produit.ImageBase64 = request.ImageBase64 ?? string.Empty;

        produit.UtilisateurId = request.UtilisateurId;
        produit.UtilisateurNom = request.UtilisateurNom;
        produit.RoleUtilisateur = request.RoleUtilisateur;
        produit.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(produit);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var produit = await context.ProduitsVente
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (produit == null)
            return NotFound();

        var produitUtilise = await context.VenteDetails
            .AnyAsync(d => d.IsActive && d.ProduitVenteId == id);

        if (produitUtilise)
            return BadRequest("Ce produit est déjà utilisé dans une vente. Il ne peut pas être supprimé.");

        produit.IsActive = false;
        produit.DateModification = DateTime.UtcNow;

        var mouvements = await context.StockMouvements
            .Where(m => m.IsActive && m.ProduitVenteId == id)
            .ToListAsync();

        foreach (var mouvement in mouvements)
        {
            mouvement.IsActive = false;
            mouvement.DateModification = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

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
}
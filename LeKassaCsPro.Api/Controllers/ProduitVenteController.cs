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

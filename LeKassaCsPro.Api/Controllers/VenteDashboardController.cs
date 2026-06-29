using LeKassaCsPro.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenteDashboardController(AppDbContext context) : ControllerBase
{
    [HttpGet("resume")]
    public async Task<ActionResult<VenteDashboardResumeResponse>> GetResumeAsync()
    {
        var aujourdHui = DateTime.UtcNow.Date;
        var demain = aujourdHui.AddDays(1);

        var ventesJour = await context.Ventes
            .Where(v => v.IsActive && v.DateVente >= aujourdHui && v.DateVente < demain)
            .ToListAsync();

        var venteIds = ventesJour.Select(v => v.Id).ToList();

        var detailsJour = await context.VenteDetails
            .Where(d => d.IsActive && venteIds.Contains(d.VenteId))
            .ToListAsync();

        var produits = await context.ProduitsVente
            .Where(p => p.IsActive)
            .ToListAsync();

        var produitsParId = produits.ToDictionary(p => p.Id);

        var benefice = detailsJour.Sum(detail =>
        {
            var prixAchat = produitsParId.TryGetValue(detail.ProduitVenteId, out var produit)
                ? produit.PrixAchat
                : 0m;

            return (detail.PrixUnitaire - prixAchat) * detail.Quantite;
        });

        var alertes = new List<ProduitAlerteApiResponse>();

        foreach (var produit in produits)
        {
            var stock = await GetStockProduitAsync(produit.Id);

            if (produit.StockAlerte > 0 && stock <= produit.StockAlerte)
            {
                alertes.Add(new ProduitAlerteApiResponse
                {
                    Nom = produit.Nom,
                    Unite = produit.Unite,
                    Stock = stock
                });
            }
        }

        return Ok(new VenteDashboardResumeResponse
        {
            Date = aujourdHui,
            VentesJour = ventesJour.Count,
            ChiffreAffaires = ventesJour.Sum(v => v.MontantTotal),
            Benefice = benefice,
            Alertes = alertes
        });
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

public class VenteDashboardResumeResponse
{
    public DateTime Date { get; set; }
    public int VentesJour { get; set; }
    public decimal ChiffreAffaires { get; set; }
    public decimal Benefice { get; set; }
    public List<ProduitAlerteApiResponse> Alertes { get; set; } = new();
}

public class ProduitAlerteApiResponse
{
    public string Nom { get; set; } = string.Empty;
    public string Unite { get; set; } = string.Empty;
    public decimal Stock { get; set; }
}

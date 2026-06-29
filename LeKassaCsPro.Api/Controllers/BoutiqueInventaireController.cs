using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoutiqueInventaireController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppBoutiqueInventaire>>> GetAllAsync()
    {
        var inventaires = await context.BoutiqueInventaires
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderByDescending(i => i.DateInventaire)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

        return Ok(inventaires);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppBoutiqueInventaire>> GetByIdAsync(int id)
    {
        var inventaire = await context.BoutiqueInventaires
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

        if (inventaire == null)
            return NotFound();

        return Ok(inventaire);
    }

    [HttpGet("boutique/{boutiqueId:int}")]
    public async Task<ActionResult<List<AppBoutiqueInventaire>>> GetByBoutiqueAsync(int boutiqueId)
    {
        var inventaires = await context.BoutiqueInventaires
            .AsNoTracking()
            .Where(i => i.IsActive && i.BoutiqueId == boutiqueId)
            .OrderByDescending(i => i.DateInventaire)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

        return Ok(inventaires);
    }

    [HttpGet("boutique/{boutiqueId:int}/dernier")]
    public async Task<ActionResult<AppBoutiqueInventaire?>> GetDernierByBoutiqueAsync(int boutiqueId)
    {
        var inventaire = await context.BoutiqueInventaires
            .AsNoTracking()
            .Where(i => i.IsActive && i.BoutiqueId == boutiqueId)
            .OrderByDescending(i => i.DateInventaire)
            .ThenByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        return Ok(inventaire);
    }

    [HttpPost]
    public async Task<ActionResult<AppBoutiqueInventaire>> CreateAsync(AppBoutiqueInventaire inventaire)
    {
        inventaire.Id = 0;
        inventaire.DateInventaire = NormaliserDateUtc(inventaire.DateInventaire);
        inventaire.BoutiqueNom = inventaire.BoutiqueNom?.Trim() ?? string.Empty;
        inventaire.Observation = inventaire.Observation?.Trim() ?? string.Empty;
        inventaire.UtilisateurNom = inventaire.UtilisateurNom?.Trim() ?? string.Empty;
        inventaire.RoleUtilisateur = inventaire.RoleUtilisateur?.Trim() ?? string.Empty;
        inventaire.IsActive = true;
        inventaire.DateCreation = DateTime.UtcNow;
        inventaire.DateModification = DateTime.UtcNow;

        context.BoutiqueInventaires.Add(inventaire);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = inventaire.Id }, inventaire);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppBoutiqueInventaire>> UpdateAsync(int id, AppBoutiqueInventaire request)
    {
        var inventaire = await context.BoutiqueInventaires.FirstOrDefaultAsync(i => i.Id == id);

        if (inventaire == null)
            return NotFound();

        inventaire.BoutiqueId = request.BoutiqueId;
        inventaire.BoutiqueNom = request.BoutiqueNom?.Trim() ?? string.Empty;
        inventaire.DateInventaire = NormaliserDateUtc(request.DateInventaire);
        inventaire.BudgetInitial = request.BudgetInitial;
        inventaire.MontantVente = request.MontantVente;
        inventaire.DepenseProprietaire = request.DepenseProprietaire;
        inventaire.SoldeCaisse = request.SoldeCaisse;
        inventaire.GainMois = request.GainMois;
        inventaire.Observation = request.Observation?.Trim() ?? string.Empty;
        inventaire.IsActive = request.IsActive;
        inventaire.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Ok(inventaire);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var inventaire = await context.BoutiqueInventaires.FirstOrDefaultAsync(i => i.Id == id);

        if (inventaire == null)
            return NotFound();

        inventaire.IsActive = false;
        inventaire.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            return DateTime.UtcNow;

        if (date.Kind == DateTimeKind.Utc)
            return date;

        if (date.Kind == DateTimeKind.Local)
            return date.ToUniversalTime();

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

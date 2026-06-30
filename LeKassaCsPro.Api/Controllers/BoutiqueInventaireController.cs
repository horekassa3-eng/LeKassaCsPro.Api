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
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

        if (inventaire == null)
            return NotFound();

        return Ok(inventaire);
    }

    [HttpGet("boutique/{boutiqueId:int}")]
    public async Task<ActionResult<List<AppBoutiqueInventaire>>> GetByBoutiqueAsync(int boutiqueId)
    {
        var inventaires = await context.BoutiqueInventaires
            .Where(i => i.IsActive && i.BoutiqueId == boutiqueId)
            .OrderByDescending(i => i.DateInventaire)
            .ThenByDescending(i => i.Id)
            .ToListAsync();

        return Ok(inventaires);
    }

    [HttpGet("dernier/{boutiqueId:int}")]
    public async Task<ActionResult<AppBoutiqueInventaire?>> GetDernierAsync(int boutiqueId)
    {
        var inventaire = await context.BoutiqueInventaires
            .Where(i => i.IsActive && i.BoutiqueId == boutiqueId)
            .OrderByDescending(i => i.DateInventaire)
            .ThenByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        if (inventaire == null)
            return NotFound();

        return Ok(inventaire);
    }

    [HttpPost]
    public async Task<ActionResult<AppBoutiqueInventaire>> CreateAsync(AppBoutiqueInventaire inventaire)
    {
        if (inventaire.BoutiqueId <= 0)
            return BadRequest("Boutique obligatoire.");

        if (inventaire.ValeurStock <= 0)
            return BadRequest("Chiffre actuel obligatoire.");

        inventaire.Id = 0;
        inventaire.BoutiqueNom = inventaire.BoutiqueNom?.Trim() ?? string.Empty;
        inventaire.Observation = inventaire.Observation?.Trim() ?? string.Empty;
        inventaire.UtilisateurNom = inventaire.UtilisateurNom?.Trim() ?? string.Empty;
        inventaire.RoleUtilisateur = inventaire.RoleUtilisateur?.Trim() ?? string.Empty;
        inventaire.DateInventaire = NormaliserDateUtc(inventaire.DateInventaire);
        inventaire.DateCreation = DateTime.UtcNow;
        inventaire.DateModification = DateTime.UtcNow;
        inventaire.IsActive = true;

        context.BoutiqueInventaires.Add(inventaire);
        await context.SaveChangesAsync();

        return Ok(inventaire);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppBoutiqueInventaire>> UpdateAsync(int id, AppBoutiqueInventaire request)
    {
        var inventaire = await context.BoutiqueInventaires
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

        if (inventaire == null)
            return NotFound();

        if (request.BoutiqueId <= 0)
            return BadRequest("Boutique obligatoire.");

        if (request.ValeurStock <= 0)
            return BadRequest("Chiffre actuel obligatoire.");

        inventaire.BoutiqueId = request.BoutiqueId;
        inventaire.BoutiqueNom = request.BoutiqueNom?.Trim() ?? string.Empty;
        inventaire.DateInventaire = NormaliserDateUtc(request.DateInventaire);
        inventaire.ValeurStock = request.ValeurStock;
        inventaire.GainMois = request.GainMois;
        inventaire.DepenseProprietaireMois = request.DepenseProprietaireMois;
        inventaire.Observation = request.Observation?.Trim() ?? string.Empty;
        inventaire.UtilisateurId = request.UtilisateurId;
        inventaire.UtilisateurNom = request.UtilisateurNom?.Trim() ?? string.Empty;
        inventaire.RoleUtilisateur = request.RoleUtilisateur?.Trim() ?? string.Empty;
        inventaire.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(inventaire);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var inventaire = await context.BoutiqueInventaires
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

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

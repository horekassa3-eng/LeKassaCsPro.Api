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

    [HttpGet("boutique/{boutiqueId:int}/dernier")]
    public async Task<ActionResult<AppBoutiqueInventaire?>> GetDernierAsync(int boutiqueId)
    {
        var inventaire = await context.BoutiqueInventaires
            .Where(i => i.IsActive && i.BoutiqueId == boutiqueId)
            .OrderByDescending(i => i.DateInventaire)
            .ThenByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        return Ok(inventaire);
    }

    [HttpPost]
    public async Task<ActionResult<AppBoutiqueInventaire>> SaveAsync([FromBody] AppBoutiqueInventaire request)
    {
        if (request.BoutiqueId <= 0)
            return BadRequest("La boutique est obligatoire.");

        var totalComposants = CalculerTotalActuel(request);
        var composantsSaisis = request.ValeurMarchandise > 0
                               || request.ArgentLiquide > 0
                               || request.DetteClient > 0
                               || request.Depot > 0;

        if (composantsSaisis)
            request.ValeurStock = totalComposants;

        if (request.ValeurStock <= 0)
            return BadRequest("Le total actuel doit être supérieur à 0.");

        request.DateInventaire = NormaliserDateUtc(request.DateInventaire);

        if (request.Id == 0)
        {
            request.IsActive = true;
            request.DateCreation = DateTime.UtcNow;
            request.DateModification = DateTime.UtcNow;

            context.BoutiqueInventaires.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var inventaire = await context.BoutiqueInventaires
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.IsActive);

        if (inventaire == null)
            return NotFound();

        inventaire.BoutiqueId = request.BoutiqueId;
        inventaire.BoutiqueNom = request.BoutiqueNom?.Trim() ?? string.Empty;
        inventaire.DateInventaire = request.DateInventaire;
        inventaire.ValeurStock = request.ValeurStock;
        inventaire.ValeurMarchandise = request.ValeurMarchandise;
        inventaire.ArgentLiquide = request.ArgentLiquide;
        inventaire.DetteClient = request.DetteClient;
        inventaire.Depot = request.Depot;
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

    private static decimal CalculerTotalActuel(AppBoutiqueInventaire inventaire)
    {
        return inventaire.ValeurMarchandise
               + inventaire.ArgentLiquide
               + inventaire.DetteClient
               - inventaire.Depot;
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

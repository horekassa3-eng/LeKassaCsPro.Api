using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoutiqueBudgetMouvementController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppBoutiqueBudgetMouvement>>> GetAllAsync()
    {
        var mouvements = await context.BoutiqueBudgetMouvements
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("boutique/{boutiqueId:int}")]
    public async Task<ActionResult<List<AppBoutiqueBudgetMouvement>>> GetByBoutiqueAsync(int boutiqueId)
    {
        var mouvements = await context.BoutiqueBudgetMouvements
            .Where(m => m.IsActive && m.BoutiqueId == boutiqueId)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpPost]
    public async Task<ActionResult<AppBoutiqueBudgetMouvement>> SaveAsync([FromBody] AppBoutiqueBudgetMouvement request)
    {
        if (request.BoutiqueId <= 0)
            return BadRequest("La boutique est obligatoire.");

        if (request.Montant <= 0)
            return BadRequest("Le montant est obligatoire.");

        request.DateMouvement = NormaliserDateUtc(request.DateMouvement);

        if (request.Id == 0)
        {
            request.IsActive = true;
            request.DateCreation = DateTime.UtcNow;
            request.DateModification = DateTime.UtcNow;

            context.BoutiqueBudgetMouvements.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var mouvement = await context.BoutiqueBudgetMouvements
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        mouvement.BoutiqueId = request.BoutiqueId;
        mouvement.BoutiqueNom = request.BoutiqueNom?.Trim() ?? string.Empty;
        mouvement.DateMouvement = request.DateMouvement;
        mouvement.TypeMouvement = request.TypeMouvement?.Trim() ?? string.Empty;
        mouvement.Montant = request.Montant;
        mouvement.Motif = request.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = request.Observation?.Trim() ?? string.Empty;
        mouvement.UtilisateurId = request.UtilisateurId;
        mouvement.UtilisateurNom = request.UtilisateurNom?.Trim() ?? string.Empty;
        mouvement.RoleUtilisateur = request.RoleUtilisateur?.Trim() ?? string.Empty;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(mouvement);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var mouvement = await context.BoutiqueBudgetMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

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

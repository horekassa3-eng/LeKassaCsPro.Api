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
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("boutique/{boutiqueId:int}")]
    public async Task<ActionResult<List<AppBoutiqueBudgetMouvement>>> GetByBoutiqueAsync(int boutiqueId)
    {
        var mouvements = await context.BoutiqueBudgetMouvements
            .Where(m => m.IsActive && m.BoutiqueId == boutiqueId)
            .OrderByDescending(m => m.DateMouvement)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppBoutiqueBudgetMouvement>> GetByIdAsync(int id)
    {
        var mouvement = await context.BoutiqueBudgetMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        return Ok(mouvement);
    }

    [HttpPost]
    public async Task<ActionResult<AppBoutiqueBudgetMouvement>> CreateAsync(AppBoutiqueBudgetMouvement mouvement)
    {
        mouvement.Id = 0;
        mouvement.DateMouvement = NormaliserDateUtc(mouvement.DateMouvement);
        mouvement.DateCreation = DateTime.UtcNow;
        mouvement.DateModification = DateTime.UtcNow;
        mouvement.IsActive = true;
        Nettoyer(mouvement);

        context.BoutiqueBudgetMouvements.Add(mouvement);
        await context.SaveChangesAsync();

        return Ok(mouvement);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppBoutiqueBudgetMouvement>> UpdateAsync(int id, AppBoutiqueBudgetMouvement request)
    {
        var mouvement = await context.BoutiqueBudgetMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        mouvement.BoutiqueId = request.BoutiqueId;
        mouvement.BoutiqueNom = request.BoutiqueNom;
        mouvement.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        mouvement.TypeMouvement = request.TypeMouvement;
        mouvement.Montant = request.Montant;
        mouvement.Motif = request.Motif;
        mouvement.Observation = request.Observation;
        mouvement.UtilisateurId = request.UtilisateurId;
        mouvement.UtilisateurNom = request.UtilisateurNom;
        mouvement.RoleUtilisateur = request.RoleUtilisateur;
        mouvement.DateModification = DateTime.UtcNow;
        Nettoyer(mouvement);

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

    private static void Nettoyer(AppBoutiqueBudgetMouvement mouvement)
    {
        mouvement.BoutiqueNom = mouvement.BoutiqueNom?.Trim() ?? string.Empty;
        mouvement.TypeMouvement = mouvement.TypeMouvement?.Trim() ?? string.Empty;
        mouvement.Motif = mouvement.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = mouvement.Observation?.Trim() ?? string.Empty;
        mouvement.UtilisateurNom = mouvement.UtilisateurNom?.Trim() ?? string.Empty;
        mouvement.RoleUtilisateur = mouvement.RoleUtilisateur?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(mouvement.TypeMouvement))
            mouvement.TypeMouvement = "Depense proprietaire";

        if (string.IsNullOrWhiteSpace(mouvement.Motif))
            mouvement.Motif = mouvement.TypeMouvement;
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

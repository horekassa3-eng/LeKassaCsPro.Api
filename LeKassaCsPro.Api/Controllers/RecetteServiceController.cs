using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecetteServiceController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppRecetteService>>> GetAllAsync()
    {
        var recettes = await context.RecettesServices
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.DateRecette)
            .ThenByDescending(r => r.Id)
            .ToListAsync();

        return Ok(recettes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppRecetteService>> GetByIdAsync(int id)
    {
        var recette = await context.RecettesServices
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (recette == null)
            return NotFound();

        return Ok(recette);
    }

    [HttpPost]
    public async Task<ActionResult<AppRecetteService>> SaveAsync([FromBody] AppRecetteService request)
    {
        if (string.IsNullOrWhiteSpace(request.TypeService))
            return BadRequest("Le type de recette est obligatoire.");

        if (request.Montant <= 0)
            return BadRequest("Le montant doit être supérieur à zéro.");

        request.DateRecette = NormaliserDateUtc(request.DateRecette);
        request.Devise = string.IsNullOrWhiteSpace(request.Devise) ? "FCFA" : request.Devise.Trim();
        request.TypeService = request.TypeService.Trim();
        request.Observation = request.Observation?.Trim() ?? string.Empty;
        request.UtilisateurNom = string.IsNullOrWhiteSpace(request.UtilisateurNom)
            ? "Utilisateur local"
            : request.UtilisateurNom.Trim();
        request.RoleUtilisateur = string.IsNullOrWhiteSpace(request.RoleUtilisateur)
            ? "Caissier"
            : request.RoleUtilisateur.Trim();
        request.IsActive = true;

        if (request.Id == 0)
        {
            request.DateCreation = DateTime.UtcNow;
            request.DateModification = DateTime.UtcNow;

            context.RecettesServices.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var recette = await context.RecettesServices
            .FirstOrDefaultAsync(r => r.Id == request.Id);

        if (recette == null)
            return NotFound();

        recette.DateRecette = request.DateRecette;
        recette.TypeService = request.TypeService;
        recette.Devise = request.Devise;
        recette.Montant = request.Montant;
        recette.Observation = request.Observation;
        recette.IsActive = true;
        recette.UtilisateurId = request.UtilisateurId;
        recette.UtilisateurNom = request.UtilisateurNom;
        recette.RoleUtilisateur = request.RoleUtilisateur;
        recette.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(recette);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var recette = await context.RecettesServices
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

        if (recette == null)
            return NotFound();

        recette.IsActive = false;
        recette.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

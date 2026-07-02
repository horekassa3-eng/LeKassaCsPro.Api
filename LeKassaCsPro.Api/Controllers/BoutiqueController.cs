using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoutiqueController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppBoutique>>> GetAllAsync()
    {
        var boutiques = await context.Boutiques
            .Where(b => b.IsActive)
            .OrderBy(b => b.Nom)
            .ToListAsync();

        return Ok(boutiques);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppBoutique>> GetByIdAsync(int id)
    {
        var boutique = await context.Boutiques
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (boutique == null)
            return NotFound();

        return Ok(boutique);
    }

    [HttpPost]
    public async Task<ActionResult<AppBoutique>> SaveAsync([FromBody] AppBoutique request)
    {
        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom de la boutique est obligatoire.");

        if (request.Id == 0)
        {
            request.Nom = request.Nom.Trim();
            request.IsActive = true;
            request.DateCreation = DateTime.UtcNow;
            request.DateModification = DateTime.UtcNow;

            context.Boutiques.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var boutique = await context.Boutiques
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.IsActive);

        if (boutique == null)
            return NotFound();

        boutique.Nom = request.Nom.Trim();
        boutique.Pays = request.Pays?.Trim() ?? string.Empty;
        boutique.Ville = request.Ville?.Trim() ?? string.Empty;
        boutique.GerantNom = request.GerantNom?.Trim() ?? string.Empty;
        boutique.GerantTelephone = request.GerantTelephone?.Trim() ?? string.Empty;
        boutique.AssistantNom = request.AssistantNom?.Trim() ?? string.Empty;
        boutique.AssistantTelephone = request.AssistantTelephone?.Trim() ?? string.Empty;
        boutique.BudgetInitial = request.BudgetInitial;
        boutique.DateOuverture = NormaliserDateUtc(request.DateOuverture);
        boutique.GerantUtilisateurId = request.GerantUtilisateurId;
        boutique.AssistantUtilisateurId = request.AssistantUtilisateurId;
        boutique.UtilisateurId = request.UtilisateurId;
        boutique.UtilisateurNom = request.UtilisateurNom?.Trim() ?? string.Empty;
        boutique.RoleUtilisateur = request.RoleUtilisateur?.Trim() ?? string.Empty;
        boutique.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(boutique);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var boutique = await context.Boutiques
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (boutique == null)
            return NotFound();

        boutique.IsActive = false;
        boutique.DateModification = DateTime.UtcNow;

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

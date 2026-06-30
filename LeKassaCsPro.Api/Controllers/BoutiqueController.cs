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
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Nom)
            .ThenBy(b => b.Id)
            .ToListAsync();

        return Ok(boutiques);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppBoutique>> GetByIdAsync(int id)
    {
        var boutique = await context.Boutiques
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

        if (boutique == null)
            return NotFound();

        return Ok(boutique);
    }

    [HttpPost]
    public async Task<ActionResult<AppBoutique>> CreateAsync(AppBoutique boutique)
    {
        boutique.Id = 0;
        NettoyerBoutique(boutique);
        boutique.IsActive = true;
        boutique.DateCreation = DateTime.UtcNow;
        boutique.DateModification = DateTime.UtcNow;

        context.Boutiques.Add(boutique);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = boutique.Id }, boutique);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppBoutique>> UpdateAsync(int id, AppBoutique request)
    {
        var boutique = await context.Boutiques.FirstOrDefaultAsync(b => b.Id == id);

        if (boutique == null)
            return NotFound();

        boutique.Nom = request.Nom?.Trim() ?? string.Empty;
        boutique.Pays = request.Pays?.Trim() ?? string.Empty;
        boutique.Ville = request.Ville?.Trim() ?? string.Empty;
        boutique.Adresse = request.Adresse?.Trim() ?? string.Empty;
        boutique.DateOuverture = NormaliserDateUtc(request.DateOuverture);
        boutique.BudgetInitial = request.BudgetInitial;
        boutique.GerantUtilisateurId = request.GerantUtilisateurId;
        boutique.GerantNom = request.GerantNom?.Trim() ?? string.Empty;
        boutique.GerantTelephone = request.GerantTelephone?.Trim() ?? string.Empty;
        boutique.AssistantUtilisateurId = request.AssistantUtilisateurId;
        boutique.AssistantNom = request.AssistantNom?.Trim() ?? string.Empty;
        boutique.AssistantTelephone = request.AssistantTelephone?.Trim() ?? string.Empty;
        boutique.Observation = request.Observation?.Trim() ?? string.Empty;
        boutique.IsActive = request.IsActive;
        boutique.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Ok(boutique);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var boutique = await context.Boutiques.FirstOrDefaultAsync(b => b.Id == id);

        if (boutique == null)
            return NotFound();

        boutique.IsActive = false;
        boutique.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    private static void NettoyerBoutique(AppBoutique boutique)
    {
        boutique.Nom = boutique.Nom?.Trim() ?? string.Empty;
        boutique.Pays = boutique.Pays?.Trim() ?? string.Empty;
        boutique.Ville = boutique.Ville?.Trim() ?? string.Empty;
        boutique.Adresse = boutique.Adresse?.Trim() ?? string.Empty;
        boutique.DateOuverture = NormaliserDateUtc(boutique.DateOuverture);
        boutique.GerantNom = boutique.GerantNom?.Trim() ?? string.Empty;
        boutique.GerantTelephone = boutique.GerantTelephone?.Trim() ?? string.Empty;
        boutique.AssistantNom = boutique.AssistantNom?.Trim() ?? string.Empty;
        boutique.AssistantTelephone = boutique.AssistantTelephone?.Trim() ?? string.Empty;
        boutique.Observation = boutique.Observation?.Trim() ?? string.Empty;
        boutique.UtilisateurNom = boutique.UtilisateurNom?.Trim() ?? string.Empty;
        boutique.RoleUtilisateur = boutique.RoleUtilisateur?.Trim() ?? string.Empty;
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

using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SoldeAgenceMouvementController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppSoldeAgenceMouvement>>> GetAllAsync()
    {
        var items = await context.SoldeAgenceMouvements
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("solde")]
    public async Task<ActionResult<decimal>> GetSoldeAsync(
        [FromQuery] string pays,
        [FromQuery] string moyen,
        [FromQuery] string devise)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Where(m => m.PaysAgence == pays)
            .Where(m => m.MoyenPaiement == moyen)
            .Where(m => m.Devise == devise)
            .ToListAsync();

        decimal solde = 0;

        foreach (var mouvement in mouvements)
        {
            solde += EstSortie(mouvement.TypeMouvement)
                ? -mouvement.Montant
                : mouvement.Montant;
        }

        return Ok(solde);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> GetByIdAsync(int id)
    {
        var item = await context.SoldeAgenceMouvements
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> CreateAsync([FromBody] AppSoldeAgenceMouvement item)
    {
        if (item == null)
            return BadRequest("Données invalides.");

        if (item.DateMouvement == default)
            item.DateMouvement = DateTime.UtcNow;

        item.DateMouvement = NormaliserDateUtc(item.DateMouvement);
        item.DateCreation = DateTime.UtcNow;
        item.DateModification = DateTime.UtcNow;
        item.IsActive = true;

        context.SoldeAgenceMouvements.Add(item);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> UpdateAsync(int id, [FromBody] AppSoldeAgenceMouvement item)
    {
        if (item == null || id <= 0)
            return BadRequest("Données invalides.");

        var existing = await context.SoldeAgenceMouvements.FirstOrDefaultAsync(m => m.Id == id);

        if (existing == null)
            return NotFound();

        existing.DateMouvement = NormaliserDateUtc(item.DateMouvement == default ? existing.DateMouvement : item.DateMouvement);
        existing.PaysAgence = item.PaysAgence?.Trim() ?? string.Empty;
        existing.MoyenPaiement = item.MoyenPaiement?.Trim() ?? string.Empty;
        existing.Devise = string.IsNullOrWhiteSpace(item.Devise) ? "FCFA" : item.Devise.Trim();
        existing.TypeMouvement = item.TypeMouvement?.Trim() ?? string.Empty;
        existing.Montant = item.Montant;
        existing.Motif = item.Motif?.Trim() ?? string.Empty;
        existing.Observation = item.Observation?.Trim() ?? string.Empty;
        existing.SourceModule = item.SourceModule?.Trim() ?? string.Empty;
        existing.SourceId = item.SourceId;
        existing.IsActive = item.IsActive;
        existing.UtilisateurId = item.UtilisateurId;
        existing.UtilisateurNom = item.UtilisateurNom?.Trim() ?? string.Empty;
        existing.RoleUtilisateur = item.RoleUtilisateur?.Trim() ?? string.Empty;
        existing.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var item = await context.SoldeAgenceMouvements.FirstOrDefaultAsync(m => m.Id == id);

        if (item == null)
            return NotFound();

        item.IsActive = false;
        item.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    private static bool EstSortie(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return false;

        return type.Contains("sortie", StringComparison.OrdinalIgnoreCase)
               || type.Contains("débit", StringComparison.OrdinalIgnoreCase)
               || type.Contains("debit", StringComparison.OrdinalIgnoreCase)
               || type.Contains("envoi", StringComparison.OrdinalIgnoreCase)
               || type.Contains("retrait", StringComparison.OrdinalIgnoreCase)
               || type.Contains("annulation", StringComparison.OrdinalIgnoreCase);
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

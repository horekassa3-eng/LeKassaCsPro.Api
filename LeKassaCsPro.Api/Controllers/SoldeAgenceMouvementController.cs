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
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("pays/{paysAgence}")]
    public async Task<ActionResult<List<AppSoldeAgenceMouvement>>> GetByPaysAsync(string paysAgence)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive && m.PaysAgence.ToLower() == paysAgence.Trim().ToLower())
            .OrderByDescending(m => m.DateMouvement)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("solde")]
    public async Task<ActionResult<decimal>> GetSoldeAsync(
        [FromQuery] string paysAgence,
        [FromQuery] string moyenPaiement,
        [FromQuery] string devise)
    {
        paysAgence = paysAgence?.Trim() ?? string.Empty;
        moyenPaiement = moyenPaiement?.Trim() ?? string.Empty;
        devise = devise?.Trim() ?? string.Empty;

        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive
                        && m.PaysAgence.ToLower() == paysAgence.ToLower()
                        && m.MoyenPaiement.ToLower() == moyenPaiement.ToLower()
                        && m.Devise.ToLower() == devise.ToLower())
            .ToListAsync();

        decimal solde = 0;

        foreach (var mouvement in mouvements)
        {
            if (EstEntree(mouvement.TypeMouvement))
                solde += mouvement.Montant;
            else
                solde -= mouvement.Montant;
        }

        return Ok(solde);
    }

    [HttpPost]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> CreateAsync(AppSoldeAgenceMouvement mouvement)
    {
        mouvement.Id = 0;
        mouvement.PaysAgence = mouvement.PaysAgence?.Trim() ?? string.Empty;
        mouvement.MoyenPaiement = string.IsNullOrWhiteSpace(mouvement.MoyenPaiement)
            ? "Espèces"
            : mouvement.MoyenPaiement.Trim();
        mouvement.Devise = string.IsNullOrWhiteSpace(mouvement.Devise)
            ? "FCFA"
            : mouvement.Devise.Trim();
        mouvement.TypeMouvement = mouvement.TypeMouvement?.Trim() ?? string.Empty;
        mouvement.DateMouvement = NormaliserDateUtc(mouvement.DateMouvement);
        mouvement.DateCreation = DateTime.UtcNow;
        mouvement.DateModification = DateTime.UtcNow;
        mouvement.IsActive = true;

        context.SoldeAgenceMouvements.Add(mouvement);
        await context.SaveChangesAsync();

        return Ok(mouvement);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> UpdateAsync(int id, AppSoldeAgenceMouvement request)
    {
        var mouvement = await context.SoldeAgenceMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        mouvement.PaysAgence = request.PaysAgence?.Trim() ?? string.Empty;
        mouvement.MoyenPaiement = request.MoyenPaiement?.Trim() ?? "Espèces";
        mouvement.Devise = request.Devise?.Trim() ?? "FCFA";
        mouvement.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        mouvement.TypeMouvement = request.TypeMouvement?.Trim() ?? string.Empty;
        mouvement.Montant = request.Montant;
        mouvement.Motif = request.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = request.Observation?.Trim() ?? string.Empty;
        mouvement.SourceModule = request.SourceModule?.Trim() ?? string.Empty;
        mouvement.SourceId = request.SourceId;
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
        var mouvement = await context.SoldeAgenceMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static bool EstEntree(string? type)
    {
        return string.Equals(type, "Entrée", StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "Entree", StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "Approvisionnement", StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "Réception approvisionnement", StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "Reception approvisionnement", StringComparison.OrdinalIgnoreCase);
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

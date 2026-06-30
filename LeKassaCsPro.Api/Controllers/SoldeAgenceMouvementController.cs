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
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> GetByIdAsync(int id)
    {
        var mouvement = await context.SoldeAgenceMouvements
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        return Ok(mouvement);
    }

    [HttpGet("solde")]
    public async Task<ActionResult<decimal>> GetSoldeAsync(
        [FromQuery] string? pays,
        [FromQuery] string? moyen,
        [FromQuery] string? devise)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .AsNoTracking()
            .Where(m => m.IsActive)
            .ToListAsync();

        var total = mouvements
            .Where(m => Correspond(m.PaysAgence, pays)
                        && Correspond(m.MoyenPaiement, moyen)
                        && Correspond(m.Devise, devise))
            .Sum(CalculerImpact);

        return Ok(total);
    }

    [HttpGet("pays/{pays}")]
    public async Task<ActionResult<List<AppSoldeAgenceMouvement>>> GetByPaysAsync(string pays)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .AsNoTracking()
            .Where(m => m.IsActive && m.PaysAgence.ToLower() == pays.Trim().ToLower())
            .OrderByDescending(m => m.DateMouvement)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpPost]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> CreateAsync([FromBody] AppSoldeAgenceMouvement mouvement)
    {
        if (mouvement == null)
            return BadRequest("Données invalides.");

        if (mouvement.Montant <= 0)
            return BadRequest("Montant obligatoire.");

        mouvement.Id = 0;
        Nettoyer(mouvement);
        mouvement.DateMouvement = NormaliserDateUtc(mouvement.DateMouvement);
        mouvement.DateCreation = DateTime.UtcNow;
        mouvement.DateModification = DateTime.UtcNow;
        mouvement.IsActive = true;

        context.SoldeAgenceMouvements.Add(mouvement);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = mouvement.Id }, mouvement);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> UpdateAsync(int id, [FromBody] AppSoldeAgenceMouvement mouvement)
    {
        if (mouvement == null || id <= 0)
            return BadRequest("Données invalides.");

        var existant = await context.SoldeAgenceMouvements.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (existant == null)
            return NotFound();

        existant.DateMouvement = NormaliserDateUtc(mouvement.DateMouvement == default ? existant.DateMouvement : mouvement.DateMouvement);
        existant.PaysAgence = mouvement.PaysAgence?.Trim() ?? string.Empty;
        existant.MoyenPaiement = string.IsNullOrWhiteSpace(mouvement.MoyenPaiement) ? "Espèces" : mouvement.MoyenPaiement.Trim();
        existant.Devise = string.IsNullOrWhiteSpace(mouvement.Devise) ? "FCFA" : mouvement.Devise.Trim();
        existant.TypeMouvement = mouvement.TypeMouvement?.Trim() ?? string.Empty;
        existant.Montant = mouvement.Montant;
        existant.Motif = mouvement.Motif?.Trim() ?? string.Empty;
        existant.Observation = mouvement.Observation?.Trim() ?? string.Empty;
        existant.UtilisateurId = mouvement.UtilisateurId;
        existant.UtilisateurNom = mouvement.UtilisateurNom?.Trim() ?? string.Empty;
        existant.RoleUtilisateur = mouvement.RoleUtilisateur?.Trim() ?? string.Empty;
        existant.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(existant);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var mouvement = await context.SoldeAgenceMouvements.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static void Nettoyer(AppSoldeAgenceMouvement mouvement)
    {
        mouvement.PaysAgence = mouvement.PaysAgence?.Trim() ?? string.Empty;
        mouvement.MoyenPaiement = string.IsNullOrWhiteSpace(mouvement.MoyenPaiement) ? "Espèces" : mouvement.MoyenPaiement.Trim();
        mouvement.Devise = string.IsNullOrWhiteSpace(mouvement.Devise) ? "FCFA" : mouvement.Devise.Trim();
        mouvement.TypeMouvement = mouvement.TypeMouvement?.Trim() ?? string.Empty;
        mouvement.Motif = mouvement.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = mouvement.Observation?.Trim() ?? string.Empty;
        mouvement.UtilisateurNom = mouvement.UtilisateurNom?.Trim() ?? string.Empty;
        mouvement.RoleUtilisateur = mouvement.RoleUtilisateur?.Trim() ?? string.Empty;
    }

    private static bool Correspond(string valeur, string? filtre)
    {
        if (string.IsNullOrWhiteSpace(filtre))
            return true;

        return string.Equals(
            Normaliser(valeur),
            Normaliser(filtre),
            StringComparison.OrdinalIgnoreCase);
    }

    private static decimal CalculerImpact(AppSoldeAgenceMouvement mouvement)
    {
        return EstSortie(mouvement.TypeMouvement)
            ? -mouvement.Montant
            : mouvement.Montant;
    }

    private static bool EstSortie(string? type)
    {
        var texte = Normaliser(type);

        return texte.Contains("sortie")
               || texte.Contains("retrait")
               || texte.Contains("depense")
               || texte.Contains("dépense")
               || texte.Contains("annulation")
               || texte.Contains("annule");
    }

    private static string Normaliser(string? valeur)
    {
        return (valeur ?? string.Empty)
            .Trim()
            .Replace("é", "e")
            .Replace("è", "e")
            .Replace("ê", "e")
            .Replace("ë", "e")
            .Replace("É", "e")
            .Replace("È", "e")
            .Replace("Ê", "e")
            .Replace("Ë", "e")
            .ToLowerInvariant();
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

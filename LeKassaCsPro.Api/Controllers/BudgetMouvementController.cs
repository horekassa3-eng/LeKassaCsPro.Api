using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetMouvementController(AppDbContext context) : ControllerBase
{
    private const string TypeEntree = "Entrée";
    private const string TypeSortie = "Sortie";
    private const string DeviseFcfa = "FCFA";
    private const string SourceManuel = "Manuel";

    [HttpGet]
    public async Task<ActionResult<List<AppBudgetMouvement>>> GetAllAsync()
    {
        var mouvements = await context.BudgetMouvements
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("solde/{devise}")]
    public async Task<ActionResult<decimal>> GetSoldeAsync(string devise)
    {
        var mouvements = await context.BudgetMouvements
            .Where(m => m.IsActive && m.Devise == devise)
            .ToListAsync();

        var soldeInitial = mouvements
            .Where(EstSoldeInitial)
            .Sum(m => m.Montant);

        var entrees = mouvements
            .Where(m => EstType(m.TypeMouvement, TypeEntree) && !EstSoldeInitial(m))
            .Sum(m => m.Montant);

        var sorties = mouvements
            .Where(m => EstType(m.TypeMouvement, TypeSortie))
            .Sum(m => m.Montant);

        return Ok(soldeInitial + entrees - sorties);
    }

    [HttpPost]
    public async Task<ActionResult<AppBudgetMouvement>> CreateAsync([FromBody] AppBudgetMouvement request)
    {
        var validation = Valider(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return BadRequest(validation);

        request.Id = 0;
        request.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        request.Devise = string.IsNullOrWhiteSpace(request.Devise) ? DeviseFcfa : request.Devise.Trim();
        request.TypeMouvement = request.TypeMouvement.Trim();
        request.Motif = request.Motif?.Trim() ?? string.Empty;
        request.Observation = request.Observation?.Trim() ?? string.Empty;
        request.SourceModule = string.IsNullOrWhiteSpace(request.SourceModule) ? SourceManuel : request.SourceModule.Trim();
        request.IsActive = true;
        request.DateCreation = DateTime.UtcNow;
        request.DateModification = DateTime.UtcNow;

        context.BudgetMouvements.Add(request);
        await context.SaveChangesAsync();

        return Ok(request);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppBudgetMouvement>> UpdateAsync(int id, [FromBody] AppBudgetMouvement request)
    {
        var mouvement = await context.BudgetMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        if (mouvement.IsAutomatique)
            return BadRequest("Ce mouvement automatique ne peut pas être modifié depuis la page Budget.");

        var validation = Valider(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return BadRequest(validation);

        mouvement.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        mouvement.TypeMouvement = request.TypeMouvement.Trim();
        mouvement.Devise = string.IsNullOrWhiteSpace(request.Devise) ? DeviseFcfa : request.Devise.Trim();
        mouvement.Montant = request.Montant;
        mouvement.Motif = request.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = request.Observation?.Trim() ?? string.Empty;
        mouvement.SourceModule = string.IsNullOrWhiteSpace(request.SourceModule) ? SourceManuel : request.SourceModule.Trim();
        mouvement.SourceId = request.SourceId;
        mouvement.IsAutomatique = request.IsAutomatique;
        mouvement.UtilisateurId = request.UtilisateurId;
        mouvement.UtilisateurNom = string.IsNullOrWhiteSpace(request.UtilisateurNom)
            ? "Utilisateur local"
            : request.UtilisateurNom;
        mouvement.RoleUtilisateur = string.IsNullOrWhiteSpace(request.RoleUtilisateur)
            ? "Caissier"
            : request.RoleUtilisateur;
        mouvement.DateModification = DateTime.UtcNow;
        mouvement.IsActive = true;

        await context.SaveChangesAsync();

        return Ok(mouvement);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var mouvement = await context.BudgetMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        if (mouvement.IsAutomatique)
            return BadRequest("Ce mouvement automatique ne peut pas être supprimé depuis la page Budget.");

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static string? Valider(AppBudgetMouvement mouvement)
    {
        if (mouvement.Montant <= 0)
            return "Le montant est obligatoire.";

        if (string.IsNullOrWhiteSpace(mouvement.TypeMouvement))
            return "Le type de mouvement est obligatoire.";

        if (!EstType(mouvement.TypeMouvement, TypeEntree) &&
            !EstType(mouvement.TypeMouvement, TypeSortie))
            return "Type de mouvement invalide.";

        if (string.IsNullOrWhiteSpace(mouvement.Devise))
            return "La devise est obligatoire.";

        return null;
    }

    private static bool EstSoldeInitial(AppBudgetMouvement mouvement)
    {
        return EstType(mouvement.TypeMouvement, TypeEntree)
               && (
                   mouvement.Motif?.Contains("solde initial", StringComparison.OrdinalIgnoreCase) == true
                   || mouvement.Motif?.Contains("budget", StringComparison.OrdinalIgnoreCase) == true
                   || mouvement.Motif?.Contains("départ", StringComparison.OrdinalIgnoreCase) == true
                   || mouvement.Motif?.Contains("depart", StringComparison.OrdinalIgnoreCase) == true
               );
    }

    private static bool EstType(string? valeur, string type)
    {
        return string.Equals(valeur?.Trim(), type, StringComparison.OrdinalIgnoreCase);
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

using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EpargneMouvementController(AppDbContext context) : ControllerBase
{
    private const string TypeDepot = "Dépôt";
    private const string TypeRetrait = "Retrait";
    private const string DeviseFcfa = "FCFA";

    [HttpGet]
    public async Task<ActionResult<List<AppEpargneMouvement>>> GetAllAsync()
    {
        var mouvements = await context.EpargneMouvements
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("client/{clientId:int}")]
    public async Task<ActionResult<List<AppEpargneMouvement>>> GetByClientAsync(int clientId)
    {
        var mouvements = await context.EpargneMouvements
            .Where(m => m.IsActive && m.ClientId == clientId)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("solde-total/{devise}")]
    public async Task<ActionResult<decimal>> GetSoldeTotalAsync(string devise)
    {
        var solde = await CalculerSoldeAsync(null, devise);
        return Ok(solde);
    }

    [HttpGet("solde-client/{clientId:int}/{devise}")]
    public async Task<ActionResult<decimal>> GetSoldeClientAsync(int clientId, string devise)
    {
        var solde = await CalculerSoldeAsync(clientId, devise);
        return Ok(solde);
    }

    [HttpPost]
    public async Task<ActionResult<AppEpargneMouvement>> CreateAsync([FromBody] AppEpargneMouvement request)
    {
        var validation = await ValiderAsync(request, isUpdate: false, ancienMouvement: null);
        if (!string.IsNullOrWhiteSpace(validation))
            return BadRequest(validation);

        request.Id = 0;
        Nettoyer(request);
        request.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        request.IsActive = true;
        request.DateCreation = DateTime.UtcNow;
        request.DateModification = DateTime.UtcNow;

        context.EpargneMouvements.Add(request);
        await context.SaveChangesAsync();

        return Ok(request);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppEpargneMouvement>> UpdateAsync(int id, [FromBody] AppEpargneMouvement request)
    {
        var mouvement = await context.EpargneMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        var validation = await ValiderAsync(request, isUpdate: true, ancienMouvement: mouvement);
        if (!string.IsNullOrWhiteSpace(validation))
            return BadRequest(validation);

        mouvement.ClientId = request.ClientId;
        mouvement.ClientNom = request.ClientNom?.Trim() ?? string.Empty;
        mouvement.ClientTelephone = request.ClientTelephone?.Trim() ?? string.Empty;
        mouvement.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        mouvement.TypeMouvement = request.TypeMouvement?.Trim() ?? TypeDepot;
        mouvement.Montant = request.Montant;
        mouvement.Devise = string.IsNullOrWhiteSpace(request.Devise) ? DeviseFcfa : request.Devise.Trim();
        mouvement.Motif = request.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = request.Observation?.Trim() ?? string.Empty;
        mouvement.UtilisateurId = request.UtilisateurId;
        mouvement.UtilisateurNom = request.UtilisateurNom?.Trim() ?? string.Empty;
        mouvement.RoleUtilisateur = request.RoleUtilisateur?.Trim() ?? string.Empty;
        mouvement.IsActive = true;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(mouvement);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var mouvement = await context.EpargneMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        if (EstDepot(mouvement.TypeMouvement))
        {
            var solde = await CalculerSoldeAsync(mouvement.ClientId, mouvement.Devise);

            if (solde - mouvement.Montant < 0)
                return BadRequest("Ce dépôt ne peut pas être supprimé car le solde du client deviendrait négatif.");
        }

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> ValiderAsync(
        AppEpargneMouvement mouvement,
        bool isUpdate,
        AppEpargneMouvement? ancienMouvement)
    {
        if (mouvement.ClientId <= 0)
            return "Le client est obligatoire.";

        if (mouvement.Montant <= 0)
            return "Le montant est obligatoire.";

        if (string.IsNullOrWhiteSpace(mouvement.TypeMouvement))
            return "Le type de mouvement est obligatoire.";

        if (!EstDepot(mouvement.TypeMouvement) && !EstRetrait(mouvement.TypeMouvement))
            return "Type de mouvement invalide.";

        var devise = string.IsNullOrWhiteSpace(mouvement.Devise) ? DeviseFcfa : mouvement.Devise.Trim();

        if (EstRetrait(mouvement.TypeMouvement))
        {
            var solde = await CalculerSoldeAsync(mouvement.ClientId, devise);

            if (isUpdate && ancienMouvement != null &&
                ancienMouvement.ClientId == mouvement.ClientId &&
                string.Equals(ancienMouvement.Devise, devise, StringComparison.OrdinalIgnoreCase))
            {
                // On retire l'effet de l'ancienne ligne pour recalculer la disponibilité.
                solde -= GetEffetSolde(ancienMouvement);
            }

            if (mouvement.Montant > solde)
                return "Solde épargne insuffisant.";
        }

        return null;
    }

    private async Task<decimal> CalculerSoldeAsync(int? clientId, string? devise)
    {
        devise = string.IsNullOrWhiteSpace(devise) ? DeviseFcfa : devise.Trim();

        var query = context.EpargneMouvements
            .Where(m => m.IsActive && m.Devise == devise);

        if (clientId.HasValue)
            query = query.Where(m => m.ClientId == clientId.Value);

        var mouvements = await query.ToListAsync();

        return mouvements.Sum(GetEffetSolde);
    }

    private static decimal GetEffetSolde(AppEpargneMouvement mouvement)
    {
        if (EstRetrait(mouvement.TypeMouvement))
            return -mouvement.Montant;

        return mouvement.Montant;
    }

    private static void Nettoyer(AppEpargneMouvement mouvement)
    {
        mouvement.ClientNom = mouvement.ClientNom?.Trim() ?? string.Empty;
        mouvement.ClientTelephone = mouvement.ClientTelephone?.Trim() ?? string.Empty;
        mouvement.TypeMouvement = mouvement.TypeMouvement?.Trim() ?? TypeDepot;
        mouvement.Devise = string.IsNullOrWhiteSpace(mouvement.Devise) ? DeviseFcfa : mouvement.Devise.Trim();
        mouvement.Motif = mouvement.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = mouvement.Observation?.Trim() ?? string.Empty;
        mouvement.UtilisateurNom = mouvement.UtilisateurNom?.Trim() ?? string.Empty;
        mouvement.RoleUtilisateur = mouvement.RoleUtilisateur?.Trim() ?? string.Empty;
    }

    private static bool EstDepot(string? type)
    {
        return string.Equals(type, TypeDepot, StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "Depot", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstRetrait(string? type)
    {
        return string.Equals(type, TypeRetrait, StringComparison.OrdinalIgnoreCase);
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

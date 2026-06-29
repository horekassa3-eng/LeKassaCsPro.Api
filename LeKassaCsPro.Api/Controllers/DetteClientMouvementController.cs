using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DetteClientMouvementController(AppDbContext context) : ControllerBase
{
    private const string TypeDette = "Dette";
    private const string TypePaiement = "Paiement";
    private const string DeviseFcfa = "FCFA";

    [HttpGet]
    public async Task<ActionResult<List<AppDetteClientMouvement>>> GetAllAsync()
    {
        var mouvements = await context.DetteClientMouvements
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("client/{clientId:int}")]
    public async Task<ActionResult<List<AppDetteClientMouvement>>> GetByClientAsync(int clientId)
    {
        var mouvements = await context.DetteClientMouvements
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
    public async Task<ActionResult<AppDetteClientMouvement>> CreateAsync([FromBody] AppDetteClientMouvement request)
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

        context.DetteClientMouvements.Add(request);
        await context.SaveChangesAsync();

        return Ok(request);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppDetteClientMouvement>> UpdateAsync(int id, [FromBody] AppDetteClientMouvement request)
    {
        var mouvement = await context.DetteClientMouvements
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
        mouvement.TypeMouvement = request.TypeMouvement?.Trim() ?? TypeDette;
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
        var mouvement = await context.DetteClientMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        if (EstDette(mouvement.TypeMouvement))
        {
            var solde = await CalculerSoldeAsync(mouvement.ClientId, mouvement.Devise);

            if (solde - mouvement.Montant < 0)
                return BadRequest("Cette dette ne peut pas être supprimée car le solde du client deviendrait négatif.");
        }

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> ValiderAsync(
        AppDetteClientMouvement mouvement,
        bool isUpdate,
        AppDetteClientMouvement? ancienMouvement)
    {
        if (mouvement.ClientId <= 0)
            return "Le client est obligatoire.";

        if (mouvement.Montant <= 0)
            return "Le montant est obligatoire.";

        if (string.IsNullOrWhiteSpace(mouvement.TypeMouvement))
            return "Le type de mouvement est obligatoire.";

        if (!EstDette(mouvement.TypeMouvement) && !EstPaiement(mouvement.TypeMouvement))
            return "Type de mouvement invalide.";

        var devise = string.IsNullOrWhiteSpace(mouvement.Devise) ? DeviseFcfa : mouvement.Devise.Trim();

        if (EstPaiement(mouvement.TypeMouvement))
        {
            var solde = await CalculerSoldeAsync(mouvement.ClientId, devise);

            if (isUpdate && ancienMouvement != null &&
                ancienMouvement.ClientId == mouvement.ClientId &&
                string.Equals(ancienMouvement.Devise, devise, StringComparison.OrdinalIgnoreCase))
            {
                // On retire l'effet de l'ancienne ligne pour recalculer la dette disponible.
                solde -= GetEffetSolde(ancienMouvement);
            }

            if (mouvement.Montant > solde)
                return "Paiement supérieur à la dette actuelle.";
        }

        return null;
    }

    private async Task<decimal> CalculerSoldeAsync(int? clientId, string? devise)
    {
        devise = string.IsNullOrWhiteSpace(devise) ? DeviseFcfa : devise.Trim();

        var query = context.DetteClientMouvements
            .Where(m => m.IsActive && m.Devise == devise);

        if (clientId.HasValue)
            query = query.Where(m => m.ClientId == clientId.Value);

        var mouvements = await query.ToListAsync();

        return mouvements.Sum(GetEffetSolde);
    }

    private static decimal GetEffetSolde(AppDetteClientMouvement mouvement)
    {
        if (EstPaiement(mouvement.TypeMouvement))
            return -mouvement.Montant;

        return mouvement.Montant;
    }

    private static void Nettoyer(AppDetteClientMouvement mouvement)
    {
        mouvement.ClientNom = mouvement.ClientNom?.Trim() ?? string.Empty;
        mouvement.ClientTelephone = mouvement.ClientTelephone?.Trim() ?? string.Empty;
        mouvement.TypeMouvement = mouvement.TypeMouvement?.Trim() ?? TypeDette;
        mouvement.Devise = string.IsNullOrWhiteSpace(mouvement.Devise) ? DeviseFcfa : mouvement.Devise.Trim();
        mouvement.Motif = mouvement.Motif?.Trim() ?? string.Empty;
        mouvement.Observation = mouvement.Observation?.Trim() ?? string.Empty;
        mouvement.UtilisateurNom = mouvement.UtilisateurNom?.Trim() ?? string.Empty;
        mouvement.RoleUtilisateur = mouvement.RoleUtilisateur?.Trim() ?? string.Empty;
    }

    private static bool EstDette(string? type)
    {
        return string.Equals(type, TypeDette, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstPaiement(string? type)
    {
        return string.Equals(type, TypePaiement, StringComparison.OrdinalIgnoreCase)
               || string.Equals(type, "Payement", StringComparison.OrdinalIgnoreCase);
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

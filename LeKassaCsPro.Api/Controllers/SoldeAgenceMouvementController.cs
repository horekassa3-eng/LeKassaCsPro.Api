using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SoldeAgenceMouvementController(AppDbContext context) : ControllerBase
{
    private const string SourceApprovisionnementCode = "ApprovisionnementCode";

    [HttpGet]
    public async Task<ActionResult<List<AppSoldeAgenceMouvement>>> GetAllAsync()
    {
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("pays/{pays}")]
    public async Task<ActionResult<List<AppSoldeAgenceMouvement>>> GetByPaysAsync(string pays)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive
                        && (m.PaysAgence == pays || m.Pays == pays))
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("solde")]
    public async Task<ActionResult<decimal>> GetSoldeAsync(
        [FromQuery] string pays,
        [FromQuery] string moyen,
        [FromQuery] string devise)
    {
        var solde = await GetSoldeAgenceAsync(pays, moyen, devise);
        return Ok(solde);
    }

    [HttpPost]
    public async Task<ActionResult<AppSoldeAgenceMouvement>> SaveAsync([FromBody] AppSoldeAgenceMouvement mouvement)
    {
        if (string.IsNullOrWhiteSpace(mouvement.PaysAgence))
            mouvement.PaysAgence = mouvement.Pays;

        if (string.IsNullOrWhiteSpace(mouvement.Pays))
            mouvement.Pays = mouvement.PaysAgence;

        if (string.IsNullOrWhiteSpace(mouvement.PaysAgence))
            return BadRequest("Le pays est obligatoire.");

        if (string.IsNullOrWhiteSpace(mouvement.Moyen))
            return BadRequest("Le moyen est obligatoire.");

        if (string.IsNullOrWhiteSpace(mouvement.Devise))
            mouvement.Devise = "FCFA";

        if (mouvement.Montant <= 0)
            return BadRequest("Le montant est obligatoire.");

        if (EstPaysGuinee(mouvement.PaysAgence) && !EstMouvementApprovisionnementCode(mouvement))
        {
            return BadRequest(
                "Le solde Guinée ne peut pas être créé manuellement. Utilisez la réception d'approvisionnement par code.");
        }

        if (EstSortie(mouvement.TypeMouvement))
        {
            var solde = await GetSoldeAgenceAsync(
                mouvement.PaysAgence,
                mouvement.Moyen,
                mouvement.Devise);

            if (mouvement.Montant > solde)
                return BadRequest($"Solde insuffisant. Solde disponible : {solde:N0} {mouvement.Devise}.");
        }

        mouvement.Id = 0;
        mouvement.DateMouvement = NormaliserDateUtc(mouvement.DateMouvement);
        mouvement.IsActive = true;
        mouvement.DateCreation = DateTime.UtcNow;
        mouvement.DateModification = DateTime.UtcNow;

        context.SoldeAgenceMouvements.Add(mouvement);
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

        if (EstPaysGuinee(mouvement.PaysAgence) || EstPaysGuinee(mouvement.Pays))
        {
            return BadRequest(
                "Le solde Guinée est géré uniquement par l'approvisionnement code.");
        }

        if (EstEntree(mouvement.TypeMouvement))
        {
            var solde = await GetSoldeAgenceAsync(
                mouvement.PaysAgence,
                mouvement.Moyen,
                mouvement.Devise);

            if (solde - mouvement.Montant < 0)
                return BadRequest("Ce mouvement ne peut pas être supprimé, car le solde deviendrait négatif.");
        }

        mouvement.IsActive = false;
        mouvement.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<decimal> GetSoldeAgenceAsync(string pays, string moyen, string devise)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive
            && (NormaliserTexte(m.PaysAgence) == NormaliserTexte(pays)
                || NormaliserTexte(m.Pays) == NormaliserTexte(pays))
            && m.Moyen == moyen
            && m.Devise == devise)
            .ToListAsync();

        var entrees = mouvements
            .Where(m => EstEntree(m.TypeMouvement))
            .Sum(m => m.Montant);

        var sorties = mouvements
            .Where(m => EstSortie(m.TypeMouvement))
            .Sum(m => m.Montant);

        return entrees - sorties;
    }

    private static bool EstEntree(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Entrée", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Entree", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Approvisionnement reçu", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSortie(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Sortie", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Retrait code", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi code", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Approvisionnement envoyé", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstMouvementApprovisionnementCode(AppSoldeAgenceMouvement mouvement)
    {
        return string.Equals(
            mouvement.SourceModule?.Trim(),
            SourceApprovisionnementCode,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstPaysGuinee(string? pays)
    {
        return NormaliserTexte(pays).Contains("guinee", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormaliserTexte(string? valeur)
    {
        return (valeur ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace("é", "e")
            .Replace("è", "e")
            .Replace("ê", "e")
            .Replace("ë", "e")
            .Replace("ï", "i")
            .Replace("î", "i");
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

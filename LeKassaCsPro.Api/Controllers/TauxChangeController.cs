using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TauxChangeController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppTauxChange>>> GetAllAsync()
    {
        var taux = await context.TauxChanges
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.DateTaux)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        return Ok(taux);
    }

    [HttpGet("actif")]
    public async Task<ActionResult<AppTauxChange?>> GetActifAsync()
    {
        var taux = await context.TauxChanges
            .Where(t => t.IsActive && t.IsActif)
            .OrderByDescending(t => t.DateTaux)
            .ThenByDescending(t => t.Id)
            .FirstOrDefaultAsync();

        return Ok(taux);
    }

    [HttpPost]
    public async Task<ActionResult<AppTauxChange>> SaveAsync([FromBody] AppTauxChange request)
    {
        if (request.MontantReferenceFcfa <= 0 || request.MontantEquivalentGnf <= 0)
            return BadRequest("Le montant référence et le montant équivalent sont obligatoires.");

        request.DateTaux = NormaliserDateUtc(request.DateTaux);
        request.TauxGnfParFcfa = request.MontantEquivalentGnf / request.MontantReferenceFcfa;
        request.IsActive = true;

        if (request.FraisServiceSnGnPourcentage < 0)
            request.FraisServiceSnGnPourcentage = 0;

        if (request.FraisServiceGnSnPourcentage < 0)
            request.FraisServiceGnSnPourcentage = 0;

        if (request.FraisFournisseurPour5000Fcfa < 0)
            request.FraisFournisseurPour5000Fcfa = 0;

        if (request.IsActif)
            await DesactiverAutresTauxAsync(request.Id);

        if (request.Id == 0)
        {
            context.TauxChanges.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var taux = await context.TauxChanges
            .FirstOrDefaultAsync(t => t.Id == request.Id);

        if (taux == null)
            return NotFound();

        taux.DateTaux = request.DateTaux;
        taux.MontantReferenceFcfa = request.MontantReferenceFcfa;
        taux.MontantEquivalentGnf = request.MontantEquivalentGnf;
        taux.TauxGnfParFcfa = request.TauxGnfParFcfa;
        taux.FraisServiceSnGnPourcentage = request.FraisServiceSnGnPourcentage;
        taux.FraisServiceGnSnPourcentage = request.FraisServiceGnSnPourcentage;
        taux.FraisFournisseurPour5000Fcfa = request.FraisFournisseurPour5000Fcfa;
        taux.IsActif = request.IsActif;
        taux.IsActive = true;

        await context.SaveChangesAsync();

        return Ok(taux);
    }

    [HttpPut("{id:int}/actif")]
    public async Task<ActionResult<AppTauxChange>> DefinirActifAsync(int id)
    {
        var taux = await context.TauxChanges
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (taux == null)
            return NotFound();

        await DesactiverAutresTauxAsync(id);

        taux.IsActif = true;
        await context.SaveChangesAsync();

        return Ok(taux);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var taux = await context.TauxChanges
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (taux == null)
            return NotFound();

        taux.IsActive = false;
        taux.IsActif = false;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task DesactiverAutresTauxAsync(int idActuel)
    {
        var anciens = await context.TauxChanges
            .Where(t => t.IsActive && t.IsActif && t.Id != idActuel)
            .ToListAsync();

        foreach (var ancien in anciens)
            ancien.IsActif = false;
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }
}
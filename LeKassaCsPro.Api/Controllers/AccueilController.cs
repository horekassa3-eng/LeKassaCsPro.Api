using LeKassaCsPro.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccueilController(AppDbContext context) : ControllerBase
{
    [HttpGet("apercu-rapide")]
    public async Task<ActionResult<AccueilApercuRapideResponse>> GetApercuRapideAsync(
        [FromQuery] DateTime? debut,
        [FromQuery] DateTime? fin)
    {
        var debutUtc = NormaliserDateUtc(debut ?? DateTime.UtcNow.Date);
        var finUtc = NormaliserDateUtc(fin ?? debutUtc.AddDays(1));

        if (finUtc <= debutUtc)
            finUtc = debutUtc.AddDays(1);

        var totalBoutiquesInventaires = await context.BoutiqueInventaires
            .AsNoTracking()
            .CountAsync(i => i.IsActive
                             && i.DateInventaire >= debutUtc
                             && i.DateInventaire < finUtc);

        var totalBoutiquesMouvements = await context.BoutiqueBudgetMouvements
            .AsNoTracking()
            .CountAsync(m => m.IsActive
                             && m.DateMouvement >= debutUtc
                             && m.DateMouvement < finUtc);

        var totalServices = await context.RecettesServices
            .AsNoTracking()
            .CountAsync(r => r.IsActive
                             && r.DateRecette >= debutUtc
                             && r.DateRecette < finUtc);

        var totalVentes = await context.Ventes
            .AsNoTracking()
            .CountAsync(v => v.IsActive
                             && v.DateVente >= debutUtc
                             && v.DateVente < finUtc);

        return Ok(new AccueilApercuRapideResponse
        {
            TotalBoutiques = totalBoutiquesInventaires + totalBoutiquesMouvements,
            TotalServices = totalServices,
            TotalVentes = totalVentes
        });
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

public class AccueilApercuRapideResponse
{
    public int TotalBoutiques { get; set; }

    public int TotalServices { get; set; }

    public int TotalVentes { get; set; }
}

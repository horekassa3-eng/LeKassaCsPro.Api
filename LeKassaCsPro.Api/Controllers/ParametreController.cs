using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParametreController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppParametre>>> GetAllAsync()
    {
        var parametres = await context.Parametres
            .Where(p => p.IsActive)
            .OrderBy(p => p.Cle)
            .ToListAsync();

        return Ok(parametres);
    }

    [HttpGet("{cle}")]
    public async Task<ActionResult<string>> GetAsync(string cle, [FromQuery] string? valeurDefaut = null)
    {
        cle = NormaliserCle(cle);

        if (string.IsNullOrWhiteSpace(cle))
            return Ok(valeurDefaut ?? string.Empty);

        var parametre = await context.Parametres
            .Where(p => p.IsActive && p.Cle == cle)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        return Ok(parametre?.Valeur ?? valeurDefaut ?? string.Empty);
    }

    [HttpPost]
    public async Task<ActionResult<AppParametre>> SaveAsync([FromBody] AppParametre request)
    {
        var cle = NormaliserCle(request.Cle);

        if (string.IsNullOrWhiteSpace(cle))
            return BadRequest("La clé du paramètre est obligatoire.");

        var valeur = request.Valeur?.Trim() ?? string.Empty;

        var parametre = await context.Parametres
            .Where(p => p.IsActive && p.Cle == cle)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (parametre == null)
        {
            parametre = new AppParametre
            {
                Cle = cle,
                Valeur = valeur,
                IsActive = true,
                DateCreation = DateTime.UtcNow,
                DateModification = DateTime.UtcNow
            };

            context.Parametres.Add(parametre);
        }
        else
        {
            parametre.Valeur = valeur;
            parametre.IsActive = true;
            parametre.DateModification = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return Ok(parametre);
    }

    [HttpPut("{cle}")]
    public async Task<ActionResult<AppParametre>> UpdateAsync(string cle, [FromBody] AppParametre request)
    {
        request.Cle = cle;
        return await SaveAsync(request);
    }

    [HttpDelete("{cle}")]
    public async Task<IActionResult> DeleteAsync(string cle)
    {
        cle = NormaliserCle(cle);

        if (string.IsNullOrWhiteSpace(cle))
            return BadRequest("La clé du paramètre est obligatoire.");

        var parametre = await context.Parametres
            .Where(p => p.IsActive && p.Cle == cle)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (parametre == null)
            return NotFound();

        parametre.IsActive = false;
        parametre.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static string NormaliserCle(string? cle)
    {
        return cle?.Trim() ?? string.Empty;
    }
}

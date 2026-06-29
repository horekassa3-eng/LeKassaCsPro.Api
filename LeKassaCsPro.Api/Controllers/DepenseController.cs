using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepenseController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppDepense>>> GetAllAsync()
    {
        var depenses = await context.Depenses
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.DateDepense)
            .ToListAsync();

        return Ok(depenses);
    }

    [HttpPost]
    public async Task<ActionResult<AppDepense>> SaveAsync([FromBody] AppDepense depense)
    {
        if (string.IsNullOrWhiteSpace(depense.Categorie))
            return BadRequest("La catégorie est obligatoire.");

        if (string.IsNullOrWhiteSpace(depense.Description))
            return BadRequest("La description est obligatoire.");

        if (depense.Montant <= 0)
            return BadRequest("Le montant est obligatoire.");

        depense.Id = 0;
        depense.DateDepense = NormaliserDateUtc(depense.DateDepense);
        depense.DateCreation = DateTime.UtcNow;
        depense.DateModification = DateTime.UtcNow;
        depense.IsActive = true;

        context.Depenses.Add(depense);
        await context.SaveChangesAsync();

        return Ok(depense);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppDepense>> UpdateAsync(int id, [FromBody] AppDepense request)
    {
        var depense = await context.Depenses
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

        if (depense == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Categorie))
            return BadRequest("La catégorie est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("La description est obligatoire.");

        if (request.Montant <= 0)
            return BadRequest("Le montant est obligatoire.");

        depense.DateDepense = NormaliserDateUtc(request.DateDepense);
        depense.Categorie = request.Categorie;
        depense.Description = request.Description;
        depense.Montant = request.Montant;
        depense.Devise = request.Devise;
        depense.ModePaiement = request.ModePaiement;
        depense.UtilisateurId = request.UtilisateurId;
        depense.UtilisateurNom = request.UtilisateurNom;
        depense.RoleUtilisateur = request.RoleUtilisateur;
        depense.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(depense);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var depense = await context.Depenses
            .FirstOrDefaultAsync(d => d.Id == id && d.IsActive);

        if (depense == null)
            return NotFound();

        depense.IsActive = false;
        depense.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

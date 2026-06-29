using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FournisseurController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppFournisseur>>> GetAllAsync()
    {
        var fournisseurs = await context.Fournisseurs
            .Where(f => f.IsActive)
            .OrderBy(f => f.Nom)
            .ToListAsync();

        return Ok(fournisseurs);
    }

    [HttpPost]
    public async Task<ActionResult<AppFournisseur>> SaveAsync([FromBody] AppFournisseur request)
    {
        if (string.IsNullOrWhiteSpace(request.Nom))
            return BadRequest("Le nom du fournisseur est obligatoire.");

        if (request.Id == 0)
        {
            request.DateCreation = DateTime.UtcNow;
            request.DateModification = DateTime.UtcNow;
            request.IsActive = true;

            context.Fournisseurs.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var fournisseur = await context.Fournisseurs
            .FirstOrDefaultAsync(f => f.Id == request.Id);

        if (fournisseur == null)
            return NotFound();

        fournisseur.Nom = request.Nom;
        fournisseur.Telephone = request.Telephone;
        fournisseur.Pays = request.Pays;
        fournisseur.Ville = request.Ville;
        fournisseur.Observation = request.Observation;
        fournisseur.IsActive = true;
        fournisseur.UtilisateurId = request.UtilisateurId;
        fournisseur.UtilisateurNom = request.UtilisateurNom;
        fournisseur.RoleUtilisateur = request.RoleUtilisateur;
        fournisseur.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(fournisseur);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var fournisseur = await context.Fournisseurs
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

        if (fournisseur == null)
            return NotFound();

        fournisseur.IsActive = false;
        fournisseur.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }
}

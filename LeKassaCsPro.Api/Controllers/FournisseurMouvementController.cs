using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FournisseurMouvementController(AppDbContext context) : ControllerBase
{
    private const string TypeApprovisionnement = "Approvisionnement";
    private const string TypeUtilisationTransfert = "Utilisation transfert";

    [HttpGet("fournisseur/{fournisseurId:int}")]
    public async Task<ActionResult<List<AppFournisseurMouvement>>> GetByFournisseurAsync(int fournisseurId)
    {
        var mouvements = await context.FournisseurMouvements
            .Where(m => m.IsActive && m.FournisseurId == fournisseurId)
            .OrderByDescending(m => m.DateMouvement)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mouvements);
    }

    [HttpGet("solde/{fournisseurId:int}/{devise}")]
    public async Task<ActionResult<decimal>> GetSoldeAsync(int fournisseurId, string devise)
    {
        var solde = await CalculerSoldeAsync(fournisseurId, devise);
        return Ok(solde);
    }

    [HttpPost]
    public async Task<ActionResult<AppFournisseurMouvement>> SaveAsync([FromBody] AppFournisseurMouvement request)
    {
        if (request.FournisseurId <= 0)
            return BadRequest("Le fournisseur est obligatoire.");

        if (string.IsNullOrWhiteSpace(request.TypeMouvement))
            return BadRequest("Le type de mouvement est obligatoire.");

        if (request.TypeMouvement != TypeApprovisionnement &&
            request.TypeMouvement != TypeUtilisationTransfert)
            return BadRequest("Type de mouvement invalide.");

        if (string.IsNullOrWhiteSpace(request.Devise))
            return BadRequest("La devise est obligatoire.");

        if (request.Montant <= 0)
            return BadRequest("Le montant est obligatoire.");

        request.DateMouvement = NormaliserDateUtc(request.DateMouvement);
        request.IsActive = true;

        if (request.Id == 0)
        {
            context.FournisseurMouvements.Add(request);
            await context.SaveChangesAsync();

            return Ok(request);
        }

        var mouvement = await context.FournisseurMouvements
            .FirstOrDefaultAsync(m => m.Id == request.Id);

        if (mouvement == null)
            return NotFound();

        mouvement.FournisseurId = request.FournisseurId;
        mouvement.DateMouvement = request.DateMouvement;
        mouvement.TypeMouvement = request.TypeMouvement;
        mouvement.Devise = request.Devise;
        mouvement.MoyenPaiement = request.MoyenPaiement;
        mouvement.Montant = request.Montant;
        mouvement.FraisFournisseur = request.FraisFournisseur;
        mouvement.MontantGnfEnvoye = request.MontantGnfEnvoye;
        mouvement.TauxGnfParFcfa = request.TauxGnfParFcfa;
        mouvement.Observation = request.Observation;
        mouvement.UtilisateurId = request.UtilisateurId;
        mouvement.UtilisateurNom = request.UtilisateurNom;
        mouvement.RoleUtilisateur = request.RoleUtilisateur;
        mouvement.IsActive = true;

        await context.SaveChangesAsync();

        return Ok(mouvement);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var mouvement = await context.FournisseurMouvements
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (mouvement == null)
            return NotFound();

        mouvement.IsActive = false;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<decimal> CalculerSoldeAsync(int fournisseurId, string devise)
    {
        var mouvements = await context.FournisseurMouvements
            .Where(m => m.IsActive &&
                        m.FournisseurId == fournisseurId &&
                        m.Devise == devise)
            .ToListAsync();

        var entrees = mouvements
            .Where(m => EstEntreeSoldeFournisseur(m.TypeMouvement))
            .Sum(m => m.Montant);

        var sorties = mouvements
            .Where(m => EstSortieSoldeFournisseur(m.TypeMouvement))
            .Sum(m => m.Montant);

        return entrees - sorties;
    }

    private static bool EstEntreeSoldeFournisseur(string? typeMouvement)
    {
        return string.Equals(typeMouvement, TypeApprovisionnement, StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Entree client Guinee", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Entrée client Guinée", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Réception", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Reception", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Retrait", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Retrait code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSortieSoldeFournisseur(string? typeMouvement)
    {
        return string.Equals(typeMouvement, TypeUtilisationTransfert, StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi code", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

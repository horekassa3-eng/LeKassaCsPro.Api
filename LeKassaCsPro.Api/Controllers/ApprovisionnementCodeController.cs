using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovisionnementCodeController(AppDbContext context) : ControllerBase
{
    private const string StatutEnAttente = "En attente";
    private const string StatutAnnule = "Annule";
    private const string StatutAnnuleAccent = "Annulé";
    private const string StatutRecu = "Recu";
    private const string StatutRecuAccent = "Reçu";

    [HttpGet]
    public async Task<ActionResult<List<AppApprovisionnementCode>>> GetAllAsync()
    {
        var codes = await context.ApprovisionnementCodes
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DateCreationCode)
            .ToListAsync();

        return Ok(codes);
    }

    [HttpGet("{codeUnique}")]
    public async Task<ActionResult<AppApprovisionnementCode>> GetByCodeAsync(string codeUnique)
    {
        var code = NormaliserCode(codeUnique);

        var approvisionnement = await context.ApprovisionnementCodes
            .FirstOrDefaultAsync(c => c.IsActive && c.CodeUnique == code);

        if (approvisionnement == null)
            return NotFound();

        return Ok(approvisionnement);
    }

    [HttpPost]
    public async Task<ActionResult<AppApprovisionnementCode>> SaveAsync([FromBody] AppApprovisionnementCode request)
    {
        if (request.Montant <= 0)
            return BadRequest("Le montant est obligatoire.");

        if (request.FraisFournisseur < 0)
            request.FraisFournisseur = 0;

        request.CodeUnique = NormaliserCode(request.CodeUnique);
        request.Devise = string.IsNullOrWhiteSpace(request.Devise) ? "FCFA" : request.Devise.Trim();
        request.Statut = string.IsNullOrWhiteSpace(request.Statut) ? StatutEnAttente : request.Statut.Trim();

        if (string.IsNullOrWhiteSpace(request.CodeUnique))
            return BadRequest("Le code est obligatoire.");

        var existe = await context.ApprovisionnementCodes
            .AnyAsync(c => c.IsActive && c.CodeUnique == request.CodeUnique && c.Id != request.Id);

        if (existe)
            return BadRequest("Ce code existe déjà.");

        var totalSortie = request.Montant + request.FraisFournisseur;
        var soldeOrigine = await GetSoldeAgenceAsync(request.PaysOrigine, "Espèces", "FCFA");

        if (request.Id == 0 && totalSortie > soldeOrigine)
            return BadRequest($"Solde insuffisant. Solde disponible : {soldeOrigine:N0} FCFA.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        AppApprovisionnementCode approvisionnement;

        if (request.Id == 0)
        {
            request.DateCreationCode = NormaliserDateUtc(request.DateCreationCode);
            request.DerniereModification = DateTime.UtcNow;
            request.DateCreation = DateTime.UtcNow;
            request.DateModification = DateTime.UtcNow;
            request.IsActive = true;
            request.EstSynchronise = false;

            if (string.IsNullOrWhiteSpace(request.SyncId))
                request.SyncId = Guid.NewGuid().ToString();

            context.ApprovisionnementCodes.Add(request);
            await context.SaveChangesAsync();

            approvisionnement = request;
        }
        else
        {
            approvisionnement = await context.ApprovisionnementCodes
                .FirstOrDefaultAsync(c => c.Id == request.Id && c.IsActive)
                ?? throw new InvalidOperationException("Code approvisionnement introuvable.");

            approvisionnement.PaysOrigine = request.PaysOrigine;
            approvisionnement.PaysDestination = request.PaysDestination;
            approvisionnement.Montant = request.Montant;
            approvisionnement.FraisFournisseur = request.FraisFournisseur;
            approvisionnement.Devise = request.Devise;
            approvisionnement.Statut = request.Statut;
            approvisionnement.DateReception = request.DateReception.HasValue
                ? NormaliserDateUtc(request.DateReception.Value)
                : null;
            approvisionnement.Observation = request.Observation;
            approvisionnement.DerniereModification = DateTime.UtcNow;
            approvisionnement.EstSynchronise = false;
            approvisionnement.UtilisateurReceptionId = request.UtilisateurReceptionId;
            approvisionnement.UtilisateurReceptionNom = request.UtilisateurReceptionNom;
            approvisionnement.RoleUtilisateurReception = request.RoleUtilisateurReception;
            approvisionnement.DateModification = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }

        await SynchroniserMouvementsSoldeAsync(approvisionnement);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(approvisionnement);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var approvisionnement = await context.ApprovisionnementCodes
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (approvisionnement == null)
            return NotFound();

        await using var transaction = await context.Database.BeginTransactionAsync();

        approvisionnement.IsActive = false;
        approvisionnement.DerniereModification = DateTime.UtcNow;
        approvisionnement.DateModification = DateTime.UtcNow;

        await DesactiverMouvementsSoldeAsync(approvisionnement.Id);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    private async Task SynchroniserMouvementsSoldeAsync(AppApprovisionnementCode approvisionnement)
    {
        await DesactiverMouvementsSoldeAsync(approvisionnement.Id);

        if (!approvisionnement.IsActive
            || approvisionnement.Montant <= 0
            || EstAnnule(approvisionnement.Statut))
        {
            return;
        }

        var frais = Math.Max(0, approvisionnement.FraisFournisseur);
        var totalSortie = approvisionnement.Montant + frais;

        context.SoldeAgenceMouvements.Add(new AppSoldeAgenceMouvement
        {
            PaysAgence = approvisionnement.PaysOrigine,
            Pays = approvisionnement.PaysOrigine,
            Moyen = "Espèces",
            DateMouvement = approvisionnement.DateCreationCode,
            TypeMouvement = "Sortie",
            Montant = totalSortie,
            Devise = "FCFA",
            Motif = $"Approvisionnement envoyé par code {approvisionnement.CodeUnique}",
            Observation = $"Approvisionnement envoyé - {approvisionnement.PaysOrigine} vers {approvisionnement.PaysDestination} | Montant {approvisionnement.Montant:N0} FCFA | Frais fournisseur {frais:N0} FCFA | ApprovisionnementCode #{approvisionnement.Id}",
            SourceModule = "ApprovisionnementCode",
            SourceId = approvisionnement.Id,
            IsAutomatique = true,
            IsActive = true,
            UtilisateurId = approvisionnement.UtilisateurId,
            UtilisateurNom = approvisionnement.UtilisateurNom,
            RoleUtilisateur = approvisionnement.RoleUtilisateur,
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        if (!EstRecu(approvisionnement.Statut))
            return;

        context.SoldeAgenceMouvements.Add(new AppSoldeAgenceMouvement
        {
            PaysAgence = approvisionnement.PaysDestination,
            Pays = approvisionnement.PaysDestination,
            Moyen = "Espèces",
            DateMouvement = approvisionnement.DateReception ?? DateTime.UtcNow,
            TypeMouvement = "Entrée",
            Montant = approvisionnement.Montant,
            Devise = "FCFA",
            Motif = $"Approvisionnement par code {approvisionnement.CodeUnique}",
            Observation = $"Approvisionnement reçu - {approvisionnement.PaysOrigine} vers {approvisionnement.PaysDestination} | ApprovisionnementCode #{approvisionnement.Id}",
            SourceModule = "ApprovisionnementCode",
            SourceId = approvisionnement.Id,
            IsAutomatique = true,
            IsActive = true,
            UtilisateurId = approvisionnement.UtilisateurReceptionId,
            UtilisateurNom = approvisionnement.UtilisateurReceptionNom,
            RoleUtilisateur = approvisionnement.RoleUtilisateurReception,
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });
    }

    private async Task DesactiverMouvementsSoldeAsync(int approvisionnementId)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive
                        && m.SourceModule == "ApprovisionnementCode"
                        && m.SourceId == approvisionnementId)
            .ToListAsync();

        foreach (var mouvement in mouvements)
        {
            mouvement.IsActive = false;
            mouvement.DateModification = DateTime.UtcNow;
        }
    }

    private async Task<decimal> GetSoldeAgenceAsync(string pays, string moyen, string devise)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive
                        && (m.PaysAgence == pays || m.Pays == pays)
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
               || string.Equals(typeMouvement, "Retrait code", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Approvisionnement reçu", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSortie(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Sortie", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi code", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Approvisionnement envoyé", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstAnnule(string? statut)
    {
        return string.Equals(statut, StatutAnnule, StringComparison.OrdinalIgnoreCase)
               || string.Equals(statut, StatutAnnuleAccent, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstRecu(string? statut)
    {
        return string.Equals(statut, StatutRecu, StringComparison.OrdinalIgnoreCase)
               || string.Equals(statut, StatutRecuAccent, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormaliserCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

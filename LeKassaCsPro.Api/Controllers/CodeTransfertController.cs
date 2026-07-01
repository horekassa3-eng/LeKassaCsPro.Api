using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeTransfertController(AppDbContext context) : ControllerBase
{
    private const string StatutAnnule = "Annule";
    private const string StatutAnnuleAccent = "Annulé";
    private const string StatutRetire = "Retire";
    private const string StatutRetireAccent = "Retiré";
    private const string SourceCodeTransfert = "CodeTransfert";

    [HttpGet]
    public async Task<ActionResult<List<AppCodeTransfert>>> GetAllAsync()
    {
        var codes = await context.CodeTransferts
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DateEnvoi)
            .ToListAsync();

        return Ok(codes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppCodeTransfert>> GetByIdAsync(int id)
    {
        var code = await context.CodeTransferts
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (code == null)
            return NotFound();

        return Ok(code);
    }

    [HttpGet("{codeUnique}")]
    [HttpGet("code/{codeUnique}")]
    public async Task<ActionResult<AppCodeTransfert>> GetByCodeAsync(string codeUnique)
    {
        if (string.IsNullOrWhiteSpace(codeUnique))
            return BadRequest("Code obligatoire.");

        var codeNormalise = NormaliserCode(codeUnique);

        var code = await context.CodeTransferts
            .FirstOrDefaultAsync(c => c.IsActive && c.CodeUnique.ToUpper() == codeNormalise);

        if (code == null)
            return NotFound();

        return Ok(code);
    }

    [HttpPost]
    public async Task<ActionResult<AppCodeTransfert>> CreateAsync(AppCodeTransfert code)
    {
        if (string.IsNullOrWhiteSpace(code.CodeUnique))
            return BadRequest("Code obligatoire.");

        if (code.Montant <= 0)
            return BadRequest("Le montant est obligatoire.");

        var codeNormalise = NormaliserCode(code.CodeUnique);

        var existe = await context.CodeTransferts
            .AnyAsync(c => c.IsActive && c.CodeUnique.ToUpper() == codeNormalise);

        if (existe)
            return BadRequest("Ce code existe déjà.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        code.Id = 0;
        code.CodeUnique = codeNormalise;
        NormaliserCodeTransfert(code);
        code.DateCreation = DateTime.UtcNow;
        code.DateModification = DateTime.UtcNow;
        code.IsActive = true;

        context.CodeTransferts.Add(code);
        await context.SaveChangesAsync();

        await SynchroniserMouvementsSoldeAsync(code);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(code);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppCodeTransfert>> UpdateAsync(int id, AppCodeTransfert request)
    {
        var code = await context.CodeTransferts
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (code == null)
            return NotFound();

        var codeNormalise = string.IsNullOrWhiteSpace(request.CodeUnique)
            ? NormaliserCode(code.CodeUnique)
            : NormaliserCode(request.CodeUnique);

        var existe = await context.CodeTransferts
            .AnyAsync(c => c.Id != id && c.IsActive && c.CodeUnique.ToUpper() == codeNormalise);

        if (existe)
            return BadRequest("Ce code existe déjà.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        code.CodeUnique = codeNormalise;
        code.NomEnvoyeur = request.NomEnvoyeur?.Trim() ?? string.Empty;
        code.TelephoneEnvoyeur = request.TelephoneEnvoyeur?.Trim() ?? string.Empty;
        code.NomBeneficiaire = request.NomBeneficiaire?.Trim() ?? string.Empty;
        code.TelephoneBeneficiaire = request.TelephoneBeneficiaire?.Trim() ?? string.Empty;
        code.AdresseBeneficiaire = request.AdresseBeneficiaire?.Trim() ?? string.Empty;
        code.PaysEnvoi = request.PaysEnvoi?.Trim() ?? string.Empty;
        code.PaysRetrait = request.PaysRetrait?.Trim() ?? string.Empty;
        code.Montant = request.Montant;
        code.Frais = request.Frais;
        code.TotalPaye = request.TotalPaye;
        code.Statut = string.IsNullOrWhiteSpace(request.Statut) ? code.Statut : request.Statut.Trim();
        code.DateEnvoi = NormaliserDateUtc(request.DateEnvoi == default ? code.DateEnvoi : request.DateEnvoi);
        code.DateRetrait = NormaliserDateUtcNullable(request.DateRetrait);
        code.Observation = request.Observation?.Trim() ?? string.Empty;
        code.UtilisateurEnvoiId = request.UtilisateurEnvoiId;
        code.UtilisateurEnvoiNom = request.UtilisateurEnvoiNom?.Trim() ?? string.Empty;
        code.RoleUtilisateurEnvoi = request.RoleUtilisateurEnvoi?.Trim() ?? string.Empty;
        code.UtilisateurRetraitId = request.UtilisateurRetraitId;
        code.UtilisateurRetraitNom = request.UtilisateurRetraitNom?.Trim() ?? string.Empty;
        code.RoleUtilisateurRetrait = request.RoleUtilisateurRetrait?.Trim() ?? string.Empty;
        code.DateModification = DateTime.UtcNow;

        NormaliserCodeTransfert(code);

        await context.SaveChangesAsync();

        await SynchroniserMouvementsSoldeAsync(code);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(code);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var code = await context.CodeTransferts
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (code == null)
            return NotFound();

        await using var transaction = await context.Database.BeginTransactionAsync();

        code.IsActive = false;
        code.DateModification = DateTime.UtcNow;

        await DesactiverMouvementsSoldeAsync(code.Id);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }

    private async Task SynchroniserMouvementsSoldeAsync(AppCodeTransfert code)
    {
        await DesactiverMouvementsSoldeAsync(code.Id);

        if (!code.IsActive
            || code.Montant <= 0
            || EstAnnule(code.Statut)
            || !EstRetire(code.Statut))
        {
            return;
        }

        context.SoldeAgenceMouvements.Add(new AppSoldeAgenceMouvement
        {
            PaysAgence = code.PaysRetrait,
            Pays = code.PaysRetrait,
            Moyen = "Espèces",
            DateMouvement = code.DateRetrait ?? DateTime.UtcNow,
            TypeMouvement = "Retrait code",
            Montant = code.Montant,
            Devise = "FCFA",
            Motif = $"Retrait espèces - code {code.CodeUnique} - {code.PaysRetrait}",
            Observation = $"Retrait espèces - code {code.CodeUnique} - {code.PaysRetrait} | CodeTransfert #{code.Id}",
            SourceModule = SourceCodeTransfert,
            SourceId = code.Id,
            IsAutomatique = true,
            IsActive = true,
            UtilisateurId = code.UtilisateurRetraitId,
            UtilisateurNom = string.IsNullOrWhiteSpace(code.UtilisateurRetraitNom)
                ? "Agent Guinée"
                : code.UtilisateurRetraitNom,
            RoleUtilisateur = string.IsNullOrWhiteSpace(code.RoleUtilisateurRetrait)
                ? "Agent"
                : code.RoleUtilisateurRetrait,
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });
    }
    private async Task DesactiverMouvementsSoldeAsync(int codeId)
    {
        var mouvements = await context.SoldeAgenceMouvements
            .Where(m => m.IsActive
                        && m.SourceModule == SourceCodeTransfert
                        && m.SourceId == codeId)
            .ToListAsync();

        foreach (var mouvement in mouvements)
        {
            mouvement.IsActive = false;
            mouvement.DateModification = DateTime.UtcNow;
        }
    }

    private static void NormaliserCodeTransfert(AppCodeTransfert code)
    {
        code.Statut = string.IsNullOrWhiteSpace(code.Statut)
            ? "En attente"
            : code.Statut.Trim();

        code.PaysEnvoi = string.IsNullOrWhiteSpace(code.PaysEnvoi)
            ? "Sénégal"
            : code.PaysEnvoi.Trim();

        code.PaysRetrait = string.IsNullOrWhiteSpace(code.PaysRetrait)
            ? "Guinée"
            : code.PaysRetrait.Trim();

        code.DateEnvoi = NormaliserDateUtc(code.DateEnvoi);
        code.DateRetrait = NormaliserDateUtcNullable(code.DateRetrait);
    }

    private static bool EstAnnule(string? statut)
    {
        return string.Equals(statut, StatutAnnule, StringComparison.OrdinalIgnoreCase)
               || string.Equals(statut, StatutAnnuleAccent, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstRetire(string? statut)
    {
        return string.Equals(statut, StatutRetire, StringComparison.OrdinalIgnoreCase)
               || string.Equals(statut, StatutRetireAccent, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormaliserCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
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

    private static DateTime? NormaliserDateUtcNullable(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return NormaliserDateUtc(date.Value);
    }
}
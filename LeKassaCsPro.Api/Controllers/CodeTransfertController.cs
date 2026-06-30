using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeTransfertController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppCodeTransfert>>> GetAllAsync()
    {
        var codes = await context.CodeTransferts
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DateEnvoi)
            .ToListAsync();

        return Ok(codes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppCodeTransfert>> GetByIdAsync(int id)
    {
        var code = await context.CodeTransferts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (code == null)
            return NotFound();

        return Ok(code);
    }

    [HttpGet("code/{codeUnique}")]
    public async Task<ActionResult<AppCodeTransfert>> GetByCodeAsync(string codeUnique)
    {
        if (string.IsNullOrWhiteSpace(codeUnique))
            return BadRequest("Code invalide.");

        var codeNormalise = codeUnique.Trim();

        var code = await context.CodeTransferts
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.IsActive &&
                c.CodeUnique.ToLower() == codeNormalise.ToLower());

        if (code == null)
            return NotFound();

        return Ok(code);
    }

    [HttpGet("en-attente/{paysRetrait}")]
    public async Task<ActionResult<decimal>> GetMontantEnAttenteAsync(string paysRetrait)
    {
        if (string.IsNullOrWhiteSpace(paysRetrait))
            return Ok(0m);

        var pays = paysRetrait.Trim();

        var total = await context.CodeTransferts
            .AsNoTracking()
            .Where(c =>
                c.IsActive &&
                c.Statut.ToLower() == "en attente" &&
                c.PaysRetrait.ToLower() == pays.ToLower())
            .SumAsync(c => c.Montant);

        return Ok(total);
    }

    [HttpPost]
    public async Task<ActionResult<AppCodeTransfert>> CreateAsync([FromBody] AppCodeTransfert code)
    {
        if (code == null)
            return BadRequest("Données invalides.");

        if (string.IsNullOrWhiteSpace(code.CodeUnique))
            return BadRequest("Code obligatoire.");

        if (code.Montant <= 0)
            return BadRequest("Montant obligatoire.");

        var existe = await context.CodeTransferts.AnyAsync(c =>
            c.IsActive &&
            c.CodeUnique.ToLower() == code.CodeUnique.Trim().ToLower());

        if (existe)
            return BadRequest("Ce code existe déjà.");

        code.Id = 0;
        Nettoyer(code);
        code.DateEnvoi = NormaliserDateUtc(code.DateEnvoi);
        code.DateRetrait = NormaliserDateNullableUtc(code.DateRetrait);
        code.DateCreation = DateTime.UtcNow;
        code.DateModification = DateTime.UtcNow;
        code.IsActive = true;

        context.CodeTransferts.Add(code);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = code.Id }, code);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppCodeTransfert>> UpdateAsync(int id, [FromBody] AppCodeTransfert code)
    {
        if (code == null || id <= 0)
            return BadRequest("Données invalides.");

        var existant = await context.CodeTransferts.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (existant == null)
            return NotFound();

        var codeUnique = string.IsNullOrWhiteSpace(code.CodeUnique)
            ? existant.CodeUnique
            : code.CodeUnique.Trim();

        var doublon = await context.CodeTransferts.AnyAsync(c =>
            c.Id != id &&
            c.IsActive &&
            c.CodeUnique.ToLower() == codeUnique.ToLower());

        if (doublon)
            return BadRequest("Ce code existe déjà.");

        existant.CodeUnique = codeUnique;
        existant.NomEnvoyeur = code.NomEnvoyeur?.Trim() ?? string.Empty;
        existant.TelephoneEnvoyeur = code.TelephoneEnvoyeur?.Trim() ?? string.Empty;
        existant.NomBeneficiaire = code.NomBeneficiaire?.Trim() ?? string.Empty;
        existant.TelephoneBeneficiaire = code.TelephoneBeneficiaire?.Trim() ?? string.Empty;
        existant.AdresseBeneficiaire = code.AdresseBeneficiaire?.Trim() ?? string.Empty;
        existant.PaysEnvoi = code.PaysEnvoi?.Trim() ?? string.Empty;
        existant.PaysRetrait = code.PaysRetrait?.Trim() ?? string.Empty;
        existant.Montant = code.Montant;
        existant.Frais = code.Frais;
        existant.TotalPaye = code.TotalPaye;
        existant.Statut = string.IsNullOrWhiteSpace(code.Statut) ? existant.Statut : code.Statut.Trim();
        existant.DateEnvoi = NormaliserDateUtc(code.DateEnvoi == default ? existant.DateEnvoi : code.DateEnvoi);
        existant.DateRetrait = NormaliserDateNullableUtc(code.DateRetrait);
        existant.NomRetireur = code.NomRetireur?.Trim() ?? string.Empty;
        existant.TelephoneRetireur = code.TelephoneRetireur?.Trim() ?? string.Empty;
        existant.PieceIdentite = code.PieceIdentite?.Trim() ?? string.Empty;
        existant.Observation = code.Observation?.Trim() ?? string.Empty;
        existant.ObservationRetrait = code.ObservationRetrait?.Trim() ?? string.Empty;
        existant.UtilisateurId = code.UtilisateurId;
        existant.UtilisateurNom = code.UtilisateurNom?.Trim() ?? string.Empty;
        existant.RoleUtilisateur = code.RoleUtilisateur?.Trim() ?? string.Empty;
        existant.RetraitUtilisateurId = code.RetraitUtilisateurId;
        existant.RetraitUtilisateurNom = code.RetraitUtilisateurNom?.Trim() ?? string.Empty;
        existant.RetraitRoleUtilisateur = code.RetraitRoleUtilisateur?.Trim() ?? string.Empty;
        existant.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(existant);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var code = await context.CodeTransferts.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (code == null)
            return NotFound();

        code.IsActive = false;
        code.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static void Nettoyer(AppCodeTransfert code)
    {
        code.CodeUnique = code.CodeUnique?.Trim() ?? string.Empty;
        code.NomEnvoyeur = code.NomEnvoyeur?.Trim() ?? string.Empty;
        code.TelephoneEnvoyeur = code.TelephoneEnvoyeur?.Trim() ?? string.Empty;
        code.NomBeneficiaire = code.NomBeneficiaire?.Trim() ?? string.Empty;
        code.TelephoneBeneficiaire = code.TelephoneBeneficiaire?.Trim() ?? string.Empty;
        code.AdresseBeneficiaire = code.AdresseBeneficiaire?.Trim() ?? string.Empty;
        code.PaysEnvoi = code.PaysEnvoi?.Trim() ?? string.Empty;
        code.PaysRetrait = code.PaysRetrait?.Trim() ?? string.Empty;
        code.Statut = string.IsNullOrWhiteSpace(code.Statut) ? "En attente" : code.Statut.Trim();
        code.NomRetireur = code.NomRetireur?.Trim() ?? string.Empty;
        code.TelephoneRetireur = code.TelephoneRetireur?.Trim() ?? string.Empty;
        code.PieceIdentite = code.PieceIdentite?.Trim() ?? string.Empty;
        code.Observation = code.Observation?.Trim() ?? string.Empty;
        code.ObservationRetrait = code.ObservationRetrait?.Trim() ?? string.Empty;
        code.UtilisateurNom = code.UtilisateurNom?.Trim() ?? string.Empty;
        code.RoleUtilisateur = code.RoleUtilisateur?.Trim() ?? string.Empty;
        code.RetraitUtilisateurNom = code.RetraitUtilisateurNom?.Trim() ?? string.Empty;
        code.RetraitRoleUtilisateur = code.RetraitRoleUtilisateur?.Trim() ?? string.Empty;
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

    private static DateTime? NormaliserDateNullableUtc(DateTime? date)
    {
        if (date == null)
            return null;

        return NormaliserDateUtc(date.Value);
    }
}

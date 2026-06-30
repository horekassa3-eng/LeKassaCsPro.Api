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

    [HttpGet("code/{codeUnique}")]
    public async Task<ActionResult<AppCodeTransfert>> GetByCodeAsync(string codeUnique)
    {
        if (string.IsNullOrWhiteSpace(codeUnique))
            return BadRequest("Code obligatoire.");

        var codeNormalise = codeUnique.Trim().ToUpper();

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

        var codeNormalise = code.CodeUnique.Trim().ToUpper();

        var existe = await context.CodeTransferts
            .AnyAsync(c => c.IsActive && c.CodeUnique.ToUpper() == codeNormalise);

        if (existe)
            return BadRequest("Ce code existe déjà.");

        code.Id = 0;
        code.CodeUnique = codeNormalise;
        code.Statut = string.IsNullOrWhiteSpace(code.Statut)
            ? "En attente"
            : code.Statut.Trim();
        code.DateEnvoi = NormaliserDateUtc(code.DateEnvoi);
        code.DateRetrait = NormaliserDateUtcNullable(code.DateRetrait);
        code.DateCreation = DateTime.UtcNow;
        code.DateModification = DateTime.UtcNow;
        code.IsActive = true;

        context.CodeTransferts.Add(code);
        await context.SaveChangesAsync();

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
            ? code.CodeUnique.Trim().ToUpper()
            : request.CodeUnique.Trim().ToUpper();

        var existe = await context.CodeTransferts
            .AnyAsync(c => c.Id != id && c.IsActive && c.CodeUnique.ToUpper() == codeNormalise);

        if (existe)
            return BadRequest("Ce code existe déjà.");

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
        code.Statut = string.IsNullOrWhiteSpace(request.Statut)
            ? code.Statut
            : request.Statut.Trim();
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

        await context.SaveChangesAsync();

        return Ok(code);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var code = await context.CodeTransferts
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (code == null)
            return NotFound();

        code.IsActive = false;
        code.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
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

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
        var items = await context.CodeTransferts
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.DateEnvoi)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppCodeTransfert>> GetByIdAsync(int id)
    {
        var item = await context.CodeTransferts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<AppCodeTransfert>> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Code obligatoire.");

        var codeNormalise = code.Trim();

        var item = await context.CodeTransferts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsActive && c.Code == codeNormalise);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<AppCodeTransfert>> CreateAsync([FromBody] AppCodeTransfert item)
    {
        if (item == null)
            return BadRequest("Données invalides.");

        if (item.DateEnvoi == default)
            item.DateEnvoi = DateTime.UtcNow;

        item.Code = item.Code?.Trim() ?? string.Empty;
        item.Statut = string.IsNullOrWhiteSpace(item.Statut) ? "En attente" : item.Statut.Trim();
        item.DateEnvoi = NormaliserDateUtc(item.DateEnvoi);
        item.DateRetrait = NormaliserDateUtcNullable(item.DateRetrait);
        item.DateCreation = DateTime.UtcNow;
        item.DateModification = DateTime.UtcNow;
        item.IsActive = true;

        context.CodeTransferts.Add(item);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppCodeTransfert>> UpdateAsync(int id, [FromBody] AppCodeTransfert item)
    {
        if (item == null || id <= 0)
            return BadRequest("Données invalides.");

        var existing = await context.CodeTransferts.FirstOrDefaultAsync(c => c.Id == id);

        if (existing == null)
            return NotFound();

        existing.Code = item.Code?.Trim() ?? string.Empty;
        existing.DateEnvoi = NormaliserDateUtc(item.DateEnvoi == default ? existing.DateEnvoi : item.DateEnvoi);
        existing.DateRetrait = NormaliserDateUtcNullable(item.DateRetrait);
        existing.PaysEnvoi = item.PaysEnvoi?.Trim() ?? string.Empty;
        existing.PaysRetrait = item.PaysRetrait?.Trim() ?? string.Empty;
        existing.AgenceEnvoi = item.AgenceEnvoi?.Trim() ?? string.Empty;
        existing.AgenceRetrait = item.AgenceRetrait?.Trim() ?? string.Empty;
        existing.NomExpediteur = item.NomExpediteur?.Trim() ?? string.Empty;
        existing.TelephoneExpediteur = item.TelephoneExpediteur?.Trim() ?? string.Empty;
        existing.NomBeneficiaire = item.NomBeneficiaire?.Trim() ?? string.Empty;
        existing.TelephoneBeneficiaire = item.TelephoneBeneficiaire?.Trim() ?? string.Empty;
        existing.Montant = item.Montant;
        existing.Frais = item.Frais;
        existing.TotalPaye = item.TotalPaye;
        existing.Statut = string.IsNullOrWhiteSpace(item.Statut) ? existing.Statut : item.Statut.Trim();
        existing.Observation = item.Observation?.Trim() ?? string.Empty;
        existing.IsActive = item.IsActive;
        existing.UtilisateurId = item.UtilisateurId;
        existing.UtilisateurNom = item.UtilisateurNom?.Trim() ?? string.Empty;
        existing.RoleUtilisateur = item.RoleUtilisateur?.Trim() ?? string.Empty;
        existing.RetraitUtilisateurId = item.RetraitUtilisateurId;
        existing.RetraitUtilisateurNom = item.RetraitUtilisateurNom?.Trim() ?? string.Empty;
        existing.RetraitRoleUtilisateur = item.RetraitRoleUtilisateur?.Trim() ?? string.Empty;
        existing.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var item = await context.CodeTransferts.FirstOrDefaultAsync(c => c.Id == id);

        if (item == null)
            return NotFound();

        item.IsActive = false;
        item.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/retirer")]
    public async Task<ActionResult<AppCodeTransfert>> RetirerAsync(int id, [FromBody] RetraitCodeRequest request)
    {
        var item = await context.CodeTransferts.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (item == null)
            return NotFound();

        item.Statut = "Retiré";
        item.DateRetrait = DateTime.UtcNow;
        item.RetraitUtilisateurId = request.UtilisateurId;
        item.RetraitUtilisateurNom = request.UtilisateurNom?.Trim() ?? string.Empty;
        item.RetraitRoleUtilisateur = request.RoleUtilisateur?.Trim() ?? string.Empty;
        item.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return Ok(item);
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
        return date.HasValue ? NormaliserDateUtc(date.Value) : null;
    }

    public sealed class RetraitCodeRequest
    {
        public int UtilisateurId { get; set; }

        public string UtilisateurNom { get; set; } = string.Empty;

        public string RoleUtilisateur { get; set; } = string.Empty;
    }
}

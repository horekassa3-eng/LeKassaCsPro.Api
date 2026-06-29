using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilisateurAdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public UtilisateurAdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<UtilisateurAdminDto>>> GetAllAsync()
    {
        var utilisateurs = await _context.Utilisateurs
            .OrderBy(u => u.NomComplet)
            .Select(u => new UtilisateurAdminDto
            {
                Id = u.Id,
                NomComplet = u.NomComplet,
                NomUtilisateur = u.NomUtilisateur,
                Role = u.Role,
                IsActif = u.IsActif
            })
            .ToListAsync();

        return Ok(utilisateurs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UtilisateurAdminDto>> GetByIdAsync(int id)
    {
        var utilisateur = await _context.Utilisateurs
            .Where(u => u.Id == id)
            .Select(u => new UtilisateurAdminDto
            {
                Id = u.Id,
                NomComplet = u.NomComplet,
                NomUtilisateur = u.NomUtilisateur,
                Role = u.Role,
                IsActif = u.IsActif
            })
            .FirstOrDefaultAsync();

        if (utilisateur == null)
            return NotFound("Utilisateur introuvable.");

        return Ok(utilisateur);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UtilisateurAdminDto>> UpdateAsync(
        int id,
        [FromBody] ModifierUtilisateurAdminRequest request)
    {
        if (id <= 0)
            return BadRequest("Identifiant utilisateur invalide.");

        var nomComplet = request.NomComplet?.Trim() ?? string.Empty;
        var nomUtilisateur = request.NomUtilisateur?.Trim() ?? string.Empty;
        var role = request.Role?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomComplet))
            return BadRequest("Le nom complet est obligatoire.");

        if (string.IsNullOrWhiteSpace(nomUtilisateur))
            return BadRequest("Le nom utilisateur est obligatoire.");

        if (string.IsNullOrWhiteSpace(role))
            return BadRequest("Le rôle est obligatoire.");

        var utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id);

        if (utilisateur == null)
            return NotFound("Utilisateur introuvable.");

        var nomUtilisateurMin = nomUtilisateur.ToLower();
        var existeDeja = await _context.Utilisateurs.AnyAsync(u =>
            u.Id != id &&
            u.NomUtilisateur.ToLower() == nomUtilisateurMin);

        if (existeDeja)
            return BadRequest("Un utilisateur avec ce nom utilisateur existe déjà.");

        if (utilisateur.IsActif && !request.IsActif && EstAdmin(utilisateur.Role))
        {
            var autresAdminsActifs = await _context.Utilisateurs.CountAsync(u =>
                u.Id != utilisateur.Id &&
                u.IsActif &&
                (u.Role == "Admin" || u.Role == "Administrateur"));

            if (autresAdminsActifs <= 0)
                return BadRequest("Impossible de désactiver le dernier administrateur actif.");
        }

        utilisateur.NomComplet = nomComplet;
        utilisateur.NomUtilisateur = nomUtilisateur;
        utilisateur.Role = role;
        utilisateur.IsActif = request.IsActif;

        await _context.SaveChangesAsync();

        return Ok(ToDto(utilisateur));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        if (id <= 0)
            return BadRequest("Identifiant utilisateur invalide.");

        var utilisateur = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == id);

        if (utilisateur == null)
            return NotFound("Utilisateur introuvable.");

        if (utilisateur.IsActif && EstAdmin(utilisateur.Role))
        {
            var autresAdminsActifs = await _context.Utilisateurs.CountAsync(u =>
                u.Id != utilisateur.Id &&
                u.IsActif &&
                (u.Role == "Admin" || u.Role == "Administrateur"));

            if (autresAdminsActifs <= 0)
                return BadRequest("Impossible de supprimer le dernier administrateur actif.");
        }

        utilisateur.IsActif = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static UtilisateurAdminDto ToDto(AppUtilisateur utilisateur)
    {
        return new UtilisateurAdminDto
        {
            Id = utilisateur.Id,
            NomComplet = utilisateur.NomComplet,
            NomUtilisateur = utilisateur.NomUtilisateur,
            Role = utilisateur.Role,
            IsActif = utilisateur.IsActif
        };
    }

    private static bool EstAdmin(string? role)
    {
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, "Administrateur", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class UtilisateurAdminDto
{
    public int Id { get; set; }

    public string NomComplet { get; set; } = string.Empty;

    public string NomUtilisateur { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActif { get; set; } = true;
}

public sealed class ModifierUtilisateurAdminRequest
{
    public string NomComplet { get; set; } = string.Empty;

    public string NomUtilisateur { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActif { get; set; } = true;
}

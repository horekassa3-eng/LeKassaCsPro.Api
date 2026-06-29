using BCrypt.Net;
using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilUtilisateurController(AppDbContext context) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfilUtilisateurResponse>> GetAsync(int id)
    {
        var utilisateur = await context.Utilisateurs
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (utilisateur == null)
            return NotFound("Utilisateur introuvable.");

        return Ok(CreerResponse(utilisateur));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProfilUtilisateurResponse>> ModifierAsync(
        int id,
        ModifierProfilUtilisateurRequest request)
    {
        if (request == null)
            return BadRequest("Requête invalide.");

        var utilisateur = await context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Id == id);

        if (utilisateur == null)
            return NotFound("Utilisateur introuvable.");

        if (!utilisateur.IsActif)
            return BadRequest("Utilisateur désactivé.");

        var nomUtilisateur = request.NomUtilisateur?.Trim() ?? string.Empty;
        var ancienMotDePasse = request.AncienMotDePasse?.Trim() ?? string.Empty;
        var nouveauMotDePasse = request.NouveauMotDePasse?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomUtilisateur))
            return BadRequest("Veuillez saisir le nom utilisateur.");

        if (nomUtilisateur.Length < 3)
            return BadRequest("Le nom utilisateur doit contenir au moins 3 caractères.");

        if (string.IsNullOrWhiteSpace(ancienMotDePasse))
            return BadRequest("Veuillez saisir l'ancien mot de passe.");

        if (string.IsNullOrWhiteSpace(utilisateur.MotDePasseHash)
            || !BCrypt.Net.BCrypt.Verify(ancienMotDePasse, utilisateur.MotDePasseHash))
        {
            return BadRequest("Ancien mot de passe incorrect.");
        }

        var existeDeja = await context.Utilisateurs.AnyAsync(u =>
            u.Id != id &&
            u.NomUtilisateur.ToLower() == nomUtilisateur.ToLower());

        if (existeDeja)
            return Conflict("Ce nom utilisateur existe déjà.");

        if (!string.IsNullOrWhiteSpace(nouveauMotDePasse))
        {
            if (nouveauMotDePasse.Length < 4)
                return BadRequest("Le nouveau mot de passe doit contenir au moins 4 caractères.");

            if (string.Equals(ancienMotDePasse, nouveauMotDePasse, StringComparison.Ordinal))
                return BadRequest("Le nouveau mot de passe doit être différent de l'ancien.");

            utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(nouveauMotDePasse);
        }

        utilisateur.NomUtilisateur = nomUtilisateur;

        await context.SaveChangesAsync();

        return Ok(CreerResponse(utilisateur));
    }

    private static ProfilUtilisateurResponse CreerResponse(AppUtilisateur utilisateur)
    {
        return new ProfilUtilisateurResponse
        {
            Id = utilisateur.Id,
            NomComplet = utilisateur.NomComplet,
            NomUtilisateur = utilisateur.NomUtilisateur,
            Role = utilisateur.Role,
            IsActif = utilisateur.IsActif
        };
    }
}

public class ModifierProfilUtilisateurRequest
{
    public string NomUtilisateur { get; set; } = string.Empty;

    public string AncienMotDePasse { get; set; } = string.Empty;

    public string NouveauMotDePasse { get; set; } = string.Empty;
}

public class ProfilUtilisateurResponse
{
    public int Id { get; set; }

    public string NomComplet { get; set; } = string.Empty;

    public string NomUtilisateur { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActif { get; set; }
}

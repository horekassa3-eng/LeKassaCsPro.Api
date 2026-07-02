using System.Security.Cryptography;
using System.Text;
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
    public async Task<ActionResult<object>> GetUtilisateurAsync(int id)
    {
        var utilisateur = await context.Utilisateurs
            .Where(u => u.Id == id && u.IsActif)
            .Select(u => new
            {
                u.Id,
                u.NomComplet,
                u.NomUtilisateur,
                u.Role,
                u.PaysAgence,
                u.IsActif,
                u.DateCreation
            })
            .FirstOrDefaultAsync();

        if (utilisateur == null)
            return NotFound("Utilisateur introuvable.");

        return Ok(utilisateur);
    }

    [HttpPut("{id:int}")]
    [HttpPost("{id:int}/modifier")]
    [HttpPut("{id:int}/modifier")]
    [HttpPost("{id:int}/modifier-profil")]
    [HttpPut("{id:int}/modifier-profil")]
    public async Task<ActionResult<object>> ModifierProfilAsync(int id, ModifierProfilRequest request)
    {
        var utilisateur = await context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActif);

        if (utilisateur == null)
            return NotFound("Utilisateur introuvable.");

        var nomUtilisateur = (request.NomUtilisateur ?? string.Empty).Trim();
        var ancienMotDePasse = request.AncienMotDePasse ?? string.Empty;
        var nouveauMotDePasse = request.NouveauMotDePasse ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomUtilisateur))
            return BadRequest("Le nom utilisateur est obligatoire.");

        if (nomUtilisateur.Length < 3)
            return BadRequest("Le nom utilisateur doit contenir au moins 3 caractères.");

        if (string.IsNullOrWhiteSpace(ancienMotDePasse))
            return BadRequest("L'ancien mot de passe est obligatoire.");

        if (!VerifierMotDePasse(ancienMotDePasse, utilisateur.MotDePasseHash))
            return BadRequest("Ancien mot de passe incorrect.");

        var nomNormalise = nomUtilisateur.ToLower();
        var existeDeja = await context.Utilisateurs.AnyAsync(u =>
            u.Id != utilisateur.Id &&
            u.IsActif &&
            u.NomUtilisateur.ToLower() == nomNormalise);

        if (existeDeja)
            return BadRequest("Ce nom utilisateur existe déjà.");

        utilisateur.NomUtilisateur = nomUtilisateur;

        if (!string.IsNullOrWhiteSpace(nouveauMotDePasse))
        {
            if (nouveauMotDePasse.Length < 4)
                return BadRequest("Le nouveau mot de passe doit contenir au moins 4 caractères.");

            utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(nouveauMotDePasse);
        }

        await context.SaveChangesAsync();

        return Ok(new
        {
            utilisateur.Id,
            utilisateur.NomComplet,
            utilisateur.NomUtilisateur,
            utilisateur.Role,
            utilisateur.PaysAgence,
            utilisateur.IsActif,
            utilisateur.DateCreation
        });
    }

    private static bool VerifierMotDePasse(string motDePasse, string? hash)
    {
        if (string.IsNullOrWhiteSpace(motDePasse) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            if (hash.StartsWith("$2", StringComparison.Ordinal))
                return BCrypt.Net.BCrypt.Verify(motDePasse, hash);
        }
        catch
        {
            // Continue avec les anciens formats possibles.
        }

        return string.Equals(hash, motDePasse, StringComparison.Ordinal)
               || string.Equals(hash, HashSha256(motDePasse), StringComparison.OrdinalIgnoreCase);
    }

    private static string HashSha256(string valeur)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(valeur));
        return Convert.ToHexString(bytes);
    }
}

public class ModifierProfilRequest
{
    public string? NomUtilisateur { get; set; }
    public string? AncienMotDePasse { get; set; }
    public string? NouveauMotDePasse { get; set; }
}

using System.Security.Cryptography;
using System.Text;
using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotDePasseOublieController : ControllerBase
{
    private readonly AppDbContext _context;

    public MotDePasseOublieController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("generer")]
    public async Task<ActionResult<CodeRecuperationResponse>> GenererCodeAsync(
        GenererCodeRecuperationRequest request)
    {
        if (request.UtilisateurId <= 0)
            return BadRequest(new { message = "Utilisateur invalide." });

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Id == request.UtilisateurId && u.IsActif);

        if (utilisateur == null)
            return NotFound(new { message = "Utilisateur actif introuvable." });

        var maintenant = DateTime.UtcNow;
        var dureeMinutes = request.DureeMinutes <= 0
            ? 60
            : Math.Min(request.DureeMinutes, 1440);

        var anciensCodes = await _context.CodesRecuperationMotDePasse
            .Where(c => c.UtilisateurId == utilisateur.Id && c.IsActive && !c.IsUtilise)
            .ToListAsync();

        foreach (var ancien in anciensCodes)
        {
            ancien.IsActive = false;
        }

        var code = await GenererCodeUniqueAsync();

        var codeRecuperation = new AppCodeRecuperationMotDePasse
        {
            UtilisateurId = utilisateur.Id,
            Code = code,
            DateCreation = maintenant,
            DateExpiration = maintenant.AddMinutes(dureeMinutes),
            IsUtilise = false,
            IsActive = true,
            UtilisateurCreationId = request.UtilisateurCreationId,
            UtilisateurCreationNom = request.UtilisateurCreationNom?.Trim() ?? string.Empty
        };

        _context.CodesRecuperationMotDePasse.Add(codeRecuperation);
        await _context.SaveChangesAsync();

        return Ok(new CodeRecuperationResponse
        {
            UtilisateurId = utilisateur.Id,
            NomComplet = utilisateur.NomComplet,
            NomUtilisateur = utilisateur.NomUtilisateur,
            Code = codeRecuperation.Code,
            DateExpiration = codeRecuperation.DateExpiration
        });
    }

    [HttpPost("reinitialiser")]
    public async Task<IActionResult> ReinitialiserAsync(ReinitialiserMotDePasseRequest request)
    {
        var nomUtilisateur = request.NomUtilisateur?.Trim() ?? string.Empty;
        var codeSaisi = request.CodeRecuperation?.Trim() ?? string.Empty;
        var nouveauMotDePasse = request.NouveauMotDePasse?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomUtilisateur))
            return BadRequest(new { message = "Veuillez saisir le nom utilisateur." });

        if (string.IsNullOrWhiteSpace(codeSaisi))
            return BadRequest(new { message = "Veuillez saisir le code de récupération." });

        if (string.IsNullOrWhiteSpace(nouveauMotDePasse) || nouveauMotDePasse.Length < 4)
            return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 4 caractères." });

        var nomNormalise = nomUtilisateur.ToLower();

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.NomUtilisateur.ToLower() == nomNormalise && u.IsActif);

        if (utilisateur == null)
            return NotFound(new { message = "Aucun compte actif ne correspond à ce nom utilisateur." });

        var maintenant = DateTime.UtcNow;

        var code = await _context.CodesRecuperationMotDePasse
            .Where(c => c.UtilisateurId == utilisateur.Id
                        && c.Code == codeSaisi
                        && c.IsActive
                        && !c.IsUtilise)
            .OrderByDescending(c => c.DateCreation)
            .FirstOrDefaultAsync();

        if (code == null || code.DateExpiration < maintenant)
            return BadRequest(new { message = "Le code est incorrect, expiré ou déjà utilisé." });

        // CORRECTION ICI : AppUtilisateur n'a pas MotDePasse, il a MotDePasseHash.
        utilisateur.MotDePasseHash = HashMotDePasse(nouveauMotDePasse);

        code.IsUtilise = true;
        code.IsActive = false;
        code.DateUtilisation = maintenant;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Mot de passe réinitialisé." });
    }

    private async Task<string> GenererCodeUniqueAsync()
    {
        for (var tentative = 0; tentative < 30; tentative++)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var existe = await _context.CodesRecuperationMotDePasse
                .AnyAsync(c => c.Code == code && c.IsActive && !c.IsUtilise);

            if (!existe)
                return code;
        }

        return DateTime.UtcNow.ToString("HHmmss");
    }

    private static string HashMotDePasse(string motDePasse)
    {
        var bytes = Encoding.UTF8.GetBytes(motDePasse);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}

public sealed class GenererCodeRecuperationRequest
{
    public int UtilisateurId { get; set; }
    public int DureeMinutes { get; set; } = 60;
    public int UtilisateurCreationId { get; set; }
    public string UtilisateurCreationNom { get; set; } = string.Empty;
}

public sealed class ReinitialiserMotDePasseRequest
{
    public string NomUtilisateur { get; set; } = string.Empty;
    public string CodeRecuperation { get; set; } = string.Empty;
    public string NouveauMotDePasse { get; set; } = string.Empty;
}

public sealed class CodeRecuperationResponse
{
    public int UtilisateurId { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string NomUtilisateur { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime DateExpiration { get; set; }
}
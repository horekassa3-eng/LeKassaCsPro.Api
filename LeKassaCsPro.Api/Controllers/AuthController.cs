using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Dtos;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("existe-utilisateur")]
    public async Task<ActionResult<bool>> ExisteUtilisateurAsync()
    {
        return await _context.Utilisateurs.AnyAsync();
    }

    [HttpGet("utilisateurs")]
    public async Task<ActionResult<List<LoginResponse>>> GetUtilisateursAsync()
    {
        var utilisateurs = await _context.Utilisateurs
            .OrderBy(u => u.NomComplet)
            .Select(u => new LoginResponse
            {
                Id = u.Id,
                NomComplet = u.NomComplet,
                NomUtilisateur = u.NomUtilisateur,
                Role = u.Role,
                Token = string.Empty
            })
            .ToListAsync();

        return Ok(utilisateurs);
    }

    [HttpPost("creer-premier-admin")]
    public async Task<ActionResult<LoginResponse>> CreerPremierAdminAsync(CreerPremierAdminRequest request)
    {
        if (await _context.Utilisateurs.AnyAsync())
            return BadRequest("Le premier compte existe déjà.");

        if (string.IsNullOrWhiteSpace(request.NomComplet) ||
            string.IsNullOrWhiteSpace(request.NomUtilisateur) ||
            string.IsNullOrWhiteSpace(request.MotDePasse))
        {
            return BadRequest("Veuillez remplir tous les champs.");
        }

        var utilisateur = new AppUtilisateur
        {
            NomComplet = request.NomComplet.Trim(),
            NomUtilisateur = request.NomUtilisateur.Trim(),
            MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(request.MotDePasse),
            Role = "Admin",
            IsActif = true,
            DateCreation = DateTime.UtcNow
        };

        _context.Utilisateurs.Add(utilisateur);
        await _context.SaveChangesAsync();

        return Ok(CreerReponse(utilisateur));
    }

    [HttpPost("creer-utilisateur")]
    public async Task<ActionResult<LoginResponse>> CreerUtilisateurAsync(CreerUtilisateurRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NomComplet) ||
            string.IsNullOrWhiteSpace(request.NomUtilisateur) ||
            string.IsNullOrWhiteSpace(request.MotDePasse) ||
            string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest("Veuillez remplir tous les champs.");
        }

        var nomUtilisateur = request.NomUtilisateur.Trim().ToLower();

        var existeDeja = await _context.Utilisateurs
            .AnyAsync(u => u.NomUtilisateur.ToLower() == nomUtilisateur);

        if (existeDeja)
            return BadRequest("Ce nom utilisateur existe déjà.");

        var utilisateur = new AppUtilisateur
        {
            NomComplet = request.NomComplet.Trim(),
            NomUtilisateur = request.NomUtilisateur.Trim(),
            MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(request.MotDePasse),
            Role = request.Role.Trim(),
            IsActif = true,
            DateCreation = DateTime.UtcNow
        };

        _context.Utilisateurs.Add(utilisateur);
        await _context.SaveChangesAsync();

        return Ok(CreerReponse(utilisateur));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var nomUtilisateur = request.NomUtilisateur.Trim().ToLower();

        var utilisateur = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.NomUtilisateur.ToLower() == nomUtilisateur);

        if (utilisateur is null || !utilisateur.IsActif)
            return Unauthorized("Utilisateur ou mot de passe incorrect.");

        var motDePasseOk = BCrypt.Net.BCrypt.Verify(request.MotDePasse, utilisateur.MotDePasseHash);

        if (!motDePasseOk)
            return Unauthorized("Utilisateur ou mot de passe incorrect.");

        return Ok(CreerReponse(utilisateur));
    }

    private LoginResponse CreerReponse(AppUtilisateur utilisateur)
    {
        return new LoginResponse
        {
            Id = utilisateur.Id,
            NomComplet = utilisateur.NomComplet,
            NomUtilisateur = utilisateur.NomUtilisateur,
            Role = utilisateur.Role,
            Token = CreerToken(utilisateur)
        };
    }

    private string CreerToken(AppUtilisateur utilisateur)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("La clé JWT est manquante.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
            new Claim(ClaimTypes.Name, utilisateur.NomUtilisateur),
            new Claim(ClaimTypes.Role, utilisateur.Role),
            new Claim("nom_complet", utilisateur.NomComplet)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
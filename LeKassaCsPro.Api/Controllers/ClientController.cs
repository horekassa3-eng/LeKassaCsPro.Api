using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppClient>>> GetAllAsync()
    {
        var clients = await context.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.NomComplet)
            .ThenBy(c => c.Telephone)
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppClient>> GetByIdAsync(int id)
    {
        var client = await context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (client == null)
            return NotFound();

        return Ok(client);
    }

    [HttpGet("{id:int}/a-dette")]
    public async Task<ActionResult<bool>> ClientADetteAsync(int id)
    {
        var client = await context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (client == null)
            return Ok(false);

        var aDette = client.DetteFcfa > 0 || client.DetteGnf > 0;
        return Ok(aDette);
    }

    [HttpPost]
    public async Task<ActionResult<AppClient>> CreateAsync([FromBody] AppClient request)
    {
        var validation = Valider(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return BadRequest(validation);

        request.Id = 0;
        Nettoyer(request);
        request.DetteFcfa = request.DetteFcfa < 0 ? 0 : request.DetteFcfa;
        request.DetteGnf = request.DetteGnf < 0 ? 0 : request.DetteGnf;
        request.IsActive = true;
        request.DateCreation = DateTime.UtcNow;
        request.DateModification = DateTime.UtcNow;

        context.Clients.Add(request);
        await context.SaveChangesAsync();

        return Ok(request);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AppClient>> UpdateAsync(int id, [FromBody] AppClient request)
    {
        var client = await context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (client == null)
            return NotFound();

        var validation = Valider(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return BadRequest(validation);

        client.NomComplet = request.NomComplet?.Trim() ?? string.Empty;
        client.Telephone = request.Telephone?.Trim() ?? string.Empty;
        client.Adresse = request.Adresse?.Trim() ?? string.Empty;
        client.TypeClient = NormaliserTypeClient(request.TypeClient);
        client.DetteFcfa = request.DetteFcfa < 0 ? 0 : request.DetteFcfa;
        client.DetteGnf = request.DetteGnf < 0 ? 0 : request.DetteGnf;
        client.Pays = request.Pays?.Trim() ?? string.Empty;
        client.Ville = request.Ville?.Trim() ?? string.Empty;
        client.Observation = request.Observation?.Trim() ?? string.Empty;
        client.UtilisateurId = request.UtilisateurId;
        client.UtilisateurNom = request.UtilisateurNom?.Trim() ?? string.Empty;
        client.RoleUtilisateur = request.RoleUtilisateur?.Trim() ?? string.Empty;
        client.IsActive = true;
        client.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(client);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var client = await context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

        if (client == null)
            return NotFound();

        if (client.DetteFcfa > 0 || client.DetteGnf > 0)
            return BadRequest("Impossible de supprimer ce client parce qu'il a une dette en cours.");

        client.IsActive = false;
        client.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }

    private static string? Valider(AppClient client)
    {
        if (string.IsNullOrWhiteSpace(client.NomComplet))
            return "Le nom du client est obligatoire.";

        if (string.IsNullOrWhiteSpace(client.Telephone))
            return "Le téléphone du client est obligatoire.";

        return null;
    }

    private static void Nettoyer(AppClient client)
    {
        client.NomComplet = client.NomComplet?.Trim() ?? string.Empty;
        client.Telephone = client.Telephone?.Trim() ?? string.Empty;
        client.Adresse = client.Adresse?.Trim() ?? string.Empty;
        client.TypeClient = NormaliserTypeClient(client.TypeClient);
        client.Pays = client.Pays?.Trim() ?? string.Empty;
        client.Ville = client.Ville?.Trim() ?? string.Empty;
        client.Observation = client.Observation?.Trim() ?? string.Empty;
        client.UtilisateurNom = client.UtilisateurNom?.Trim() ?? string.Empty;
        client.RoleUtilisateur = client.RoleUtilisateur?.Trim() ?? string.Empty;
    }

    private static string NormaliserTypeClient(string? typeClient)
    {
        return string.IsNullOrWhiteSpace(typeClient)
            ? "Normal"
            : typeClient.Trim();
    }
}

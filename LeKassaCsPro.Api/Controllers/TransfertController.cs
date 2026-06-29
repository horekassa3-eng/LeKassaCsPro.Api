using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransfertController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppTransfert>>> GetAllAsync()
    {
        var transferts = await context.Transferts
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.DateTransfert)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        return Ok(transferts);
    }

    [HttpPut("{id:int}/annuler")]
    public async Task<ActionResult<AppTransfert>> AnnulerAsync(int id)
    {
        var transfert = await context.Transferts
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (transfert == null)
            return NotFound();

        transfert.Statut = "Annulé";
        transfert.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(transfert);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var transfert = await context.Transferts
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

        if (transfert == null)
            return NotFound();

        transfert.Statut = "Annulé";
        transfert.IsActive = false;
        transfert.DateModification = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return NoContent();
    }
}

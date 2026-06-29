using LeKassaCsPro.Api.Data;
using LeKassaCsPro.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransfertController(AppDbContext context) : ControllerBase
{
    private const string SensSenegalGuinee = "Sénégal vers Guinée";
    private const string SensGuineeSenegal = "Guinée vers Sénégal";
    private const string TypeUtilisationTransfert = "Utilisation transfert";
    private const string TypeEntreeClientGuinee = "Entrée client Guinée";
    private const string DeviseFcfa = "FCFA";

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

    [HttpPost]
    public async Task<ActionResult<AppTransfert>> SaveAsync([FromBody] AppTransfert request)
    {
        if (string.IsNullOrWhiteSpace(request.SensTransfert))
            return BadRequest("Le sens du transfert est obligatoire.");

        if (request.TauxGnfParFcfa <= 0)
            return BadRequest("Le taux du transfert est invalide.");

        request.Id = 0;
        request.DateTransfert = NormaliserDateUtc(request.DateTransfert);
        request.DateCreation = DateTime.UtcNow;
        request.DateModification = DateTime.UtcNow;
        request.IsActive = true;

        await using var transaction = await context.Database.BeginTransactionAsync();

        if (EstSensSenegalVersGuinee(request.SensTransfert))
        {
            if (request.FcfaDonneFournisseur <= 0)
                return BadRequest("Le montant fournisseur FCFA est obligatoire.");

            var soldeFcfa = await GetSoldeFournisseursTotalAsync();

            if (request.FcfaDonneFournisseur > soldeFcfa)
                return BadRequest($"Solde disponible insuffisant. Solde serveur : {soldeFcfa:N0} FCFA.");
        }
        else if (EstSensGuineeVersSenegal(request.SensTransfert))
        {
            if (request.MontantFcfaBeneficiaire <= 0)
                return BadRequest("Le montant bénéficiaire FCFA est obligatoire.");
        }

        context.Transferts.Add(request);
        await context.SaveChangesAsync();

        var mouvement = CreerMouvementFournisseur(request);

        if (mouvement != null)
        {
            context.FournisseurMouvements.Add(mouvement);
            await context.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return Ok(request);
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

        await DesactiverMouvementsDuTransfertAsync(transfert.Id);
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

        await DesactiverMouvementsDuTransfertAsync(transfert.Id);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task DesactiverMouvementsDuTransfertAsync(int transfertId)
    {
        var mouvements = await context.FournisseurMouvements
            .Where(m => m.IsActive && m.TransfertId == transfertId)
            .ToListAsync();

        foreach (var mouvement in mouvements)
            mouvement.IsActive = false;
    }

    private async Task<decimal> GetSoldeFournisseursTotalAsync()
    {
        var soldeMouvementsManuels = await GetSoldeFournisseursHorsTransfertsAsync();

        var transfertsPayes = await context.Transferts
            .Where(t => t.IsActive && (t.Statut == "Payé" || t.Statut == "Paye"))
            .ToListAsync();

        var entreesGuineeSenegal = transfertsPayes
            .Where(t => EstSensGuineeVersSenegal(t.SensTransfert))
            .Sum(t => t.MontantFcfaBeneficiaire);

        var sortiesSenegalGuinee = transfertsPayes
            .Where(t => EstSensSenegalVersGuinee(t.SensTransfert))
            .Sum(t => t.FcfaDonneFournisseur > 0 ? t.FcfaDonneFournisseur : t.TotalAPayerFcfa);

        return soldeMouvementsManuels + entreesGuineeSenegal - sortiesSenegalGuinee;
    }

    private async Task<decimal> GetSoldeFournisseursHorsTransfertsAsync()
    {
        var mouvements = await context.FournisseurMouvements
            .Where(m => m.IsActive && m.Devise == DeviseFcfa && m.TransfertId == 0)
            .ToListAsync();

        var entrees = mouvements
            .Where(m => EstEntreeSoldeFournisseur(m.TypeMouvement))
            .Sum(m => m.Montant);

        var sorties = mouvements
            .Where(m => EstSortieSoldeFournisseur(m.TypeMouvement))
            .Sum(m => m.Montant);

        return entrees - sorties;
    }

    private static AppFournisseurMouvement? CreerMouvementFournisseur(AppTransfert transfert)
    {
        if (EstSensSenegalVersGuinee(transfert.SensTransfert))
        {
            return new AppFournisseurMouvement
            {
                FournisseurId = transfert.FournisseurId ?? 0,
                TransfertId = transfert.Id,
                DateMouvement = transfert.DateTransfert,
                TypeMouvement = TypeUtilisationTransfert,
                Devise = DeviseFcfa,
                MoyenPaiement = "Solde disponible",
                Montant = transfert.FcfaDonneFournisseur,
                TauxGnfParFcfa = transfert.TauxGnfParFcfa,
                MontantGnfEnvoye = transfert.GnfEnvoyeBeneficiaire,
                Observation =
                    $"Utilisation pour transfert SN → GN.\n" +
                    $"Transfert #{transfert.Id}\n" +
                    $"Client paie : {transfert.TotalAPayerFcfa:N0} FCFA\n" +
                    $"Fournisseur utilisé : {transfert.FcfaDonneFournisseur:N0} FCFA\n" +
                    $"Bénéficiaire reçoit : {transfert.GnfEnvoyeBeneficiaire:N0} GNF\n" +
                    $"Commission nette : {transfert.BeneficeFcfa:N0} FCFA",
                UtilisateurId = transfert.UtilisateurId,
                UtilisateurNom = transfert.UtilisateurNom,
                RoleUtilisateur = transfert.RoleUtilisateur,
                IsActive = true
            };
        }

        if (EstSensGuineeVersSenegal(transfert.SensTransfert))
        {
            return new AppFournisseurMouvement
            {
                FournisseurId = 0,
                TransfertId = transfert.Id,
                DateMouvement = transfert.DateTransfert,
                TypeMouvement = TypeEntreeClientGuinee,
                Devise = DeviseFcfa,
                MoyenPaiement = "Client Guinée → Sénégal",
                Montant = transfert.MontantFcfaBeneficiaire,
                TauxGnfParFcfa = transfert.TauxGnfParFcfa,
                MontantGnfEnvoye = transfert.MontantGnfClient,
                Observation =
                    $"Entrée FCFA depuis cliente Guinée.\n" +
                    $"Transfert #{transfert.Id}\n" +
                    $"Client paie : {transfert.MontantGnfClient:N0} GNF\n" +
                    $"Montant reçu au Sénégal : {transfert.MontantFcfaBeneficiaire:N0} FCFA\n" +
                    $"Commission estimée : {transfert.BeneficeFcfa:N0} FCFA",
                UtilisateurId = transfert.UtilisateurId,
                UtilisateurNom = transfert.UtilisateurNom,
                RoleUtilisateur = transfert.RoleUtilisateur,
                IsActive = true
            };
        }

        return null;
    }

    private static bool EstEntreeSoldeFournisseur(string? typeMouvement)
    {
        return string.Equals(typeMouvement, "Approvisionnement", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Entree client Guinee", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, TypeEntreeClientGuinee, StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Réception", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Reception", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Retrait", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Retrait code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSortieSoldeFournisseur(string? typeMouvement)
    {
        return string.Equals(typeMouvement, TypeUtilisationTransfert, StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi", StringComparison.OrdinalIgnoreCase)
               || string.Equals(typeMouvement, "Envoi code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSensSenegalVersGuinee(string? sens)
    {
        if (string.IsNullOrWhiteSpace(sens))
            return false;

        return string.Equals(sens, SensSenegalGuinee, StringComparison.OrdinalIgnoreCase)
               || sens.StartsWith("Sénégal", StringComparison.OrdinalIgnoreCase)
               || sens.StartsWith("Senegal", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("SN → GN", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("SN->GN", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EstSensGuineeVersSenegal(string? sens)
    {
        if (string.IsNullOrWhiteSpace(sens))
            return false;

        return string.Equals(sens, SensGuineeSenegal, StringComparison.OrdinalIgnoreCase)
               || sens.StartsWith("Guinée", StringComparison.OrdinalIgnoreCase)
               || sens.StartsWith("Guinee", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("GN → SN", StringComparison.OrdinalIgnoreCase)
               || sens.Contains("GN->SN", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTime NormaliserDateUtc(DateTime date)
    {
        if (date == default)
            date = DateTime.UtcNow;

        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }
}

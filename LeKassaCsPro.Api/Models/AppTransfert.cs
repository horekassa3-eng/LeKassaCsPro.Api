namespace LeKassaCsPro.Api.Models;

public class AppTransfert
{
    public int Id { get; set; }
    public DateTime DateTransfert { get; set; } = DateTime.UtcNow;
    public string SensTransfert { get; set; } = "Sénégal vers Guinée";
    public int? ClientId { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string TelephoneClient { get; set; } = string.Empty;
    public string NomBeneficiaire { get; set; } = string.Empty;
    public string TelephoneBeneficiaire { get; set; } = string.Empty;
    public int? FournisseurId { get; set; }
    public string NomFournisseur { get; set; } = string.Empty;
    public decimal TauxGnfParFcfa { get; set; }
    public decimal FcfaDonneFournisseur { get; set; }
    public decimal GnfRecuFournisseur { get; set; }
    public decimal FcfaPayeClient { get; set; }
    public decimal GnfEnvoyeBeneficiaire { get; set; }
    public decimal MontantGnfClient { get; set; }
    public decimal MontantFcfaBeneficiaire { get; set; }
    public decimal PourcentageFraisService { get; set; } = 1m;
    public decimal FraisServiceFcfa { get; set; }
    public decimal FraisServiceGnf { get; set; }
    public decimal TotalAPayerFcfa { get; set; }
    public decimal TotalAPayerGnf { get; set; }
    public decimal BeneficeFcfa { get; set; }
    public string ModePaiement { get; set; } = "Orange Money";
    public string Statut { get; set; } = "En attente";
    public string Observation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;
}

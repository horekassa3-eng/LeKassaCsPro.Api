namespace LeKassaCsPro.Api.Models;

public class AppCodeTransfert
{
    public int Id { get; set; }

    public string CodeUnique { get; set; } = string.Empty;

    public string NomEnvoyeur { get; set; } = string.Empty;
    public string TelephoneEnvoyeur { get; set; } = string.Empty;

    public string NomBeneficiaire { get; set; } = string.Empty;
    public string TelephoneBeneficiaire { get; set; } = string.Empty;
    public string AdresseBeneficiaire { get; set; } = string.Empty;

    public string PaysEnvoi { get; set; } = string.Empty;
    public string PaysRetrait { get; set; } = string.Empty;

    public decimal Montant { get; set; }
    public decimal Frais { get; set; }
    public decimal TotalPaye { get; set; }

    public string Statut { get; set; } = "En attente";

    public DateTime DateEnvoi { get; set; }
    public DateTime? DateRetrait { get; set; }

    public string Observation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurEnvoiId { get; set; }
    public string UtilisateurEnvoiNom { get; set; } = string.Empty;
    public string RoleUtilisateurEnvoi { get; set; } = string.Empty;

    public int UtilisateurRetraitId { get; set; }
    public string UtilisateurRetraitNom { get; set; } = string.Empty;
    public string RoleUtilisateurRetrait { get; set; } = string.Empty;

    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}

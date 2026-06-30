namespace LeKassaCsPro.Api.Models;

public class AppCodeTransfert
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public DateTime DateEnvoi { get; set; }

    public DateTime? DateRetrait { get; set; }

    public string PaysEnvoi { get; set; } = string.Empty;

    public string PaysRetrait { get; set; } = string.Empty;

    public string AgenceEnvoi { get; set; } = string.Empty;

    public string AgenceRetrait { get; set; } = string.Empty;

    public string NomExpediteur { get; set; } = string.Empty;

    public string TelephoneExpediteur { get; set; } = string.Empty;

    public string NomBeneficiaire { get; set; } = string.Empty;

    public string TelephoneBeneficiaire { get; set; } = string.Empty;

    public decimal Montant { get; set; }

    public decimal Frais { get; set; }

    public decimal TotalPaye { get; set; }

    public string Statut { get; set; } = "En attente";

    public string Observation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }

    public string UtilisateurNom { get; set; } = string.Empty;

    public string RoleUtilisateur { get; set; } = string.Empty;

    public int RetraitUtilisateurId { get; set; }

    public string RetraitUtilisateurNom { get; set; } = string.Empty;

    public string RetraitRoleUtilisateur { get; set; } = string.Empty;

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }
}

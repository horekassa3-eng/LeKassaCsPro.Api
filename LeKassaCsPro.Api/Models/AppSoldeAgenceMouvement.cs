namespace LeKassaCsPro.Api.Models;

public class AppSoldeAgenceMouvement
{
    public int Id { get; set; }

    public DateTime DateMouvement { get; set; }

    public string PaysAgence { get; set; } = string.Empty;

    public string MoyenPaiement { get; set; } = string.Empty;

    public string Devise { get; set; } = "FCFA";

    public string TypeMouvement { get; set; } = string.Empty;

    public decimal Montant { get; set; }

    public string Motif { get; set; } = string.Empty;

    public string Observation { get; set; } = string.Empty;

    public string SourceModule { get; set; } = string.Empty;

    public int SourceId { get; set; }

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }

    public string UtilisateurNom { get; set; } = string.Empty;

    public string RoleUtilisateur { get; set; } = string.Empty;

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }
}

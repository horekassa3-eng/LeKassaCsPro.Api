namespace LeKassaCsPro.Api.Models;

public class AppEpargneMouvement
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public string ClientNom { get; set; } = string.Empty;

    public string ClientTelephone { get; set; } = string.Empty;

    public DateTime DateMouvement { get; set; }

    public string TypeMouvement { get; set; } = "Dépôt";

    public decimal Montant { get; set; }

    public string Devise { get; set; } = "FCFA";

    public string Motif { get; set; } = string.Empty;

    public string Observation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }

    public string UtilisateurNom { get; set; } = string.Empty;

    public string RoleUtilisateur { get; set; } = string.Empty;

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }
}
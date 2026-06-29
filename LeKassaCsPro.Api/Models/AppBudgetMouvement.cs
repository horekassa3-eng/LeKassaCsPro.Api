namespace LeKassaCsPro.Api.Models;

public class AppBudgetMouvement
{
    public int Id { get; set; }
    public DateTime DateMouvement { get; set; } = DateTime.UtcNow;
    public string TypeMouvement { get; set; } = string.Empty;
    public string Devise { get; set; } = "FCFA";
    public decimal Montant { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string Observation { get; set; } = string.Empty;
    public string SourceModule { get; set; } = "Manuel";
    public int SourceId { get; set; }
    public bool IsAutomatique { get; set; }
    public bool IsActive { get; set; } = true;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}

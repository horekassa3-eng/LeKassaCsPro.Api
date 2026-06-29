namespace LeKassaCsPro.Api.Models;

public class AppRecetteService
{
    public int Id { get; set; }
    public DateTime DateRecette { get; set; } = DateTime.UtcNow;
    public string TypeService { get; set; } = string.Empty;
    public string Devise { get; set; } = "FCFA";
    public decimal Montant { get; set; }
    public string Observation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}

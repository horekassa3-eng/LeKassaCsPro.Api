namespace LeKassaCsPro.Api.Models;

public class AppDepense
{
    public int Id { get; set; }
    public DateTime DateDepense { get; set; } = DateTime.UtcNow;
    public string Categorie { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public string Devise { get; set; } = "FCFA";
    public string ModePaiement { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}

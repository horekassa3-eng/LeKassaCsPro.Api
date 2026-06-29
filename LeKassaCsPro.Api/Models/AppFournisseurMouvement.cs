namespace LeKassaCsPro.Api.Models;

public class AppFournisseurMouvement
{
    public int Id { get; set; }
    public int FournisseurId { get; set; }
    public int TransfertId { get; set; }
    public DateTime DateMouvement { get; set; } = DateTime.UtcNow;
    public string TypeMouvement { get; set; } = string.Empty;
    public string Devise { get; set; } = string.Empty;
    public string MoyenPaiement { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public decimal FraisFournisseur { get; set; }
    public decimal MontantGnfEnvoye { get; set; }
    public decimal TauxGnfParFcfa { get; set; }
    public string Observation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
}

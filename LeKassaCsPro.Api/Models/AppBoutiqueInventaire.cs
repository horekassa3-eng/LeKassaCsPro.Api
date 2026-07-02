namespace LeKassaCsPro.Api.Models;

public class AppBoutiqueInventaire
{
    public int Id { get; set; }

    public int BoutiqueId { get; set; }

    public string BoutiqueNom { get; set; } = string.Empty;

    public DateTime DateInventaire { get; set; } = DateTime.UtcNow;

    // Total actuel = valeur marchandise + argent liquide + dette client - dépôt.
    public decimal ValeurStock { get; set; }

    public decimal ValeurMarchandise { get; set; }

    public decimal ArgentLiquide { get; set; }

    public decimal DetteClient { get; set; }

    public decimal Depot { get; set; }

    public decimal GainMois { get; set; }

    public decimal DepenseProprietaireMois { get; set; }

    public string Observation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }

    public string UtilisateurNom { get; set; } = "Utilisateur local";

    public string RoleUtilisateur { get; set; } = "Caissier";

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }
}

namespace LeKassaCsPro.Api.Models;

public class AppProduitVente
{
    public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public string CodeProduit { get; set; } = string.Empty;
    public string Unite { get; set; } = "Pièce";

    public decimal PrixAchat { get; set; }
    public decimal PrixVente { get; set; }
    public decimal StockAlerte { get; set; }

    // Image du produit synchronisée en ligne
    public string ImageBase64 { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";

    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}
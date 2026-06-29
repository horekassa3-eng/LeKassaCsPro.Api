namespace LeKassaCsPro.Api.Models;

public class AppVenteDetail
{
    public int Id { get; set; }
    public int VenteId { get; set; }
    public int ProduitVenteId { get; set; }
    public string ProduitNom { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal MontantTotal { get; set; }
    public bool IsActive { get; set; } = true;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}

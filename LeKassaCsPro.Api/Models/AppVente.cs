namespace LeKassaCsPro.Api.Models;

public class AppVente
{
    public int Id { get; set; }
    public DateTime DateVente { get; set; } = DateTime.UtcNow;
    public int ClientId { get; set; }
    public string NomClient { get; set; } = "Client comptoir";
    public string TelephoneClient { get; set; } = string.Empty;
    public string ModePaiement { get; set; } = "Espèce";
    public string StatutPaiement { get; set; } = "Payé";
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; }
    public decimal Remise { get; set; }
    public string Observation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}

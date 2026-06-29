namespace LeKassaCsPro.Api.Models;

public class AppBoutiqueInventaire
{
    public int Id { get; set; }

    public int BoutiqueId { get; set; }

    public string BoutiqueNom { get; set; } = string.Empty;

    public DateTime DateInventaire { get; set; }

    public decimal BudgetInitial { get; set; }

    public decimal MontantVente { get; set; }

    public decimal DepenseProprietaire { get; set; }

    public decimal SoldeCaisse { get; set; }

    public decimal GainMois { get; set; }

    public string Observation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }

    public string UtilisateurNom { get; set; } = string.Empty;

    public string RoleUtilisateur { get; set; } = string.Empty;

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }
}

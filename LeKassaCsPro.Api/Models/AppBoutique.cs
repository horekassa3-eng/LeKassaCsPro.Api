namespace LeKassaCsPro.Api.Models;

public class AppBoutique
{
    public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string Pays { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string Adresse { get; set; } = string.Empty;

    public decimal BudgetInitial { get; set; }

    public int GerantUtilisateurId { get; set; }

    public string GerantNom { get; set; } = string.Empty;

    public int AssistantUtilisateurId { get; set; }

    public string AssistantNom { get; set; } = string.Empty;

    public string Observation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }

    public string UtilisateurNom { get; set; } = string.Empty;

    public string RoleUtilisateur { get; set; } = string.Empty;

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }
}

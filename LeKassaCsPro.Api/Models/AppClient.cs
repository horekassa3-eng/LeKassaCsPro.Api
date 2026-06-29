namespace LeKassaCsPro.Api.Models;

public class AppClient
{
    public int Id { get; set; }

    public string NomComplet { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Adresse { get; set; } = string.Empty;
    public string Pays { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string Observation { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = string.Empty;
    public string RoleUtilisateur { get; set; } = string.Empty;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime? DateModification { get; set; }
}

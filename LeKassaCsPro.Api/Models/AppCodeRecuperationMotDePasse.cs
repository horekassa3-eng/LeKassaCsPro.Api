namespace LeKassaCsPro.Api.Models;

public class AppCodeRecuperationMotDePasse
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime DateExpiration { get; set; }
    public DateTime? DateUtilisation { get; set; }
    public bool IsUtilise { get; set; }
    public bool IsActive { get; set; } = true;

    public int UtilisateurCreationId { get; set; }
    public string UtilisateurCreationNom { get; set; } = string.Empty;
}

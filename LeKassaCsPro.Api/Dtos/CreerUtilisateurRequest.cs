namespace LeKassaCsPro.Api.Dtos;

public class CreerUtilisateurRequest
{
    public string NomComplet { get; set; } = string.Empty;
    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
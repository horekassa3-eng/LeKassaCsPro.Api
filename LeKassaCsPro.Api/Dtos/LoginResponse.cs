namespace LeKassaCsPro.Api.Dtos;

public class LoginResponse
{
    public int Id { get; set; }
    public string NomComplet { get; set; } = string.Empty;
    public string NomUtilisateur { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string PaysAgence { get; set; } = "Senegal";
    public bool IsActif { get; set; }
    public string Token { get; set; } = string.Empty;
}

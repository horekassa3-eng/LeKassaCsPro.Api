namespace LeKassaCsPro.Api.Models;

public class AppUtilisateur
{
    public int Id { get; set; }

    public string NomComplet { get; set; } = string.Empty;
    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotDePasseHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Caissier";
    public bool IsActif { get; set; } = true;

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
}
namespace LeKassaCsPro.Api.Models;

public class AppParametre
{
    public int Id { get; set; }

    public string Cle { get; set; } = string.Empty;

    public string Valeur { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? DateCreation { get; set; }

    public DateTime? DateModification { get; set; }
}

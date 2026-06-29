namespace LeKassaCsPro.Api.Models;

public class AppTauxChange
{
    public int Id { get; set; }
    public DateTime DateTaux { get; set; } = DateTime.UtcNow;
    public decimal MontantReferenceFcfa { get; set; }
    public decimal MontantEquivalentGnf { get; set; }
    public decimal TauxGnfParFcfa { get; set; }
    public bool IsActif { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public decimal FraisServiceSnGnPourcentage { get; set; } = 1m;
    public decimal FraisServiceGnSnPourcentage { get; set; } = 1m;
    public decimal FraisFournisseurPour5000Fcfa { get; set; }
}

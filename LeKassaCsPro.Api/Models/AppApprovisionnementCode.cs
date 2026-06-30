namespace LeKassaCsPro.Api.Models;

public class AppApprovisionnementCode
{
    public int Id { get; set; }
    public string CodeUnique { get; set; } = string.Empty;
    public string PaysOrigine { get; set; } = "Sénégal";
    public string PaysDestination { get; set; } = "Guinée";
    public decimal Montant { get; set; }
    public decimal FraisFournisseur { get; set; }
    public string Devise { get; set; } = "FCFA";
    public string Statut { get; set; } = "En attente";
    public DateTime DateCreationCode { get; set; } = DateTime.UtcNow;
    public DateTime? DateReception { get; set; }
    public string Observation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string SyncId { get; set; } = Guid.NewGuid().ToString();
    public bool EstSynchronise { get; set; }
    public DateTime DerniereModification { get; set; } = DateTime.UtcNow;
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = "Utilisateur local";
    public string RoleUtilisateur { get; set; } = "Caissier";
    public int UtilisateurReceptionId { get; set; }
    public string UtilisateurReceptionNom { get; set; } = string.Empty;
    public string RoleUtilisateurReception { get; set; } = string.Empty;
    public DateTime? DateCreation { get; set; }
    public DateTime? DateModification { get; set; }
}

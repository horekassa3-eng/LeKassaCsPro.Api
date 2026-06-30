using LeKassaCsPro.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LeKassaCsPro.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUtilisateur> Utilisateurs => Set<AppUtilisateur>();
    public DbSet<AppFournisseur> Fournisseurs => Set<AppFournisseur>();
    public DbSet<AppTransfert> Transferts => Set<AppTransfert>();
    public DbSet<AppTauxChange> TauxChanges => Set<AppTauxChange>();
    public DbSet<AppFournisseurMouvement> FournisseurMouvements => Set<AppFournisseurMouvement>();

    public DbSet<AppProduitVente> ProduitsVente => Set<AppProduitVente>();
    public DbSet<AppStockMouvement> StockMouvements => Set<AppStockMouvement>();
    public DbSet<AppVente> Ventes => Set<AppVente>();
    public DbSet<AppVenteDetail> VenteDetails => Set<AppVenteDetail>();

    public DbSet<AppDepense> Depenses => Set<AppDepense>();
    public DbSet<AppRecetteService> RecettesServices => Set<AppRecetteService>();
    public DbSet<AppBudgetMouvement> BudgetMouvements => Set<AppBudgetMouvement>();

    public DbSet<AppClient> Clients => Set<AppClient>();
    public DbSet<AppEpargneMouvement> EpargneMouvements => Set<AppEpargneMouvement>();
    public DbSet<AppDetteClientMouvement> DetteClientMouvements => Set<AppDetteClientMouvement>();

    public DbSet<AppBoutique> Boutiques => Set<AppBoutique>();
    public DbSet<AppBoutiqueInventaire> BoutiqueInventaires => Set<AppBoutiqueInventaire>();
    public DbSet<AppBoutiqueBudgetMouvement> BoutiqueBudgetMouvements => Set<AppBoutiqueBudgetMouvement>();

    public DbSet<AppParametre> Parametres => Set<AppParametre>();

    public DbSet<AppCodeTransfert> CodeTransferts => Set<AppCodeTransfert>();
    public DbSet<AppSoldeAgenceMouvement> SoldeAgenceMouvements => Set<AppSoldeAgenceMouvement>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUtilisateur>()
            .HasIndex(u => u.NomUtilisateur)
            .IsUnique();

        modelBuilder.Entity<AppUtilisateur>()
            .Property(u => u.NomComplet)
            .HasMaxLength(120);

        modelBuilder.Entity<AppUtilisateur>()
            .Property(u => u.NomUtilisateur)
            .HasMaxLength(60);

        modelBuilder.Entity<AppUtilisateur>()
            .Property(u => u.Role)
            .HasMaxLength(30);

        modelBuilder.Entity<AppTransfert>()
            .Property(t => t.SensTransfert)
            .HasMaxLength(60);

        modelBuilder.Entity<AppTransfert>()
            .Property(t => t.Statut)
            .HasMaxLength(30);

        modelBuilder.Entity<AppFournisseur>()
            .Property(f => f.Nom)
            .HasMaxLength(120);

        modelBuilder.Entity<AppFournisseur>()
            .Property(f => f.Telephone)
            .HasMaxLength(40);

        modelBuilder.Entity<AppFournisseur>()
            .Property(f => f.Pays)
            .HasMaxLength(60);

        modelBuilder.Entity<AppFournisseur>()
            .Property(f => f.Ville)
            .HasMaxLength(80);

        modelBuilder.Entity<AppFournisseurMouvement>()
            .Property(m => m.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppFournisseurMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(60);

        modelBuilder.Entity<AppProduitVente>()
            .Property(p => p.Nom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppProduitVente>()
            .Property(p => p.Unite)
            .HasMaxLength(40);

        modelBuilder.Entity<AppStockMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(30);

        modelBuilder.Entity<AppVente>()
            .Property(v => v.StatutPaiement)
            .HasMaxLength(30);

        modelBuilder.Entity<AppDepense>()
            .Property(d => d.Categorie)
            .HasMaxLength(80);

        modelBuilder.Entity<AppDepense>()
            .Property(d => d.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppDepense>()
            .Property(d => d.ModePaiement)
            .HasMaxLength(80);

        modelBuilder.Entity<AppRecetteService>()
            .Property(r => r.TypeService)
            .HasMaxLength(120);

        modelBuilder.Entity<AppRecetteService>()
            .Property(r => r.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppBudgetMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(30);

        modelBuilder.Entity<AppBudgetMouvement>()
            .Property(m => m.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppBudgetMouvement>()
            .Property(m => m.SourceModule)
            .HasMaxLength(80);

        modelBuilder.Entity<AppClient>()
            .Property(c => c.NomComplet)
            .HasMaxLength(140);

        modelBuilder.Entity<AppClient>()
            .Property(c => c.Telephone)
            .HasMaxLength(40);

        modelBuilder.Entity<AppClient>()
            .Property(c => c.TypeClient)
            .HasMaxLength(40);

        modelBuilder.Entity<AppClient>()
            .Property(c => c.Pays)
            .HasMaxLength(60);

        modelBuilder.Entity<AppClient>()
            .Property(c => c.Ville)
            .HasMaxLength(80);

        modelBuilder.Entity<AppEpargneMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(30);

        modelBuilder.Entity<AppEpargneMouvement>()
            .Property(m => m.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppEpargneMouvement>()
            .Property(m => m.ClientNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppEpargneMouvement>()
            .Property(m => m.ClientTelephone)
            .HasMaxLength(40);

        modelBuilder.Entity<AppDetteClientMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(30);

        modelBuilder.Entity<AppDetteClientMouvement>()
            .Property(m => m.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppDetteClientMouvement>()
            .Property(m => m.ClientNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppDetteClientMouvement>()
            .Property(m => m.ClientTelephone)
            .HasMaxLength(40);

        modelBuilder.Entity<AppDetteClientMouvement>()
            .HasIndex(m => new { m.IsActive, m.DateMouvement });

        modelBuilder.Entity<AppDetteClientMouvement>()
            .HasIndex(m => m.ClientId);

        modelBuilder.Entity<AppDetteClientMouvement>()
            .HasIndex(m => m.Devise);

        modelBuilder.Entity<AppParametre>()
            .Property(p => p.Cle)
            .HasMaxLength(120);

        modelBuilder.Entity<AppParametre>()
            .HasIndex(p => p.Cle);

        modelBuilder.Entity<AppBoutique>()
            .Property(b => b.Nom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppBoutique>()
            .Property(b => b.Pays)
            .HasMaxLength(60);

        modelBuilder.Entity<AppBoutique>()
            .Property(b => b.Ville)
            .HasMaxLength(80);

        modelBuilder.Entity<AppBoutique>()
            .Property(b => b.GerantNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppBoutique>()
            .Property(b => b.GerantTelephone)
            .HasMaxLength(40);

        modelBuilder.Entity<AppBoutique>()
            .Property(b => b.AssistantNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppBoutique>()
            .Property(b => b.AssistantTelephone)
            .HasMaxLength(40);

        modelBuilder.Entity<AppBoutique>()
            .HasIndex(b => new { b.IsActive, b.Nom });

        modelBuilder.Entity<AppBoutique>()
            .HasIndex(b => b.GerantUtilisateurId);

        modelBuilder.Entity<AppBoutique>()
            .HasIndex(b => b.AssistantUtilisateurId);

        modelBuilder.Entity<AppBoutiqueInventaire>()
            .Property(i => i.BoutiqueNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppBoutiqueInventaire>()
            .Property(i => i.UtilisateurNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppBoutiqueInventaire>()
            .Property(i => i.RoleUtilisateur)
            .HasMaxLength(40);

        modelBuilder.Entity<AppBoutiqueInventaire>()
            .HasIndex(i => new { i.BoutiqueId, i.IsActive, i.DateInventaire });

        modelBuilder.Entity<AppBoutiqueBudgetMouvement>()
            .Property(m => m.BoutiqueNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppBoutiqueBudgetMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(60);

        modelBuilder.Entity<AppBoutiqueBudgetMouvement>()
            .Property(m => m.UtilisateurNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppBoutiqueBudgetMouvement>()
            .Property(m => m.RoleUtilisateur)
            .HasMaxLength(40);

        modelBuilder.Entity<AppBoutiqueBudgetMouvement>()
            .HasIndex(m => new { m.BoutiqueId, m.IsActive, m.DateMouvement });

        modelBuilder.Entity<AppBoutiqueBudgetMouvement>()
            .HasIndex(m => m.TypeMouvement);
        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.CodeUnique)
            .HasMaxLength(40);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.Statut)
            .HasMaxLength(40);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.PaysEnvoi)
            .HasMaxLength(60);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.PaysRetrait)
            .HasMaxLength(60);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.NomEnvoyeur)
            .HasMaxLength(140);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.TelephoneEnvoyeur)
            .HasMaxLength(40);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.NomBeneficiaire)
            .HasMaxLength(140);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.TelephoneBeneficiaire)
            .HasMaxLength(40);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.AdresseBeneficiaire)
            .HasMaxLength(180);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.UtilisateurEnvoiNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.RoleUtilisateurEnvoi)
            .HasMaxLength(40);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.UtilisateurRetraitNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppCodeTransfert>()
            .Property(c => c.RoleUtilisateurRetrait)
            .HasMaxLength(40);

        modelBuilder.Entity<AppCodeTransfert>()
            .HasIndex(c => c.CodeUnique)
            .IsUnique();

        modelBuilder.Entity<AppCodeTransfert>()
            .HasIndex(c => new { c.IsActive, c.DateEnvoi });

        modelBuilder.Entity<AppCodeTransfert>()
            .HasIndex(c => new { c.IsActive, c.Statut, c.PaysRetrait });

        modelBuilder.Entity<AppCodeTransfert>()
            .HasIndex(c => c.UtilisateurEnvoiId);

        modelBuilder.Entity<AppCodeTransfert>()
            .HasIndex(c => c.UtilisateurRetraitId);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .Property(m => m.PaysAgence)
            .HasMaxLength(60);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .Property(m => m.Pays)
            .HasMaxLength(60);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .Property(m => m.Moyen)
            .HasMaxLength(80);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .Property(m => m.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(80);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .Property(m => m.UtilisateurNom)
            .HasMaxLength(140);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .Property(m => m.RoleUtilisateur)
            .HasMaxLength(40);

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .HasIndex(m => new { m.IsActive, m.PaysAgence, m.Moyen, m.Devise });

        modelBuilder.Entity<AppSoldeAgenceMouvement>()
            .HasIndex(m => new { m.IsActive, m.DateMouvement });

    }
}

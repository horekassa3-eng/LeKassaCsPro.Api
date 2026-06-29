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

        modelBuilder.Entity<AppTransfert>()
            .Property(t => t.Statut)
            .HasMaxLength(30);

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
    }
}

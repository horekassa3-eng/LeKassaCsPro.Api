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
    public DbSet<AppTransfert> Transferts => Set<AppTransfert>();
    public DbSet<AppTauxChange> TauxChanges => Set<AppTauxChange>();
    public DbSet<AppFournisseurMouvement> FournisseurMouvements => Set<AppFournisseurMouvement>();

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

        modelBuilder.Entity<AppFournisseurMouvement>()
            .Property(m => m.Devise)
            .HasMaxLength(10);

        modelBuilder.Entity<AppFournisseurMouvement>()
            .Property(m => m.TypeMouvement)
            .HasMaxLength(60);
    }
}
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using biblioteca.Models;

namespace biblioteca.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Autor> Autores => Set<Autor>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Lector> Lectores => Set<Lector>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Simulación de RowVersion compatible con MariaDB
        builder.Entity<Libro>().Property(l => l.RowVersion).IsConcurrencyToken();
        builder.Entity<Autor>().Property(a => a.RowVersion).IsConcurrencyToken();
        builder.Entity<Categoria>().Property(c => c.RowVersion).IsConcurrencyToken();
        builder.Entity<Lector>().Property(le => le.RowVersion).IsConcurrencyToken();
        builder.Entity<Prestamo>().Property(p => p.RowVersion).IsConcurrencyToken();

        // 2. Índices únicos
        builder.Entity<Libro>()
            .HasIndex(l => l.ISBN)
            .IsUnique();

        builder.Entity<Lector>()
            .HasIndex(l => l.DocumentoIdentidad)
            .IsUnique();

        builder.Entity<Lector>()
            .HasIndex(l => l.Email)
            .IsUnique();

        // 3. Reglas de integridad referencial (Restrict para no romper historial)
        builder.Entity<Libro>()
            .HasOne(l => l.Autor)
            .WithMany(a => a.Libros)
            .HasForeignKey(l => l.AutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Libro>()
            .HasOne(l => l.Categoria)
            .WithMany(c => c.Libros)
            .HasForeignKey(l => l.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prestamo>()
            .HasOne(p => p.Libro)
            .WithMany(l => l.Prestamos)
            .HasForeignKey(p => p.LibroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prestamo>()
            .HasOne(p => p.Lector)
            .WithMany(le => le.Prestamos)
            .HasForeignKey(p => p.LectorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Lector>()
            .HasOne(le => le.Usuario)
            .WithOne(u => u.Lector)
            .HasForeignKey<Lector>(le => le.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public override int SaveChanges()
    {
        ActualizarRowVersion();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ActualizarRowVersion();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ActualizarRowVersion()
    {
        foreach (var entry in ChangeTracker.Entries<IConcurrencyEntity>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }
}

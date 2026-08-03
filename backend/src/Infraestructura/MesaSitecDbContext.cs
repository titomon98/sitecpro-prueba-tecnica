using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace MesaSitec.Infraestructura.Persistencia;

public class MesaSitecDbContext : DbContext, IAppDbContext
{
    public MesaSitecDbContext(DbContextOptions<MesaSitecDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        base.OnModelCreating(modelo);

        modelo.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Nombre).IsRequired().HasMaxLength(200);
        });

        modelo.Entity<Usuario>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Nombre).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);

            e.HasOne(u => u.Tenant)
                .WithMany(t => t.Usuarios)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelo.Entity<Categoria>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Nombre).IsRequired().HasMaxLength(200);

            e.HasOne(c => c.Tenant)
                .WithMany(t => t.Categorias)
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelo.Entity<Solicitud>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Codigo).IsRequired().HasMaxLength(20);
            e.Property(s => s.Titulo).IsRequired().HasMaxLength(120);
            e.Property(s => s.Descripcion).IsRequired().HasMaxLength(4000);
            e.Property(s => s.Prioridad).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20);

            e.HasIndex(s => new { s.TenantId, s.Codigo }).IsUnique();
            e.HasIndex(s => s.TenantId);

            e.HasOne(s => s.Categoria)
                .WithMany()
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Solicitante)
                .WithMany()
                .HasForeignKey(s => s.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Agente)
                .WithMany()
                .HasForeignKey(s => s.AgenteId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
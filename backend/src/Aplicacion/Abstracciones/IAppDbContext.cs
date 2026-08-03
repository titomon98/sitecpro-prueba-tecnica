using MesaSitec.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
namespace MesaSitec.Aplicacion.Abstracciones;
public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<Categoria> Categorias { get; }
    DbSet<Solicitud> Solicitudes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
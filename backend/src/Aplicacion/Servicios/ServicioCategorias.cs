using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Aplicacion.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MesaSitec.Aplicacion.Servicios;

public class ServicioCategorias
{
    private readonly IAppDbContext _db;
    private readonly IContextoUsuario _contexto;

    public ServicioCategorias(IAppDbContext db, IContextoUsuario contexto)
    {
        _db = db;
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarActivasAsync()
    {
        return await _db.Categorias
        .Where(c => c.TenantId == _contexto.TenantId && c.Activo) //Solo activos
        .OrderBy(c => c.Nombre)
        .Select(c => new CategoriaDto(c.Id, c.Nombre, c.SlaHoras))
        .ToListAsync();
    }
}
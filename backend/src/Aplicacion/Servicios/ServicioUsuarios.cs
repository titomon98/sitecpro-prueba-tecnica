using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesaSitec.Aplicacion.Servicios;

public class ServicioUsuarios
{
    private readonly IAppDbContext _db;
    private readonly IContextoUsuario _ctx;

    public ServicioUsuarios(IAppDbContext db, IContextoUsuario ctx)
    {
        _db = db;
        _ctx = ctx;
    }

    public async Task<IReadOnlyList<ReferenciaDto>> ListarAgentesAsignablesAsync()
    {
        return await _db.Usuarios
        .Where(u => u.TenantId == _ctx.TenantId && u.Activo && (u.Rol == Rol.Agente || u.Rol == Rol.Admin))
        .OrderBy(u => u.Nombre)
        .Select(u => new ReferenciaDto(u.Id, u.Nombre))
        .ToListAsync();
    }
}
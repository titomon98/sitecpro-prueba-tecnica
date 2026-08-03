using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Dominio.Errores;
using Microsoft.EntityFrameworkCore;

namespace MesaSitec.Aplicacion.Servicios;
public class ServicioAuth
{
    private readonly IAppDbContext _db;
    private readonly IHasherContrasenia _hasher;
    private readonly IGeneradorToken _token;
    private readonly IContextoUsuario _contexto;

    public ServicioAuth(IAppDbContext db, IHasherContrasenia hasher, IGeneradorToken token, IContextoUsuario contexto)
    {
        _db = db;
        _hasher = hasher;
        _token = token;
        _contexto = contexto;
    }
    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();

        var usuario = await _db.Usuarios
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !usuario.Activo || usuario.Tenant is null || !usuario.Tenant.Activo)
            throw new NoAutenticadoException("Credenciales invalidas.");

        if (!_hasher.Verificar(req.Password ?? string.Empty, usuario.PasswordHash))
            throw new NoAutenticadoException("Credenciales invalidas.");

        var (token, expiraEn) = _token.Generar(usuario);

        return new LoginResponse(token, expiraEn, MapearUsuario(usuario));
    }

    //Devuelve el perfil del usuario autenticado
    public async Task<UsuarioDto> PerfilActualAsync()
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == _contexto.UsuarioId);

        // El token es valido pero el usuario ya no existe o esta inactivo.
        if (usuario is null || !usuario.Activo || usuario.Tenant is null)
            throw new NoAutenticadoException();

        return MapearUsuario(usuario);
    }

    private static UsuarioDto MapearUsuario(Dominio.Entidades.Usuario u) =>
        new(u.Id, u.Nombre, u.Email, u.Rol, u.TenantId, u.Tenant!.Nombre);
}
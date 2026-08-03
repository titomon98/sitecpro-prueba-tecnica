using System.Security.Claims;
using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Dominio.Enums;
using MesaSitec.Dominio.Errores;

namespace MesaSitec.Api.Autenticacion;
public class ContextoUsuarioHttp : IContextoUsuario
{
    private readonly ClaimsPrincipal? _usuario;

    public ContextoUsuarioHttp(IHttpContextAccessor accessor)
    {
        _usuario = accessor.HttpContext?.User;
    }

    public Guid UsuarioId => LeerGuid("sub", ClaimTypes.NameIdentifier);
    public Guid TenantId => LeerGuid("tenantId");
    public string Email => LeerTexto("email");

    public Rol Rol
    {
        get
        {
            var valor = LeerTexto("rol");
            if (Enum.TryParse<Rol>(valor, ignoreCase: true, out var rol))
                return rol;
            throw new NoAutenticadoException();
        }
    }

    private string LeerTexto(params string[] tipos)
    {
        foreach (var tipo in tipos)
        {
            var valor = _usuario?.FindFirst(tipo)?.Value;
            if (!string.IsNullOrEmpty(valor)) return valor;
        }
        throw new NoAutenticadoException();
    }

    private Guid LeerGuid(params string[] tipos)
    {
        var valor = LeerTexto(tipos);
        return Guid.TryParse(valor, out var guid) ? guid : throw new NoAutenticadoException();
    }
}
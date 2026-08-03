using MesaSitec.Dominio.Enums;
namespace MesaSitec.Aplicacion.Abstracciones;
public interface IContextoUsuario
{ 
    Guid UsuarioId { get; } 
    Guid TenantId { get; } 
    Rol Rol { get; } string 
    Email { get; } 
}
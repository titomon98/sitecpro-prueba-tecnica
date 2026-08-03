using MesaSitec.Dominio.Enums;
namespace MesaSitec.Aplicacion.DTOs;
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, int ExpiraEn, UsuarioDto Usuario);
public record UsuarioDto(Guid Id, string Nombre, string Email, Rol Rol, Guid TenantId, string TenantNombre);
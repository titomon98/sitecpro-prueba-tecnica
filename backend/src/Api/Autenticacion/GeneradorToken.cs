using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MesaSitec.Aplicacion.Abstracciones;
using MesaSitec.Dominio.Entidades;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MesaSitec.Api.Autenticacion;

/// <summary>
/// Genera el JWT firmado con HS256. Incluye los claims minimos exigidos (5.1):
/// sub (id de usuario), tenantId, rol y email. Expira en las horas configuradas (8).
/// </summary>
public class GeneradorToken : IGeneradorToken
{
    private readonly OpcionesJwt _opciones;

    public GeneradorToken(IOptions<OpcionesJwt> opciones) => _opciones = opciones.Value;

    public (string token, int expiraEnSegundos) Generar(Usuario usuario)
    {
        var expiraEnSegundos = _opciones.ExpiraHoras * 3600;
        var ahora = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new("tenantId", usuario.TenantId.ToString()),
            new("rol", usuario.Rol.ToString()),
            new("email", usuario.Email),
            // jti: identificador unico del token (buena practica)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credenciales = new SigningCredentials(_opciones.ObtenerClave(), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Issuer,
            audience: _opciones.Audience,
            claims: claims,
            notBefore: ahora,
            expires: ahora.AddHours(_opciones.ExpiraHoras),
            signingCredentials: credenciales);

        var tokenTexto = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenTexto, expiraEnSegundos);
    }
}
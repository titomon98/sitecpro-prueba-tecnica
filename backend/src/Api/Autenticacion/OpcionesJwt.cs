using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace MesaSitec.Api.Autenticacion;
public class OpcionesJwt
{
    public const string Seccion = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "MesaSitec";
    public string Audience { get; set; } = "MesaSitec";
    public int ExpiraHoras { get; set; } = 8;
    public SymmetricSecurityKey ObtenerClave() =>
    new(SHA256.HashData(Encoding.UTF8.GetBytes(Secret)));
}
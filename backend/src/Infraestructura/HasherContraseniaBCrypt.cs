//La contrasenia no puede ir en texto plano, va con bcrypt
using MesaSitec.Aplicacion.Abstracciones;

namespace MesaSitec.Infraestructura.Servicios;
public class HasherContrasenaBCrypt : IHasherContrasenia
{
    public string Hashear(string contrasenaPlana) => BCrypt.Net.BCrypt.HashPassword(contrasenaPlana);

    public bool Verificar(string contrasenaPlana, string hash) => BCrypt.Net.BCrypt.Verify(contrasenaPlana, hash);
}
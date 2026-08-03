using MesaSitec.Aplicacion.Abstracciones;

namespace MesaSitec.Infraestructura.Servicios;

//Devuelve la hora actual en UTC.
public class ProveedorFechaSistema : IProveedorFecha
{
    public DateTime AhoraUtc => DateTime.UtcNow;
}
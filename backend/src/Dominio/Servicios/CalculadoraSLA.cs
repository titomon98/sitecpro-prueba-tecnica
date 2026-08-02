using MesaSitec.Dominio.Enums;

namespace MesaSitec.Dominio.Servicios;

//Calcula la fecha limite de SLA de una solicitud
//factor = { Critica: 0.5, Alta: 0.75, Media: 1.0, Baja: 2.0 }
//fechaLimiteSla = fechaCreacion + (slaHorasCategoria * factor[prioridad]) horas
public static class CalculadoraSLA
{
    private static double Factor(Prioridad prioridad) => prioridad switch
    {
        Prioridad.Critica => 0.5,
        Prioridad.Alta => 0.75,
        Prioridad.Media => 1.0,
        Prioridad.Baja =>2.0,
        _ => throw new ArgumentOutOfRangeException(nameof(prioridad), prioridad, "Prioridad no encontrada.")
    };

    public static DateTime CalcularFechaLimite(DateTime fechaCreacionUtc, int slaHorasCategoria, Prioridad prioridad)
    {
        var horas = slaHorasCategoria * Factor(prioridad);
        return fechaCreacionUtc.AddHours(horas);
    }
}
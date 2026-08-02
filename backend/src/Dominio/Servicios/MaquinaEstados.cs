using MesaSitec.Dominio.Enums;
using MesaSitec.Dominio.Errores;

namespace MesaSitec.Dominio.Servicios;
//Esta maquina de estados es parte de la regla de negocio 2. No depende de la base de datos ni de 
//HTTP, la idea es poder probarlo desde aqui. Cualquier accion que no aparezca para el estado actual se considera
//invalida y lanza la excepcion 409
public static class MaquinaEstados
{
    private static readonly IReadOnlyDictionary<EstadoSolicitud, IReadOnlyDictionary<AccionSolicitud, EstadoSolicitud>> Transiciones =
        new Dictionary<EstadoSolicitud, IReadOnlyDictionary<AccionSolicitud, EstadoSolicitud>>
        {
            [EstadoSolicitud.Nueva] = new Dictionary<AccionSolicitud, EstadoSolicitud>
            {
                [AccionSolicitud.Asignar] = EstadoSolicitud.Asignada,
                [AccionSolicitud.Cancelar] = EstadoSolicitud.Cancelada,
            },
            [EstadoSolicitud.Asignada] = new Dictionary<AccionSolicitud, EstadoSolicitud>
            {
                [AccionSolicitud.Iniciar] = EstadoSolicitud.EnProceso,
                [AccionSolicitud.Asignar] = EstadoSolicitud.Asignada, //reasignar a otro agente
                [AccionSolicitud.Cancelar] = EstadoSolicitud.Cancelada,
            },
            [EstadoSolicitud.EnProceso] = new Dictionary<AccionSolicitud, EstadoSolicitud>
            {
                [AccionSolicitud.Resolver] = EstadoSolicitud.Resuelta,
                [AccionSolicitud.Asignar] = EstadoSolicitud.Asignada,
                [AccionSolicitud.Cancelar] = EstadoSolicitud.Cancelada,
            },
            [EstadoSolicitud.Resuelta] = new Dictionary<AccionSolicitud, EstadoSolicitud>
            {
                [AccionSolicitud.Cerrar] = EstadoSolicitud.Cerrada,
                [AccionSolicitud.Reabrir] = EstadoSolicitud.EnProceso,
            },
        };

    public static bool EsTransicionValida(EstadoSolicitud estadoActual, AccionSolicitud accion)
    {
        return Transiciones.TryGetValue(estadoActual, out var permitidas) && permitidas.ContainsKey(accion);
    }

    public static EstadoSolicitud Siguiente(EstadoSolicitud estadoActual, AccionSolicitud accion)
    {
        if (Transiciones.TryGetValue(estadoActual, out var permitidas)
            && permitidas.TryGetValue(accion, out var destino))
        {
            return destino;
        }

        throw new TransicionInvalidaException(
            $"No se puede aplicar '{accion.ToString().ToLowerInvariant()}' sobre una solicitud en estado '{estadoActual}'.");
    }
    public static IReadOnlyCollection<AccionSolicitud> AccionesDisponibles(EstadoSolicitud estadoActual)
    {
        return Transiciones.TryGetValue(estadoActual, out var permitidas)
            ? permitidas.Keys.ToArray()
            : Array.Empty<AccionSolicitud>();
    }
}
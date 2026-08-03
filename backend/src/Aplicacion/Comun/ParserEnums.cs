using MesaSitec.Dominio.Enums;
using MesaSitec.Dominio.Errores;

namespace MesaSitec.Aplicacion.Comun;

public static class ParserEnums
{
    private static bool IntentarParsear<T>(string valor, out T resultado) where T : struct, Enum
    {
        foreach (var nombre in Enum.GetNames<T>())
        {
            if (string.Equals(nombre, valor, StringComparison.OrdinalIgnoreCase))
            {
                resultado = Enum.Parse<T>(nombre);
                return true;
            }
        }
        resultado = default;
        return false;
    }

    public static EstadoSolicitud? EstadoFiltro(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (IntentarParsear<EstadoSolicitud>(valor, out var estado)) return estado;
        throw new ParametroInvalidoException($"El estado '{valor}' no es valido.");
    }
    public static Prioridad? PrioridadFiltro(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (IntentarParsear<Prioridad>(valor, out var prioridad)) return prioridad;
        throw new ParametroInvalidoException($"La prioridad '{valor}' no es valida.");
    }
    public static AccionSolicitud Accion(string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor) && IntentarParsear<AccionSolicitud>(valor, out var accion))
            return accion;

        var errores = new Dictionary<string, string[]>
        {
            ["accion"] = new[] { "La accion indicada no es valida. Use: asignar, iniciar, resolver, cerrar, reabrir o cancelar." }
        };
        throw new ValidacionException(errores);
    }
}
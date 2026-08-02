namespace MesaSitec.Dominio.Errores;

//Este archivo reune todas las excepciones concretas de negocio.
//Cada una fija su codigo HTTP y su "codigo" textual como lo indicado en la seccion 6.1
//del enunciado. El middleware de la Api las convierte a problem+json.

//Codigo 401, no autenticado por falta de token, vencimiento de token.
public sealed class NoAutenticadoException : ExcepcionAplicacion
{
    public NoAutenticadoException(string detalle = "No autenticado.")
        : base(401, "NO_AUTENTICADO", "No autenticado", detalle) { }
}

//Codigo 403, la operación no es permitida
public sealed class OperacionNoPermitidaException : ExcepcionAplicacion
{
    public OperacionNoPermitidaException(string detalle = "El rol no permite esta operacion.")
        : base(403, "OPERACION_NO_PERMITIDA", "Operacion no permitida", detalle) { }
}

//Codigo 404, recurso inexistente o de otra organizacion. Esta fue una solicitud explicita en los requerimientos.
//Se responde 404 a proposito, asi el usuario no puede distinguir si existe o no existe.
public sealed class RecursoNoEncontradoException : ExcepcionAplicacion
{
    public RecursoNoEncontradoException(string detalle = "El recurso no existe.")
        : base(404, "RECURSO_NO_ENCONTRADO", "Recurso no encontrado", detalle) { }
}

//Transicion de estado no permitida por la maquina de estados, regla de negocio 2
public sealed class TransicionInvalidaException : ExcepcionAplicacion
{
    public TransicionInvalidaException(string detalle)
        : base(409, "TRANSICION_INVALIDA", "Transicion invalida", detalle) { }
}

//Codigio 422, el agente indicado al asignar no cumple las condiciones necesarias, regla de negocio 5
public sealed class AgenteInvalidoException : ExcepcionAplicacion
{
    public AgenteInvalidoException(string detalle = "El agente indicado no es valido.")
        : base(422, "AGENTE_INVALIDO", "Agente invalido", detalle) { }
}

//Codigo 422, falta el motivo o es demasiado corto al resolver, regla de negocio 6
public sealed class MotivoRequeridoException : ExcepcionAplicacion
{
    public MotivoRequeridoException(string detalle)
        : base(422, "MOTIVO_REQUERIDO", "Motivo requerido", detalle) { }
}

//Codigo 400 de bad request, un parametro de la consulta esta fuera de rango
public sealed class ParametroInvalidoException : ExcepcionAplicacion
{
    public ParametroInvalidoException(string detalle)
        : base(400, "PARAMETRO_INVALIDO", "Parametro invalido", detalle) { }
}

//Codigo 422 por error de validacion de campos del cuerpo.
public sealed class ValidacionException : ExcepcionAplicacion
{
    public ValidacionException(IReadOnlyDictionary<string, string[]> errores,
        string detalle = "Uno o mas campos no son validos.")
        : base(422, "VALIDACION", "Error de validacion", detalle, errores) { }
}
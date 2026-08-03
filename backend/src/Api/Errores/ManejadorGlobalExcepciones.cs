using MesaSitec.Dominio.Errores;
using Microsoft.AspNetCore.Diagnostics;

namespace MesaSitec.Api.Errores;
public class ManejadorGlobalExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorGlobalExcepciones> _log;

    public ManejadorGlobalExcepciones(ILogger<ManejadorGlobalExcepciones> log) => _log = log;

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is ExcepcionAplicacion appEx)
        {
            // Errores esperados de negocio: se registran como advertencia, no como fallo grave.
            _log.LogWarning("Error de negocio {Codigo}: {Detalle}", appEx.Codigo, appEx.Message);
            await EscritorProblema.EscribirAsync(
                ctx, appEx.StatusHttp, appEx.Codigo, appEx.Titulo, appEx.Message, appEx.Errores);
            return true;
        }

        //Cualquier otra excepcion es inesperada se loguea completa pero al cliente solo le llega un 500 generico
        _log.LogError(ex, "Excepcion no controlada");
        await EscritorProblema.EscribirAsync(
            ctx, 500, "ERROR_INTERNO", "Error interno del servidor",
            "Ocurrio un error inesperado. Intentelo de nuevo mas tarde.");
        return true;
    }
}
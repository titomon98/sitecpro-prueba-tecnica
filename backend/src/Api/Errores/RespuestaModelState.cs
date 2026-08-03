using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MesaSitec.Api.Errores;

public static class RespuestaModelState
{
    public static IActionResult Construir(ActionContext contexto)
    {
        var errores = contexto.ModelState
            .Where(kv => kv.Value is not null && kv.Value.Errors.Count > 0)
            .ToDictionary(
                kv => NormalizarClave(kv.Key),
                kv => kv.Value!.Errors
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor no valido." : e.ErrorMessage)
                    .ToArray());

        var cuerpo = new CuerpoProblema
        {
            Type = "https://mesasitec.local/errores/validacion",
            Title = "Error de validacion",
            Status = 422,
            Detail = "Uno o mas campos no son validos.",
            Codigo = "VALIDACION",
            Errores = errores
        };

        return new ObjectResult(cuerpo)
        {
            StatusCode = 422,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static string NormalizarClave(string clave)
    {
        var limpia = clave.Replace("$.", string.Empty).Trim();
        if (string.IsNullOrEmpty(limpia)) return "cuerpo";
        // Primera letra en minuscula para mantener camelCase
        return char.ToLowerInvariant(limpia[0]) + limpia[1..];
    }
}
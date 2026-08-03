using System.Text.Json;
using System.Text.Json.Serialization;

namespace MesaSitec.Api.Errores;

/// Forma del cuerpo de error del contrato (seccion 6.1), en application/problem+json.
/// 'codigo' es obligatorio en todos los errores; 'errores' solo aparece en validaciones.
public class CuerpoProblema
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("detail")] public string Detail { get; set; } = string.Empty;
    [JsonPropertyName("codigo")] public string Codigo { get; set; } = string.Empty;

    //Se omite del JSON cuando es null
    [JsonPropertyName("errores")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errores { get; set; }
}

/// Escribe una respuesta de error unificada. Lo usan tanto el manejador global de
/// excepciones como los eventos de JWT (401), para que TODOS los errores tengan la
/// misma forma y siempre incluyan 'codigo'.
public static class EscritorProblema
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task EscribirAsync(
        HttpContext ctx, int status, string codigo, string titulo, string detalle,
        IReadOnlyDictionary<string, string[]>? errores = null)
    {
        var cuerpo = new CuerpoProblema
        {
            Type = $"https://mesasitec.local/errores/{codigo.ToLowerInvariant().Replace('_', '-')}",
            Title = titulo,
            Status = status,
            Detail = detalle,
            Codigo = codigo,
            Errores = errores
        };

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, Opciones));
    }
}
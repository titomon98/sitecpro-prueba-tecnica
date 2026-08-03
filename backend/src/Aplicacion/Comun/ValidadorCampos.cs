using MesaSitec.Dominio.Errores;
namespace MesaSitec.Aplicacion.Comun;
public class ValidadorCampos
{
    private readonly Dictionary<string, List<string>> _errores = new();
    public void Agregar(string campo, string msg)
    {
        if (!_errores.TryGetValue(campo, out var l)) { l = new(); _errores[campo] = l; }
        l.Add(msg);
    }
    public void ValidarLongitud(string campo, string? valor, int min, int max)
    {
        var t = valor?.Trim() ?? string.Empty;
        if (t.Length < min || t.Length > max) Agregar(campo, $"Debe tener entre {min} y {max} caracteres.");
    }
    public void LanzarSiHayErrores()
    {
        if (_errores.Count == 0) return;
        throw new ValidacionException(_errores.ToDictionary(k => k.Key, k => k.Value.ToArray()));
    }
}
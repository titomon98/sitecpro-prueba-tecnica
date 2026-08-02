namespace MesaSitec.Dominio.Errores;
public abstract class ExcepcionAplicacion : Exception
{
    public int StatusHttp { get; }
    public string Codigo { get; }
    public string Titulo { get; }
    public IReadOnlyDictionary<string, string[]>? Errores { get; }
    protected ExcepcionAplicacion(int statusHttp, string codigo, string titulo, string detalle,
    IReadOnlyDictionary<string, string[]>? errores = null) : base(detalle)
    {
        StatusHttp = statusHttp; Codigo = codigo; Titulo = titulo; Errores = errores;
    }
}
namespace MesaSitec.Aplicacion.DTOs;
public class ConsultaSolicitudes
{
    public string? Estado { get; set; }
    public string? Prioridad { get; set; }
    public Guid? CategoriaId { get; set; }
    public Guid? AgenteId { get; set; }
    public string? Q { get; set; }
    public bool? Vencidas { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; } =
    "-fechaCreacion";
}
using MesaSitec.Dominio.Enums;
namespace MesaSitec.Dominio.Entidades;

public class Solicitud
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public Prioridad Prioridad { get; set; }
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Nueva;
    public Guid SolicitanteId { get; set; }
    public Guid? AgenteId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaLimiteSla { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string? MotivoResolucion { get; set; }
    public string? MotivoCancelacion { get; set; }
    public Categoria? Categoria { get; set; }
    public Usuario? Solicitante { get; set; }
    public Usuario? Agente { get; set; }
    // Vencida si el límite pasó y el estado no es terminada
    public bool EstaVencida(DateTime ahoraUtc)
    {
        var cuenta = Estado is not (EstadoSolicitud.Resuelta or EstadoSolicitud.Cerrada or EstadoSolicitud.Cancelada);
        return cuenta && FechaLimiteSla < ahoraUtc;
    }
}
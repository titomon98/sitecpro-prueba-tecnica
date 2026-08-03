using MesaSitec.Dominio.Enums;
namespace MesaSitec.Aplicacion.DTOs;
public record SolicitudListaDto(Guid Id, string Codigo, string Titulo, 
EstadoSolicitud Estado, Prioridad Prioridad, ReferenciaDto Categoria, ReferenciaDto? Agente, 
DateTime FechaCreacion, DateTime FechaLimiteSla, bool Vencida);
public record SolicitudDetalleDto(Guid Id, string Codigo, string Titulo, string Descripcion, 
EstadoSolicitud Estado, Prioridad Prioridad, ReferenciaDto Categoria, ReferenciaDto Solicitante, 
ReferenciaDto? Agente, DateTime FechaCreacion, DateTime FechaLimiteSla, DateTime? FechaResolucion, 
string? MotivoResolucion, string? MotivoCancelacion, bool Vencida, IReadOnlyList<string> AccionesDisponibles);
public record CrearSolicitudRequest(string Titulo, string Descripcion, Guid CategoriaId, Prioridad Prioridad);
public record EditarSolicitudRequest(string Titulo, string Descripcion, Guid CategoriaId, Prioridad Prioridad);
public record TransicionRequest(string Accion, Guid? AgenteId, string? Motivo);
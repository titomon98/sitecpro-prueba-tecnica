namespace MesaSitec.Aplicacion.DTOs;
public record ReferenciaDto(Guid Id, string Nombre);
public record CategoriaDto(Guid Id, string Nombre, int SlaHoras);
public record PaginaDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total, int TotalPaginas);
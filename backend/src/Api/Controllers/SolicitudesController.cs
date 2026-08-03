using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesaSitec.Api.Controllers;

[ApiController]
[Route("api/v1/solicitudes")]
[Authorize]
public class SolicitudesController : ControllerBase
{
    private readonly ServicioSolicitudes _solicitudes;

    public SolicitudesController(ServicioSolicitudes solicitudes) => _solicitudes = solicitudes;

    //listar
    [HttpGet]
    public async Task<ActionResult<PaginaDto<SolicitudListaDto>>> Listar([FromQuery] ConsultaSolicitudes consulta)
    {
        var pagina = await _solicitudes.ListarAsync(consulta);
        return Ok(pagina);
    }

    // Crear solicitud, se usa codigo 201
    [HttpPost]
    public async Task<ActionResult<SolicitudDetalleDto>> Crear([FromBody] CrearSolicitudRequest req)
    {
        var creada = await _solicitudes.CrearAsync(req);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creada.Id }, creada);
    }

    // obtener solciitud por id
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SolicitudDetalleDto>> ObtenerPorId(Guid id)
    {
        var detalle = await _solicitudes.ObtenerDetalleAsync(id);
        return Ok(detalle);
    }

    //editar solicitud
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SolicitudDetalleDto>> Editar(Guid id, [FromBody] EditarSolicitudRequest req)
    {
        var actualizada = await _solicitudes.EditarAsync(id, req);
        return Ok(actualizada);
    }

    //ejecutar una transicion
    [HttpPost("{id:guid}/transiciones")]
    public async Task<ActionResult<SolicitudDetalleDto>> Transicionar(Guid id, [FromBody] TransicionRequest req)
    {
        var actualizada = await _solicitudes.EjecutarTransicionAsync(id, req);
        return Ok(actualizada);
    }
}

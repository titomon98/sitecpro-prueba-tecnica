using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesaSitec.Api.Controllers;

[ApiController]
[Route("api/v1/agentes")]
[Authorize]
public class AgentesController : ControllerBase
{
    private readonly ServicioUsuarios _usuarios;

    public AgentesController(ServicioUsuarios usuarios) => _usuarios = usuarios;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReferenciaDto>>> Listar()
    {
        var agentes = await _usuarios.ListarAgentesAsignablesAsync();
        return Ok(agentes);
    }
}

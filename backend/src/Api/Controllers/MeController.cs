using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesaSitec.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly ServicioAuth _auth;

    public MeController(ServicioAuth auth) => _auth = auth;

    //Perfil el usuario
    [HttpGet]
    public async Task<ActionResult<UsuarioDto>> Yo()
    {
        var usuario = await _auth.PerfilActualAsync();
        return Ok(usuario);
    }
}

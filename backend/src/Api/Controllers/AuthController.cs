using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesaSitec.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ServicioAuth _auth;

    public AuthController(ServicioAuth auth) => _auth = auth;

    //login, no requiere token
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var respuesta = await _auth.LoginAsync(req);
        return Ok(respuesta);
    }
}

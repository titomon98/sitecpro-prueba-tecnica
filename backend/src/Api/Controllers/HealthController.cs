using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesaSitec.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    // ve la salud de la API, tampoco requiere autenticacion
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Estado() => Ok(new { estado = "ok" });
}

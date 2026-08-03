using MesaSitec.Aplicacion.DTOs;
using MesaSitec.Aplicacion.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesaSitec.Api.Controllers;

[ApiController]
[Route("api/v1/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ServicioCategorias _categorias;

    public CategoriasController(ServicioCategorias categorias) => _categorias = categorias;

    //listar categorias
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar()
    {
        var lista = await _categorias.ListarActivasAsync();
        return Ok(lista);
    }
}

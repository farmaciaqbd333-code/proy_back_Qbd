using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/prod")]
public class ProductoController : ControllerBase
{

    private readonly IProductoService _productoService;

    public ProductoController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    [HttpGet()]
    [SwaggerResponse(200, "Obtencion exitosa", typeof(ProductoRes))]
    public async Task<IActionResult> Obtener()
    {
        List<ProductoRes>? response = new();

        response = await _productoService.Obtener();

        return Ok(response);
    }

    [HttpPost]
    [SwaggerResponse(201, "Creacion exitosa", typeof(Producto))]
    public async Task<IActionResult> Crear([FromBody] ProductoReq request)
    {
        var response = await _productoService.Crear(request);

        if (response == null) return BadRequest("Error al crear el producto");

        return CreatedAtAction(nameof(Obtener), new { id = response.Id }, response);
    }

    [HttpPut("{id}")]
    [SwaggerResponse(200, "Actualizacion exitosa", typeof(Producto))]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ProductoReq request)
    {
        var response = await _productoService.Actualizar(id, request);

        if (response == null) return NotFound("Producto no encontrado");

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [SwaggerResponse(204, "Eliminacion exitosa")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var response = await _productoService.Eliminar(id);

        if (!response) return NotFound("Producto no encontrado");

        return NoContent();
    }
}

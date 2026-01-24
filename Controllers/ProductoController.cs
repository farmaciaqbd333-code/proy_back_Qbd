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

    public ProductoController(IProductoService prodTermService)
    {
        _productoService = prodTermService;
    }

    [HttpGet()]
    [SwaggerResponse(200, "Obtencion exitosa", typeof(ProductoRes))]
    public async Task<IActionResult> Obtener()
    {
        List<ProductoRes>? response = new();

        response = await _productoService.Obtener();

        return Ok(response);
    }
    
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Insumo;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/insumo")]
public class InsumoController : ControllerBase
{

    private readonly IInsumoService _insumoService;

    public InsumoController(IInsumoService insumoService)
    {
        _insumoService = insumoService;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(Insumo))]
    public async Task<IActionResult> CrearInsumo([FromBody] InsumoCreateReq request)
    {
        Insumo? insumo = await _insumoService.Crear(request);
        if (insumo == null)
        {
            NotFound("No existe");
        }
        return Ok(insumo);
    }

    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(Insumo))]
    public async Task<IActionResult> EliminarInsumo(int id)
    {
        Insumo? insumo = await _insumoService.Eliminar(id);
        if (insumo == null)
        {
            NotFound("No existe");
        }
        return Ok(insumo);
    }

    [HttpPut("{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(InsumoUpdateReq))]
    public async Task<IActionResult> ActualizarInsumo(int id, InsumoUpdateReq request)
    {
        Insumo? insumo = await _insumoService.Actualizar(id, request);
        if (insumo == null)
        {
            NotFound("No existe");
        }
        return Ok(insumo);
    }

    [HttpGet()]
    [SwaggerResponse(200, "Operación exitosa", typeof(InsumoFindAllRes))]
    public async Task<IActionResult> ObtenerInsumos()
    {
        List<InsumoFindAllRes?> insumo = await _insumoService.Obtener();
        if (insumo == null)
        {
            NotFound("No existen");
        }
        return Ok(insumo);
    }

    [HttpGet("{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(InsumoFindIdRes))]
    public async Task<IActionResult> ObtenerInsumosPorId(int id)
    {
        InsumoFindIdRes? insumo = await _insumoService.ObtenerById(id);
        if (insumo == null)
        {
            NotFound("No existe");
        }
        return Ok(insumo);
    }
    

}

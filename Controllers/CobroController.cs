using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Response.Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/cobro")]
public class CobroController : ControllerBase
{

    private readonly ICobroService _cobroService;

    public CobroController(ICobroService cobroService)
    {
        _cobroService = cobroService;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(CobroCreateRes))]
    public async Task<IActionResult> CrearCobro([FromBody] CobroCreateReq request)
    {
        if (request == null)
        {
            return BadRequest("Request cannot be null");
        }
        CobroCreateRes? response = await _cobroService.Crear(request);
        return Ok(response);
    }
    [HttpGet("{pedidoId}/{sedeId}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(List<CobroByPedido>))]
    public async Task<IActionResult> ListarCobros(int pedidoId, int sedeId)
    {
        List<CobroByPedido>? response = await _cobroService.Obtener(pedidoId, sedeId);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }

    [HttpPut("{id}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(CobroCreateRes))]
    public async Task<IActionResult> ActualizarCobro(int id, int sedeId, [FromBody] CobroUpdateReq request)
    {
        if (request == null)
        {
            return BadRequest("Datos incorrectos");
        }
        CobroCreateRes? response = await _cobroService.Actualizar(id, sedeId, request);

        return Ok(response);
    }


    [HttpDelete("{cobroId}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(Cobro))]
    public async Task<IActionResult> EliminarCobro(int cobroId, int sedeId)
    {
        Cobro? response = await _cobroService.Eliminar(cobroId, sedeId);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }
}

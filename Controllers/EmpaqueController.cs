using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Empaque;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/empaque")]
public class EmpaqueController : ControllerBase
{

    private readonly IEmpaqueService _empaqueService;

    public EmpaqueController(IEmpaqueService empaqueService)
    {
        _empaqueService = empaqueService;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(Empaque))]
    public async Task<IActionResult> CrearEmpaque([FromBody] EmpaqueCreateReq request)
    {
        Empaque? empaque = await _empaqueService.Crear(request);
        if (empaque == null)
        {
            NotFound("No existe");
        }
        return Ok(empaque);
    }

    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(Empaque))]
    public async Task<IActionResult> EliminarEmpaque(int id)
    {
        Empaque? empaque = await _empaqueService.Eliminar(id);
        if (empaque == null)
        {
            NotFound("No existe");
        }
        return Ok(empaque);
    }

    [HttpPut("{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(EmpaqueUpdateReq))]
    public async Task<IActionResult> ActualizarEmpaque(int id, EmpaqueUpdateReq request)
    {
        Empaque? empaque = await _empaqueService.Actualizar(id, request);
        if (empaque == null)
        {
            NotFound("No existe");
        }
        return Ok(empaque);
    }

    [HttpGet()]
    [SwaggerResponse(200, "Operación exitosa", typeof(EmpaqueFindAllRes))]
    public async Task<IActionResult> ObtenerEmpaques()
    {
        List<EmpaqueFindAllRes?> empaque = await _empaqueService.Obtener();
        if (empaque == null)
        {
            NotFound("No existen");
        }
        return Ok(empaque);
    }

    [HttpGet("{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(EmpaqueFindIdRes))]
    public async Task<IActionResult> ObtenerEmpaquesPorId(int id)
    {
        EmpaqueFindIdRes? empaque = await _empaqueService.ObtenerById(id);
        if (empaque == null)
        {
            NotFound("No existe");
        }
        return Ok(empaque);
    }

}

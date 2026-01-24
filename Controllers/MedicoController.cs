using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/medico")]
public class MedicoController : ControllerBase
{

    private readonly IMedicoService _medicoService;
    public MedicoController(IMedicoService medicoService)
    {
        _medicoService = medicoService;
    }

    [HttpPost]
    [SwaggerResponse(200, "Creacion exitosa", typeof(MedicoCreateResponse))]
    public async Task<IActionResult> CrearMedico([FromBody] MedicoCreateReq request)
    {
        if (request == null)
        {
            return BadRequest("Datos incorrectos");
        }
        MedicoCreateResponse? response = await _medicoService.Crear(request);

        return Ok(response);
    }

    [HttpPut("{id}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(MedicoUpdateResponse))]
    public async Task<IActionResult> ActualizarMedico(int id, [FromBody] MedicoUpdateReq request)
    {
        if (request == null)
        {
            return BadRequest("Datos incorrectos");
        }
        MedicoUpdateResponse? response = await _medicoService.Actualizar(id, request);

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(Medico))]
    public async Task<IActionResult> EliminarMedico(int id)
    {
        if (id == null)
        {
            return BadRequest("Datos incorrectos");
        }
        Medico? response = await _medicoService.Eliminar(id);

        return Ok(response);
    }
    [HttpGet("sede/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(List<MedicoFindAllResponse?>))]
    public async Task<IActionResult> ObtenerMedicos(int sedeId)
    {
        List<MedicoFindAllResponse?> response = await _medicoService.Obtener(sedeId);

        return Ok(response);
    }
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(MedicoFindIdResponse))]
    public async Task<IActionResult> ObtenerMedicosById(int id)
    {
        MedicoFindIdResponse? response = await _medicoService.ObtenerById(id);
        if (response == null)
        {
            return NotFound("No se encontró el medico");
        }

        return Ok(response);
    }

}

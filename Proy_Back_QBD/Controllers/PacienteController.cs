using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/paciente")]
public class PacienteController : ControllerBase
{

    private readonly IPacienteService _pacienteService;

    public PacienteController(IPacienteService pacienteService)
    {
        _pacienteService = pacienteService;
    }

    [HttpPost()]
    [SwaggerResponse(200, "Operación exitosa", typeof(PacienteCreateResponse))]
    public async Task<IActionResult> CrearPaciente([FromBody] PacienteCreateReq request)
    {
        if (request == null)
        {
            return BadRequest("Request cannot be null");
        }
        PacienteCreateResponse? response = await _pacienteService.Crear(request);
        return Ok(response);
    }
    [HttpPut("{id}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(PacienteUpdateReq))]
    public async Task<IActionResult> ActualizarPaciente(int id, [FromBody] PacienteUpdateReq request)
    {
        if (request == null)
        {
            return BadRequest("Datos incorrectos");
        }
        PacienteUpdateResponse? response = await _pacienteService.Actualizar(id, request);

        return Ok(response);
    }
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(Paciente))]
    public async Task<IActionResult> EliminarPaciente(int id)
    {
        if (id == null)
        {
            return BadRequest("Datos incorrectos");
        }
        Paciente? response = await _pacienteService.Eliminar(id);

        return Ok(response);
    }
    [HttpGet("sede/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(List<PacienteFindAllResponse?>))]
    public async Task<IActionResult> ObtenerPacientes(int sedeId)
    {
        List<PacienteFindAllResponse?> response = await _pacienteService.Obtener(sedeId);

        return Ok(response);
    }
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(PacienteFindIdResponse))]
    public async Task<IActionResult> ObtenerPacienteById(int id)
    {
        PacienteFindIdResponse? response = await _pacienteService.ObtenerById(id);
        if (response == null)
        {
            return NotFound("No hay");
        }
        return Ok(response);
    }

}

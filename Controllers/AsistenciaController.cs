using Proy_back_QBD.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;
using AutoMapper;
using System.Diagnostics;
using Proy_back_QBD.Models;
namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/asistencia")]
public class AsistenciaController : ControllerBase
{

    private readonly IAsistenciaService _asistenciaService;
    private readonly IMapper _mapper;

    public AsistenciaController(IAsistenciaService userService, IMapper mapper)
    {
        _asistenciaService = userService;
        _mapper = mapper;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(Asistencia))]
    public async Task<IActionResult> CrearAsistencia([FromBody] AsistenciaCreateReq request)
    {
        if (request == null)
        {
            return BadRequest("Datos de asistencia no proporcionados");
        }
        Asistencia? response = await _asistenciaService.Crear(request);
        if (response == null)
        {
            return BadRequest("Error al crear la asistencia");
        }
        return Ok(response);
    }
    
    /// <summary>
    /// Obtiene las asistencias agrupadas por día dentro de una fecha para un usuario específico.
    /// </summary>
    [HttpPost("{id}/{sedeId}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(AsistenciaByIdRes))]
    public async Task<IActionResult> ObtenerAsistenciaByCodigo(int id, [FromBody] AsistenciaByIdReq request, int sedeId)
    {
        if (request == null || id == null)
        {
            return BadRequest("Código de asistencia o id no proporcionado");
        }

        AsistenciaByIdRes? response = await _asistenciaService.ObtenerPorId(id, request.Año, request.Mes, sedeId);
        return Ok(response);
    }


}

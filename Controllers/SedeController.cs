using Proy_back_QBD.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;
using AutoMapper;
using Proy_back_QBD.Models;
namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/sede")]
public class SedeController : ControllerBase
{

    private readonly ISedeService _sedeService;
    private readonly IMapper _mapper;

    public SedeController(ISedeService userService, IMapper mapper)
    {
        _sedeService = userService;
        _mapper = mapper;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(SedeCreateRes))]
    public async Task<IActionResult> CrearSede([FromBody] SedeCreateReq request)
    {
        Sede sede = _mapper.Map<Sede>(request);
        Sede? response = await _sedeService.Crear(sede);
        return Ok(response);
    }
    [HttpPut("{sedeId}")]
    public async Task<IActionResult> ActualizarSede(int sedeId, [FromBody] SedeUpdateReq request)
    {
        Sede? response = await _sedeService.Actualizar(sedeId, request);
        return Ok(response);
    }

    [HttpGet]
    [SwaggerResponse(200, "Operación exitosa", typeof(SedeFindAllResponse))]
    public async Task<IActionResult> ObtenerSede()
    {
        List<SedeFindAllResponse>? response = await _sedeService.Obtener();
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }

    [HttpGet("msgs/{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(GeneralRes))]
    public async Task<IActionResult> DatosGenerales(int id)
    {
        GeneralRes? response = await _sedeService.ObtGeneral(id); ;
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }
    [HttpPatch("msgs/{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(GeneralRes))]
    public async Task<IActionResult> DatosGenerales(int id, GeneralReq request)
    {
        string? response = await _sedeService.ActualizarGeneral(id, request);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }

}

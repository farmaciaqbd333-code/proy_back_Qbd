using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/formulaR")]
public class FormulaRController : ControllerBase
{

    private readonly IFormulaRService _formulaRService;

    public FormulaRController(IFormulaRService formulaService)
    {
        _formulaRService = formulaService;
    }

    [HttpGet]
    [SwaggerResponse(200, "Creacion exitosa", typeof(FormulaRRes))]
    public async Task<IActionResult> ListaFormulaR()
    {

        List<FormulaRRes>? response = await _formulaRService.Listar();

        return Ok(response);
    }

    [HttpPost]
    [SwaggerResponse(200, "Creacion exitosa", typeof(string))]
    public async Task<IActionResult> CrearFormulaR(FormulaRCreReq request)
    {
        if (request == null)
        {
            return BadRequest();
        }


        string? response = await _formulaRService.Crear(request);

        return Ok(response);
    }

    [HttpPut("{id}")]
    [SwaggerResponse(200, "Actualizar exitosa", typeof(string))]
    public async Task<IActionResult> ActualizarFormulaR(int id, FormulaRUpdReq request)
    {
        if (request == null)
        {
            return BadRequest();
        }

        string? response = await _formulaRService.Actualizar(id, request);

        return Ok(response);
    }
    
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Actualizar exitosa", typeof(string))]
    public async Task<IActionResult> EliminarFormulaR(int id)
    {

        string? response = await _formulaRService.Eliminar(id);

        return Ok(response);
    }
    
}

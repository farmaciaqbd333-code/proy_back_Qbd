using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/formulaCC")]
public class FormulaCCController : ControllerBase
{
    private readonly IFormulaCCService _formulaService;

    public FormulaCCController(IFormulaCCService formulaService)
    {
        _formulaService = formulaService;
    }

    [HttpGet("{formulaId}/{sedeId}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(FormulaCCLabRes))]
    public async Task<IActionResult> ObtenerInsumosLab(int formulaId, int sedeId)
    {
        FormulaCCLabRes? response = await _formulaService.ListarInsumosLab(formulaId, sedeId);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }
    [HttpPut("{formulaId}/{sedeId}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(List<FormulaCC>))]
    public async Task<IActionResult> ActualizarFormulas(int formulaId, int sedeId, FormulaCCUpdReqP formulas)
    {
        if (formulas == null)
        {
            return BadRequest();
        }
        string? response = await _formulaService.Actualizar(formulaId, sedeId, formulas);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }


}
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/formula")]
public class FormulaController : ControllerBase
{

    private readonly IFormulaService _formulaService;

    public FormulaController(IFormulaService formulaService)
    {
        _formulaService = formulaService;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(FormulaCreateResponse))]
    public async Task<IActionResult> CrearFormula([FromBody] FormulaCreateReq request)
    {
        if (request == null)
        {
            return BadRequest("Request cannot be null");
        }
        FormulaCreateResponse? response = await _formulaService.CrearFormPed(request);
        return Ok(response);
    }
    
    [HttpPut("{id}/{sedeId}")]
    [SwaggerResponse(200, "Actualizacion exitosa", typeof(FormulaUpdateResponse))]
    public async Task<IActionResult> ActualizarFormula(int id, int sedeId, [FromBody] FormulaUpdateReq request)
    {
        Console.Write("w");
        FormulaUpdateResponse? response = await _formulaService.Actualizar(id, sedeId, request);

        return Ok(response);
    }

    [HttpGet("etiqueta/{formulaId}/{sedeId}")]
    [SwaggerResponse(200, "Actualizacion exitosa", typeof(EtiquetaRes))]
    public async Task<IActionResult> ObtenerEtiqueta(int formulaId, int sedeId)
    {
        EtiquetaRes? response = await _formulaService.ObtenerEtiqueta(formulaId, sedeId);
        if (response == null) return NotFound();
        return Ok(response);
    }
    
    [HttpGet("detalles/{formulaId}/{sedeId}")]
    [SwaggerResponse(200, "Actualizacion exitosa", typeof(DetallesRes))]
    public async Task<IActionResult> ObtenerDetalles(int formulaId, int sedeId)
    {
        DetallesRes? response = await _formulaService.ObtenerDetalles(formulaId, sedeId);
        if (response == null) return NotFound();
        return Ok(response);
    }

    [HttpPut("lab/{formulaId}/{sedeId}")]
    [SwaggerResponse(200, "Actualizacion exitosa", typeof(Formula))]
    public async Task<IActionResult> ActualizarFormulaLaboratorio(int formulaId, int sedeId, [FromBody] FormulaUpdLabReq request)
    {

        Formula? response = await _formulaService.ActualizarLab(formulaId, sedeId, request);
        if (response == null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPatch("formulaM/{id}/{sedeId}")]
    [SwaggerResponse(200, "Actualizacion exitosa", typeof(Formula))]
    public async Task<IActionResult> ActualizarFormulaMagistral(int id, int sedeId, string FormulaMagistral)
    {
        if (FormulaMagistral == null)
        {
            return BadRequest("Datos incorrectos");
        }
        Formula? response = await _formulaService.ActualizarFormulaM(id, sedeId, FormulaMagistral);

        return Ok(response);
    }
    [HttpPatch("inserto/{formulaId}/{sedeId}")]
    [SwaggerResponse(200, "Actualizacion exitosa", typeof(string))]
    public async Task<IActionResult> AgregarInserto(int formulaId, int sedeId, string inserto)
    {
        if (inserto == null)
        {
            return BadRequest("Datos incorrectos");
        }
        string? response = await _formulaService.AgregarInserto(formulaId, sedeId, inserto);

        return Ok(response);
    }

    [HttpDelete("{id}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(Formula))]
    public async Task<IActionResult> EliminarFormula(int id, int sedeId)
    {
        if (id == null)
        {
            return BadRequest("Datos incorrectos");
        }
        Formula? response = await _formulaService.Eliminar(id, sedeId);

        return Ok(response);
    }
    [HttpGet("receta/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(List<RecetaRes>))]
    public async Task<IActionResult> ListarReceta(int sedeId)
    {
        List<RecetaRes>? response = await _formulaService.ListarReceta(sedeId);

        return Ok(response);
    }
    [HttpGet("inserto/{formulaId}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(InsertoRes))]
    public async Task<IActionResult> ObtenerInserto(int formulaId, int sedeId)
    {
        InsertoRes? response = await _formulaService.ObtenerInserto(formulaId, sedeId);
        if (response == null)
        {
            return BadRequest();
        }
        return Ok(response);
    }
    [HttpGet("formulasLab/{pedidoId}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(FormulasLab))]
    public async Task<IActionResult> ListarFormulasLab(int pedidoId, int sedeId)
    {
        FormulasLab? response = await _formulaService.ListarFormulasLab(pedidoId, sedeId);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }
    [HttpPatch("tipo")]
    public async Task<IActionResult> CambiarTipo(FormulaCambiarTipo request)
    {
        string? respuesta = await _formulaService.CambiarTipo(request);
        if (respuesta == null)
        {
            return BadRequest();
        }
        return Ok("Se registró el cambio");
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/pedido")]
public class PedidoController : ControllerBase
{

    private readonly IPedidoService _pedidoService;

    public PedidoController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(string))]
    public async Task<IActionResult> CrearPedido([FromBody] PedidoCreateReq request)
    {

        if (request == null)
        {
            return BadRequest("Request cannot be null");
        }
        PedidoCreateRes? response = await _pedidoService.Crear(request);
        if (response == null)
        {
            return BadRequest("No se pudo crear por falta de datos o se repite el recibo");
        }
        return Ok(response);
    }
    [HttpPut("{id}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(PedidoUpdateReq))]
    public async Task<IActionResult> ActualizarPedido(int id, int sedeId, [FromBody] PedidoUpdateReq request)
    {
        if (request == null)
        {
            return BadRequest("Datos incorrectos");
        }
        PedidoUpdateResponse? response = await _pedidoService.Actualizar(id, sedeId, request);
        if (response == null)
        {
            return BadRequest("No se encontro o se repite el recibo");
        }

        return Ok(response);
    }
    [HttpPatch("comprobante/{id}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(Pedido))]
    public async Task<IActionResult> ActComprobante(int id, int sedeId, string? comprobante)
    {
        if (id == null)
        {
            return BadRequest("Datos incorrectos");
        }
        Pedido? response = await _pedidoService.ActComprobante(id, sedeId, comprobante);

        return Ok(response);
    }
    [HttpPatch("estado/{id}/{sedeId}")]
    public async Task<IActionResult> ActualizarEstado(int id, int sedeId, string estado)
    {
        if (id == null)
        {
            return BadRequest("Datos incorrectos");
        }
        string? response = await _pedidoService.ActEstado(id, sedeId, estado);

        return Ok(response);
    }
    [HttpGet]
    [SwaggerResponse(200, "Creacion exitosa", typeof(List<PedidoFindAllResponse?>))]
    public async Task<IActionResult> ObtenerPedidos(int sedeId)
    {
        List<PedidoFindAllResponse?> response = await _pedidoService.Listar(sedeId);

        return Ok(response);
    }

    [HttpGet("lab/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(List<PedidoLabFindAllRes?>))]
    public async Task<IActionResult> ObtenerPedidosLab(int sedeId)
    {
        List<PedidoLabFindAllRes2?> response = await _pedidoService.ListarLab(sedeId);

        return Ok(response);
    }

    [HttpGet("{id}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(PedidoFindIdResponse))]
    public async Task<IActionResult> ObtenerPedidosId(int id, int sedeId)
    {
        PedidoFindIdResponse? response = await _pedidoService.ObtenerById(id, sedeId);
        if (response == null)
        {
            return NotFound("No se encontrò");
        }
        return Ok(response);
    }
    [HttpGet("conteo/{mes}/{anio}/{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(int))]
    public async Task<IActionResult> ObtenerPedidosId(int sedeId, int mes, int anio)
    {
        int? response = await _pedidoService.ContFormM(sedeId, mes, anio);
        if (response == null)
        {
            return NotFound("No se encontrò");
        }
        return Ok(response);
    }

}

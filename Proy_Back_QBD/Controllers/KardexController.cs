using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    public class KardexController : Controller
    {
        private readonly IKardexService _kardexService;

        public KardexController(IKardexService _service)
        {
            this._kardexService = _service;
        }

        /// <summary>
        /// Listar Principal
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerStock(string familia, int idSede)
        {
            List<StockRes> response = await _kardexService.StockListaPrincipal(familia, idSede);
            return Ok(response);
        }

        [HttpGet("detalle-insumo/{insumoId}")]
        public async Task<IActionResult> ObtenerDetalleInsumo(int insumoId, [FromQuery] int idSede)
        {
            var resultado = await _kardexService.ObtenerDetalleInsumo(insumoId, idSede);

            return Ok(resultado);
        }
        [HttpGet("detalle-producto-intermedio/{insumoId}")]
        public async Task<IActionResult> ObtenerDetallePI(int insumoId, [FromQuery] int idSede)
        {
            var resultado = await _kardexService.ObtenerDetallePI(insumoId, idSede);

            return Ok(resultado);
        }

        [HttpGet("detalle-empaque/{empaqueId}")]
        public async Task<IActionResult> ObtenerDetalleEmpaque(int empaqueId, [FromQuery] int idSede)
        {
            var resultado = await _kardexService.ObtenerDetalleEmpaque(empaqueId, idSede);

            return Ok(resultado);
        }
        [HttpGet("detalle-producto/{productoId}")]
        public async Task<IActionResult> ObtenerDetallePT(int productoId, [FromQuery] int idSede)
        {
            var resultado = await _kardexService.ObtenerDetallePT(productoId, idSede);

            return Ok(resultado);
        }
        [HttpGet("vencidos")]
        public async Task<IActionResult> ObtenerVencidos(string familia, int idSede)
        {
            var resultado = await _kardexService.ObtenerComprasVencidas(familia, idSede);

            return Ok(resultado);
        }

        [HttpGet("salidas-insumo/{insumoId}")]
        public async Task<IActionResult> ObtenerSalidasInsumo(int insumoId, [FromQuery] int idSede)
        {
            var resultado = await _kardexService.ObtenerSalidasInsumo(insumoId, idSede);
            return Ok(resultado);
        }

        [HttpGet("bajas-insumo/{insumoId}")]
        public async Task<IActionResult> ObtenerBajasInsumo(int insumoId, [FromQuery] int idSede)
        {
            var resultado = await _kardexService.ObtenerBajasInsumo(insumoId, idSede);
            return Ok(resultado);
        }

        [HttpPut("asignar-ubicacion")]
        public async Task<IActionResult> AssignLocation([FromBody] AssignLocationReq? body, [FromQuery] int? idSede, [FromQuery] int? idInsumo, [FromQuery] string? ubicacion, [FromQuery] string? familia)
        {
            var req = body ?? new AssignLocationReq();
            if (req.IdSede == 0 && idSede.HasValue) req.IdSede = idSede.Value;
            if (req.IdInsumo == 0 && idInsumo.HasValue) req.IdInsumo = idInsumo.Value;
            if (string.IsNullOrEmpty(req.Ubicacion) && !string.IsNullOrEmpty(ubicacion)) req.Ubicacion = ubicacion;
            if (string.IsNullOrEmpty(req.Familia) && !string.IsNullOrEmpty(familia)) req.Familia = familia;

            var resultado = await _kardexService.AssignLocation(req);
            return Ok(resultado);
        }

        [HttpPut("asignar-limite")]
        public async Task<IActionResult> AssignLimite([FromBody] AssignLimiteReq? body, [FromQuery] int? idSede, [FromQuery] int? idInsumo, [FromQuery] decimal? limite, [FromQuery] string? familia)
        {
            var req = body ?? new AssignLimiteReq();
            if (req.IdSede == 0 && idSede.HasValue) req.IdSede = idSede.Value;
            if (req.IdInsumo == 0 && idInsumo.HasValue) req.IdInsumo = idInsumo.Value;
            if (!req.Limite.HasValue && limite.HasValue) req.Limite = limite.Value;
            if (string.IsNullOrEmpty(req.Familia) && !string.IsNullOrEmpty(familia)) req.Familia = familia;

            var resultado = await _kardexService.AssignLimite(req);
            return Ok(resultado);
        }

    }
}
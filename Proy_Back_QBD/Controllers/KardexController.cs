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
        private readonly IKardexStockService _stock;
        private readonly IKardexDetalleService _detalle;
        private readonly IKardexMovimientoService _movimientos;
        private readonly IKardexUbicacionService _ubicacion;
        private readonly IKardexLotesService _lotes;

        public KardexController(
            IKardexStockService stock,
            IKardexDetalleService detalle,
            IKardexMovimientoService movimientos,
            IKardexUbicacionService ubicacion,
            IKardexLotesService lotes)
        {
            _stock = stock;
            _detalle = detalle;
            _movimientos = movimientos;
            _ubicacion = ubicacion;
            _lotes = lotes;
        }

        /// <summary>
        /// Listar Principal
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerStock(string familia, int idSede)
        {
            List<StockRes> response = await _stock.StockListaPrincipal(familia, idSede);
            return Ok(response);
        }

        [HttpGet("detalle-insumo/{insumoId}")]
        public async Task<IActionResult> ObtenerDetalleInsumo(int insumoId, [FromQuery] int idSede)
        {
            var resultado = await _detalle.ObtenerDetalleInsumo(insumoId, idSede);

            return Ok(resultado);
        }
        [HttpGet("detalle-producto-intermedio/{insumoId}")]
        public async Task<IActionResult> ObtenerDetallePI(int insumoId, [FromQuery] int idSede)
        {
            var resultado = await _detalle.ObtenerDetallePI(insumoId, idSede);

            return Ok(resultado);
        }

        [HttpGet("detalle-empaque/{empaqueId}")]
        public async Task<IActionResult> ObtenerDetalleEmpaque(int empaqueId, [FromQuery] int idSede)
        {
            var resultado = await _detalle.ObtenerDetalleEmpaque(empaqueId, idSede);

            return Ok(resultado);
        }
        [HttpGet("detalle-producto/{productoId}")]
        public async Task<IActionResult> ObtenerDetallePT(int productoId, [FromQuery] int idSede)
        {
            var resultado = await _detalle.ObtenerDetallePT(productoId, idSede);

            return Ok(resultado);
        }
        [HttpGet("vencidos")]
        public async Task<IActionResult> ObtenerVencidos(string familia, int idSede)
        {
            var resultado = await _stock.ObtenerComprasVencidas(familia, idSede);

            return Ok(resultado);
        }

        [HttpGet("salidas-insumo/{insumoId}")]

        public async Task<IActionResult> ObtenerSalidasInsumo(int insumoId, [FromQuery] int idSede, [FromQuery] string? familia = null)
        {
            var resultado = await _movimientos.ObtenerSalidasInsumo(insumoId, idSede, familia);
            return Ok(resultado);
        }

        [HttpGet("salidas-empaque/{empaqueId}")]
        public async Task<IActionResult> ObtenerSalidasEmpaque(int empaqueId, [FromQuery] int idSede)
        {
            var resultado = await _movimientos.ObtenerSalidasEmpaque(empaqueId, idSede);
            return Ok(resultado);
        }

        [HttpGet("salidas-producto/{productoId}")]
        public async Task<IActionResult> ObtenerSalidasPT(int productoId, [FromQuery] int idSede)
        {
            var resultado = await _movimientos.ObtenerSalidasPT(productoId, idSede);
            return Ok(resultado);
        }

        [HttpGet("salidas-economato/{economatoId}")]
        public async Task<IActionResult> ObtenerSalidasEconomato(int economatoId, [FromQuery] int idSede)
        {
            var resultado = await _movimientos.ObtenerSalidasEconomato(economatoId, idSede);
            return Ok(resultado);
        }

        [HttpGet("bajas-insumo/{insumoId}")]
        public async Task<IActionResult> ObtenerBajasInsumo(int insumoId, [FromQuery] int idSede, [FromQuery] string? familia = null)
        {
            var resultado = await _movimientos.ObtenerBajasInsumo(insumoId, idSede, familia);
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

            var resultado = await _ubicacion.AssignLocation(req);
            return Ok(resultado);
        }

        [HttpPut("asignar-limite")]
        public async Task<IActionResult> AssignLimite([FromBody] AssignLimiteReq? body, [FromQuery] int? idSede, [FromQuery] int? idInsumo, [FromQuery] decimal? limite, [FromQuery] decimal? cantidadEnviar, [FromQuery] string? familia)
        {
            var req = body ?? new AssignLimiteReq();
            if (req.IdSede == 0 && idSede.HasValue) req.IdSede = idSede.Value;
            if (req.IdInsumo == 0 && idInsumo.HasValue) req.IdInsumo = idInsumo.Value;
            if (!req.Limite.HasValue && limite.HasValue) req.Limite = limite.Value;
            if (!req.CantidadEnviar.HasValue && cantidadEnviar.HasValue) req.CantidadEnviar = cantidadEnviar.Value;
            if (string.IsNullOrEmpty(req.Familia) && !string.IsNullOrEmpty(familia)) req.Familia = familia;

            var resultado = await _ubicacion.AssignLimite(req);
            return Ok(resultado);
        }


        [HttpPost("resolver-lotes-formula")]
        public async Task<IActionResult> ResolverLotesFormula([FromBody] ResolverLotesFormulaReq request)
        {
            var resultado = await _lotes.ResolverLotesFormula(request);
            return Ok(resultado);
        }
    }
}
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
        public async Task<IActionResult> ObtenerStock()
        {
            ListarStockRes response = await _kardexService.StockListaPrincipal();

            return Ok(response);
        }
        [HttpGet("detalle-insumo/{insumoId:int}")]
        public async Task<IActionResult> ObtenerDetalleInsumo(int insumoId)
        {
            var resultado = await _kardexService.ObtenerDetalleInsumo(insumoId);

            return Ok(resultado);
        }

        [HttpGet("detalle-empaque/{empaqueId:int}")]
        public async Task<IActionResult> ObtenerDetalleEmpaque(int empaqueId)
        {
            var resultado = await _kardexService.ObtenerDetalleEmpaque(empaqueId);

            return Ok(resultado);
        }
        [HttpPost("registrar-ajuste")]
        public async Task<IActionResult> RegistrarAjuste([FromBody] CrearAjusteReq request)
        {
            await _kardexService.RegistrarAjuste(request);
            return Ok(new
            {
                mensaje = "Ajuste registrado correctamente."
            });
        }
    }
}
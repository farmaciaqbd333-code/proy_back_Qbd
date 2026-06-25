using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    public class PaqueteController : Controller
    {
        private readonly IPaqueteService _servicePaquete;

        public PaqueteController(IPaqueteService _servicePaquete)
        {
            this._servicePaquete = _servicePaquete;
        }
        /// <summary>
        /// Listar lista de paquetes
        /// </summary>
        [HttpGet("detalle/{IdCompra}")]
        public async Task<IActionResult> ObtenerDetallePaquete(int IdCompra)
        {
            PaqueteInsumoDetalleRes response = await _servicePaquete.GetDetallePaquetes(IdCompra);
            return Ok(response);
        }
        /// <summary>
        /// Crear Paquete Insumo a Detalle Compra
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CrearPaqueteInsumo([FromBody] PaqueteInsumoCrearReq req)
        {
            int id = await _servicePaquete.CrearPaqueteInsumo(req);
            return Created("", new { id });
        }
        /// <summary>
        /// Crear Paquete Empaque a Detalle Compra
        /// </summary>
        [HttpPost("empaque")]
        public async Task<IActionResult> CrearPaqueteEmpaque([FromBody] PaqueteEmpaqueCrearReq req)
        {
            int id = await _servicePaquete.CrearPaqueteEmpaque(req);
            return Created("", new { id });
        }
        /// <summary>
        /// Modificar Paquete Insumo a Detalle Compra
        /// </summary>
        [HttpPut("insumo/{idPaquete}")]
        public async Task<IActionResult> ModificarPaqueteInsumo(int idPaquete, [FromBody] PaqueteInsumoModificarReq req)
        {
            string response = await _servicePaquete.ModificarPaqueteInsumo(idPaquete, req);
            return Ok(response);
        }
        /// <summary>
        /// Modificar Paquete Empaque a Detalle Compra
        /// </summary>
        [HttpPut("empaque/{idPaquete}")]
        public async Task<IActionResult> ModificarPaqueteEmpaque(int idPaquete, [FromBody] PaqueteEmpaqueModificarReq req)
        {
            string response = await _servicePaquete.ModificarPaqueteEmpaque(idPaquete, req);
            return Ok(response);
        }
        /// <summary>
        /// Eliminar Paquete
        /// </summary>
        [HttpDelete("{idPaquete}/{empaqueInsumo}")]
        public async Task<IActionResult> EliminarPaquete(int idPaquete, int empaqueInsumo)
        {
            string response = await _servicePaquete.EliminarPaquete(idPaquete, empaqueInsumo);
            return Ok(response);
        }
    }
}
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
        [HttpGet("detalle/{detalleCompraId}")]
        public async Task<IActionResult> ObtenerDetallePaquete(int detalleCompraId)
        {
            DetallePaqueteRes? response = await _servicePaquete.GetDetallePaquete(detalleCompraId);
            if (response == null) return NotFound("No se encontro paquete");
            return Ok(response);
        }
        /// <summary>
        /// Crear Paquete a Detalle Compra
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CrearPaquete([FromBody] CrearPaqueteReq req)
        {
            int id = await _servicePaquete.CrearPaquete(req);
            return Created("", new { id });
        }
        /// <summary>
        /// Modificar Paquete a Detalle Compra
        /// </summary>
        [HttpPut("{idPaquete}")]
        public async Task<IActionResult> ModificarPaquete(int idPaquete, [FromBody] ModificarPaqueteReq req)
        {
            bool response = await _servicePaquete.ModificarPaquete(idPaquete, req);
            if (!response) return NotFound("No se modifico el paquete");
            return Ok(response);
        }
        /// <summary>
        /// Eliminar Paquete a Detalle Compra
        /// </summary>
        [HttpDelete("{idPaquete}")]
        public async Task<IActionResult> EliminarPaquete(int idPaquete)
        {
            bool response = await _servicePaquete.EliminarPaquete(idPaquete);
            if (!response) return NotFound("No se elimino el paquete");
            return Ok(response);
        }
    }
}
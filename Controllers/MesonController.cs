using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace proy_back_Qbd.Controllers
{
    [Route("api/meson")]
    public class MesonController : Controller
    {
        private readonly IMesonService _serviceMeson;
        public MesonController(IMesonService serviceMeson)
        {
            _serviceMeson = serviceMeson;
        }
        /// <summary>
        /// Listar para tabla meson
        /// </summary>
        [HttpGet("meson")]
        public async Task<ActionResult<List<MesonListaRes>>> ListarOrdenesEnviadasRes()
        {
            List<MesonListaRes> response = await _serviceMeson.ListarMeson(["ENVIADO", "MESON", "MESÓN", "LABORATORIO"]);
            if (response.Count == 0)
                return NotFound(new { message = "No se encontro ordenes" });

            return Ok(response);
        }

        /// <summary>
        /// Obtener detalle de meson
        /// </summary>
        [HttpGet("detalle/{id}")]
        public async Task<ActionResult<MesonDetalleRes>> ObtenerOrdenCompra(int id)
        {

            MesonDetalleRes response = await _serviceMeson.ObtenerDetalleOrdenOCompra(id);

            return Ok(response);
        }
        /// <summary>
        /// Completar Datos
        /// </summary>
        [HttpPatch("meson/{ordenCompraId}")]
        [SwaggerResponse(200, "Creación exitosa", typeof(MesonListaRes))]
        public async Task<ActionResult<MesonListaRes>> ConvertirCompra(int ordenCompraId, MesonConvertirReq request)
        {
            MesonListaRes? response = await _serviceMeson.CompletarDatos(ordenCompraId, request);
            if (response == null) return NotFound(new { message = "Compra no encontrada" });

            return Ok(response);
        }

        /// <summary>
        /// Obtener datos para meson modal
        /// </summary>
        [HttpGet("meson/{ordenId}")]
        public async Task<ActionResult<MesonModalRes>> ObtenerDatosMeson(int ordenId)
        {
            MesonModalRes? response = await _serviceMeson.ObtenerDatosModal(ordenId);

            if (response == null)
                return NotFound(new { message = "No se encontro alguna descripcion" });

            return Ok(response);
        }

    }
}
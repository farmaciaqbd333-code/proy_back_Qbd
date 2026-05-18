using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("[controller]")]
    public class CompraController : Controller
    {
        public readonly ICompraService _service;

        public CompraController(ICompraService service)
        {
            _service = service;
        }

        /// <summary>
        /// Listar para tabla meson
        /// </summary>
        [HttpGet("meson")]
        public async Task<ActionResult<List<OrdenesEnviadasRes>>> ListarOrdenesEnviadasRes()
        {
            List<OrdenesEnviadasRes> response = await _service.ListaOrdenesEnviadas(["ENVIADO", "LABORATORIO"]);
            if (response.Count == 0)
                return NotFound(new { message = "No se encontro ordenes" });

            return Ok(response);
        }

        /// <summary>
        /// Obtener detalle de orden de compra
        /// </summary>
        [HttpGet("detalle/{id}")]
        public async Task<ActionResult<ObtenerOrdenOCompraRes2>> ObtenerOrdenCompra(int id)
        {

            ObtenerOrdenOCompraRes2? response = await _service.ObtenerDetalleOrdenOCompra(id);

            return Ok(response);
        }
    }
}
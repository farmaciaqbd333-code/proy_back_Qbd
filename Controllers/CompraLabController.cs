using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    public class CompraLabController : Controller
    {
        private readonly ICompraService _service;
        private readonly ICompraLaboratorioService _serviceCompraLab;

        public CompraLabController(ICompraService service, ICompraLaboratorioService serviceCompraLab)
        {
            _service = service;
            _serviceCompraLab = serviceCompraLab;
        }
        /// <summary>
        /// Listar para tabla laboratorio
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<OrdenesEnviadasRes>>> ListarOrdenesLaboratorio()
        {
            List<OrdenesEnviadasRes> response = await _service.ListaOrdenesEnviadas(["LABORATORIO"]);
            if (response.Count == 0)
                return NotFound(new { message = "No se encontro ordenes" });

            return Ok(response);
        }
        /// <summary>
        /// Obtener datos para actualizar datos de laboratorio
        /// </summary>
        [HttpGet("{idCompra}")]
        public async Task<ActionResult<ObtenerCompraLabRes>> DatosCompraLaboratorio(int idCompra)
        {
            ObtenerCompraLabRes? response = await _serviceCompraLab.DatosCompraLaboratorio(idCompra);
            if (response == null)
                return NotFound(new { message = "No se encontro detalles" });

            return Ok(response);
        }
        
        /// <summary>
        /// Obtener detalle de compra laboratorio
        /// </summary>
        [HttpGet("detalle/{idCompra}")]
        public async Task<ActionResult<ObtenerCompraLab2Res>> DetalleCompraLaboratorio(int idCompra)
        {
            ObtenerCompraLab2Res response = await _serviceCompraLab.DetalleCompraLaboratorio(idCompra);

            return Ok(response);
        }

        /// <summary>
        /// Actualizar detalle de laboratorio
        /// </summary>
        [HttpPatch("{idCompra}")]
        public async Task<ActionResult<ObtenerCompraLabRes>> ActualizarDetalleLab(int idCompra, List<ActualizarDetCompraLabReq> request)
        {
            int? response = await _serviceCompraLab.ActualizarDetalleLab(idCompra, request);
            if (response == null)
                return NotFound(new { message = "No se encontro Compra" });

            return Ok(response);
        }
    }
}

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
    public class CompraLaboratorioController : Controller
    {
        private readonly ICompraService _service;
        private readonly ICompraLaboratorioService _serviceCompraLab;

        public CompraLaboratorioController(ICompraService service, ICompraLaboratorioService serviceCompraLab)
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
        /// Obtener detalle para Actualizar datos de laboratorio
        /// </summary>
        [HttpGet("{idCompra}")]
        public async Task<ActionResult<ObtenerCompraLabRes>> ObtenerDetalleCompraLaboratorio(int idCompra)
        {
            ObtenerCompraLabRes? response = await _serviceCompraLab.ObtenerCompraLaboratorio(idCompra);
            if (response == null)
                return NotFound(new { message = "No se encontro detalles" });

            return Ok(response);
        }
    }
}

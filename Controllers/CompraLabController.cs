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
        private readonly ICompraLaboratorioService _serviceCompraLab;

        public CompraLabController(ICompraLaboratorioService serviceCompraLab)
        {
            _serviceCompraLab = serviceCompraLab;
        }

        /// <summary>
        /// Listar para tabla laboratorio
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<MesonListaRes>>> ListarOrdenesLaboratorio()
        {
            List<LabListaRes> response = await _serviceCompraLab.Listar(["LABORATORIO"]);

            return Ok(response);
        }
        // /// <summary>
        // /// Obtener datos para actualizar datos de laboratorio
        // /// </summary>
        // [HttpGet("{idCompra}")]
        // public async Task<ActionResult<ObtenerCompraLabRes>> DatosCompraLaboratorio(int idCompra)
        // {
        //     ObtenerCompraLabRes? response = await _serviceCompraLab.GetCompraLab(idCompra);

        //     return Ok(response);
        // }

        /// <summary>
        /// Obtener detalle de compra laboratorio
        /// </summary>
        [HttpGet("detalle/{idCompra}")]
        public async Task<ActionResult<CompraLabIdRes>> DetalleCompraLaboratorio(int idCompra)
        {
            CompraLabIdRes response = await _serviceCompraLab.GetDetalleCompraLab(idCompra);

            return Ok(response);
        }

        /// <summary>
        /// Actualizar detalle de laboratorio
        /// </summary>
        [HttpPatch("{idCompra}")]
        public async Task<ActionResult> ActualizarDetalleLab(int idCompra, List<ActualizarDetCompraLabReq> request)
        {
            int response = await _serviceCompraLab.UpdateDetalleLab(idCompra, request);
            return Ok(response);
        }
        /// <summary>
        /// Obtener datos para etiqueta insumo
        /// </summary>
        [HttpGet("etiqueta/insumo/{idCompra}")]
        public async Task<ActionResult<EtiquetaCompra>> EtiquetaCompraInsumo(int idCompra)
        {
            EtiquetaCompra response = await _serviceCompraLab.GetEtiquetaCompraInsumo(idCompra);

            return Ok(response);
        }
        /// <summary>
        /// Obtener datos para etiqueta Empaque
        /// </summary>
        [HttpGet("etiqueta/empaque/{idCompra}")]
        public async Task<ActionResult<EtiquetaCompra>> EtiquetaCompraEmpaque(int idCompra)
        {
            EtiquetaCompra response = await _serviceCompraLab.GetEtiquetaCompraEmpaque(idCompra);

            return Ok(response);
        }
    }
}

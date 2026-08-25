using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.DetalleCompraLab;
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
        public async Task<ActionResult<List<LabListaRes>>> ListarOrdenesLaboratorio(int idSede)
        {
            try
            {
                List<LabListaRes> response = await _serviceCompraLab.Listar(["LABORATORIO"], idSede);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Obtener datos para actualizar datos de laboratorio
        /// </summary>
        [HttpGet("modal/{idCompra}")]
        public async Task<ActionResult<ObtenerCompraLabRes>> CompraLaboratorioModal(int idCompra)
        {
            try
            {
                ObtenerCompraLabRes response = await _serviceCompraLab.ModalPaquetes(idCompra);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Obtener detalle de compra laboratorio
        /// </summary>
        [HttpGet("detalle/{idCompra}")]
        public async Task<ActionResult<CompraLabDetIdRes>> DetalleCompraLaboratorio(int idCompra)
        {
            try
            {
                CompraLabDetIdRes response = await _serviceCompraLab.GetDetalleCompraLab(idCompra);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Actualizar detalle de laboratorio
        /// </summary>
        [HttpPatch("{idCompra}")]
        public async Task<IActionResult> UpdateDetalleLab(int idCompra, [FromBody] ActualizarDetCompraLabReq request)
        {
            try
            {
                await _serviceCompraLab.UpdateDetalleLab(idCompra, request);
                return Ok("Actualización correcta");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Obtener datos para etiqueta insumo
        /// </summary>
        [HttpGet("etiqueta/insumo/{idCompra}")]
        public async Task<ActionResult<EtiquetaCompra>> EtiquetaCompraInsumo(int idCompra)
        {
            try
            {
                EtiquetaCompra response = await _serviceCompraLab.GetEtiquetaCompraInsumo(idCompra);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Obtener datos para etiqueta Empaque
        /// </summary>
        [HttpGet("etiqueta/empaque/{idCompra}")]
        public async Task<ActionResult<EtiquetaCompra>> EtiquetaCompraEmpaque(int idCompra)
        {
            try
            {
                EtiquetaCompra response = await _serviceCompraLab.GetEtiquetaCompraEmpaque(idCompra);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("[controller]")]
    public class DetalleCompraController : Controller
    {
        private readonly ApiContext _context;
        public DetalleCompraController(ApiContext context)
        {
            _context = context;
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> ActualizarDetalleCompra(int id, PatchDetalleCompraReq request)
        {
            DetalleCompra? detalleCompra = await _context.DetalleCompras.FindAsync(id);
            if (detalleCompra == null)
                return NotFound(new { message = "Detalle de compra no encontrado" });

            // Actualización parcial (solo si vienen valores)
            if (request.Coa != null)
                detalleCompra.Coa = request.Coa;

            if (request.Lote != null)
                detalleCompra.Lote = request.Lote;

            if (request.Cantidad.HasValue)
                detalleCompra.CantidadPaquete = request.Cantidad.Value;

            if (request.PotenciaPorcentaje != null)
                detalleCompra.Potencia = request.PotenciaPorcentaje;

            if (request.FechaFabricacion.HasValue)
                detalleCompra.FechaFab = request.FechaFabricacion.Value;

            if (request.FechaVencimiento.HasValue)
                detalleCompra.FechaVec = request.FechaVencimiento.Value;

            if (request.CondicionAlmacenamiento != null)
                detalleCompra.CondicionAlmacenamiento = request.CondicionAlmacenamiento;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Detalle de compra actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
            }

            return Ok();
        }
    }
}
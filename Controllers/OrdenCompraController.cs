using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenCompraController : ControllerBase
    {
        private readonly ApiContext _context;
        public OrdenCompraController(ApiContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<ListadoOrdenCompra>>> GetOrdenesCompra()
        {

            var data = await _context.OrdenCompras
                .Include(i => i.Proveedor)
                .Include(i => i.Compra)
                .Include(i => i.DetalleOrdenCompras)
                .Include(i => i.Sede)
                .OrderByDescending(o => o.IdOrdenCompra)
                .ToListAsync();

            var ordenes = data.Select(s => new ListadoOrdenCompra
            {
                CUO = "OC-" + s.IdOrdenCompra,
                Fecha = s.FechaCreacion.ToString("dd/MM/yyyy"),

                Serie = s.Compra != null && !string.IsNullOrEmpty(s.Compra.CodFactura) && s.Compra.CodFactura.Contains('-') ? s.Compra.CodFactura.Split('-')[0] : "",
                Numero = s.Compra != null && !string.IsNullOrEmpty(s.Compra.CodFactura) ? (s.Compra.CodFactura.Contains('-') ? s.Compra.CodFactura.Split('-')[1] : s.Compra.CodFactura) : "",
                RUC = s.Proveedor?.CodigoProv ?? "",
                Denominacion = s.Proveedor?.Datos ?? "",
                Valor = s.DetalleOrdenCompras != null ? (s.DetalleOrdenCompras.Sum(sm => sm.Cantidad) * s.DetalleOrdenCompras.Sum(sm => sm.CostoUnitario)).ToString("F2") : "0.00",
                Total = s.DetalleOrdenCompras != null ? s.DetalleOrdenCompras.Sum(sm => sm.CostoTotal).ToString("F2") : "0.00",
                Moneda = s.Moneda ?? "PEN",
                Estado = s.Estado ?? "PEN",
                CodFac = s.Compra?.CodFactura ?? "",
                Familia = s.Familia == "Insumos" ? "MP" : s.Familia, // Mapeo de "Insumos" a "MP"
                Factura = s.Compra != null ? "SI" : "NO",
                EstadoOrdenCompra = s.Estado ?? "PEN",
                Usuario = s.Sede != null ? s.Sede.Nombre : "N/A"
            }).ToList();

            return Ok(ordenes);
        }
        [HttpGet("detalle/{id}")]
        public async Task<ActionResult<DetalleOrdenCompraRes>> GetDetalleOrdenesCompra(int id)
        {

            DetalleOrdenCompraRes? orden = await _context.OrdenCompras
                .Where(w => w.IdOrdenCompra == id)
                .Select(s => new DetalleOrdenCompraRes
                {
                    Modalidad = s.Modalidad,
                    TC = s.TipoCambio.ToString(),
                    Familia = s.Familia.ToString(),
                    FechaCotizacion = s.FechaCotizacion,
                    Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                    Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                    CodigoProveedor = s.Proveedor == null || s.Proveedor.CodigoProvedor == null ? "" : s.Proveedor.CodigoProvedor,
                    DetalleOrdenCompras = s.DetalleOrdenCompras == null
                                            ? null :
                                            s.DetalleOrdenCompras.Select(s2 => new DetalleOrdenCompra2
                                            {
                                                IdInsumo = s2.IdInsumo,
                                                Codigo = s2.IdInsumo.ToString(),
                                                DescripcionQBD = s2.Insumo == null || s2.Insumo.Descripcion == null ? "" : s2.Insumo.Descripcion,
                                                DescripcionFactura = s2.DescripcionFac,
                                                Cantidad = s2.Cantidad.ToString(),
                                                UM = s2.Um,
                                                CUnitario = s2.CostoUnitario.ToString(),
                                                CTotal = s2.CostoTotal.ToString()
                                            })
                                            .ToList()

                }).FirstOrDefaultAsync();
            if (orden == null)
            {
                return NotFound("No encontrado");

            }

            return Ok(orden);
        }

        [HttpPost]
        public async Task<ActionResult> CrearDetalleOrdenesCompra(OrdenCompraCreateReq request)
        {
            OrdenCompra ordenCompra = new OrdenCompra
            {
                IdProveedor = request.IdProveedor,
                Modalidad = request.Modalidad,
                Moneda = request.Moneda,
                TipoCambio = request.TipoCambio,
                Impuesto = request.Impuesto,
                Observaciones = request.Observaciones,
                Familia = request.Familia,
                IdSede = request.IdSede,
                Estado = "PEN",
                IdCreador = request.IdCreador,
                FechaCotizacion = request.FechaEmision,
                TipoTributario = request.TipoTributario,
            };
            _context.OrdenCompras.Add(ordenCompra);
            await _context.SaveChangesAsync();

            List<DetalleOrdenCompra> detalleOrdenCompras = request.Detalle.Select(s => new DetalleOrdenCompra
            {
                IdInsumo = s.IdInsumo,
                DescripcionFac = s.DescripcionFac,
                Cantidad = s.Cantidad,
                Um = s.UM,
                CostoUnitario = s.CUnitario,
                CostoTotal = s.CTotal,
                IdOrdenCompra = ordenCompra.IdOrdenCompra,
                FechaModificacion = ordenCompra.FechaCreacion,
                IdCreador = request.IdCreador,
                IdModificador = request.IdCreador // Asignar el mismo creador como modificador inicial
            }).ToList();

            _context.DetalleOrdenesCompras.AddRange(detalleOrdenCompras);
            await _context.SaveChangesAsync();

            ordenCompra.DetalleOrdenCompras = detalleOrdenCompras;
            return Ok(ordenCompra);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrdenCompra(int id, [FromBody] OrdenCompraUpdateReq req)
        {
            OrdenCompra? orden = await _context.OrdenCompras.FindAsync(id);

            if (orden == null)
            {
                return NotFound(new { message = "Orden de compra no encontrada" });
            }

            // Mapear campos
            orden.IdProveedor = req.IdProveedor;
            orden.Modalidad = req.Modalidad;
            orden.Moneda = req.Moneda;
            orden.TipoCambio = req.TipoCambio;
            orden.Impuesto = req.Impuesto;
            orden.FechaCotizacion = req.FechaEmision;
            orden.Observaciones = req.Observaciones;
            orden.Familia = req.Familia;
            orden.IdSede = req.IdSede;
            orden.TipoTributario = req.TipoTributario;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Orden de compra actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
            }
        }
        [HttpPatch("detalles/{id}")]
        public async Task<IActionResult> PatchDetallesOrdenCompra(int id, [FromBody] List<DetalleOrdenCompraPatchReq> detallesPatch)
        {
            var orden = await _context.OrdenCompras
        .Include(o => o.DetalleOrdenCompras)
        .FirstOrDefaultAsync(o => o.IdOrdenCompra == id);
            if (orden == null)
                return NotFound(new { message = "Orden de compra no encontrada" });

            if (orden.DetalleOrdenCompras == null || !orden.DetalleOrdenCompras.Any())
                return NotFound(new { message = "No hay detalles para esta orden" });

            foreach (var patch in detallesPatch)
            {
                var detalle = orden.DetalleOrdenCompras
                    .FirstOrDefault(d => d.IdInsumo == patch.IdInsumo);

                if (detalle == null)
                    continue; // o puedes lanzar error si quieres obligar que exista

                // PATCH: solo actualiza si viene valor
                if (patch.DescripcionFac != null)
                    detalle.DescripcionFac = patch.DescripcionFac;

                if (patch.Cantidad.HasValue)
                    detalle.Cantidad = patch.Cantidad.Value;

                if (patch.Um != null)
                    detalle.Um = patch.Um;

                if (patch.CostoUnitario.HasValue)
                    detalle.CostoUnitario = patch.CostoUnitario.Value;

                if (patch.CostoTotal.HasValue)
                    detalle.CostoTotal = patch.CostoTotal.Value;

                detalle.IdModificador = patch.IdModificador;
                detalle.FechaModificacion = DateTime.UtcNow;
            }
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Detalles actualizados correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar detalles", error = ex.Message });
            }
        }
        [HttpDelete("detalles/{idOrden}/{idInsumo}")]
        public async Task<IActionResult> DeleteDetalleOrdenCompra(int idOrden, int idInsumo)
        {
            OrdenCompra? orden = await _context.OrdenCompras
                .Include(o => o.DetalleOrdenCompras)
                .FirstOrDefaultAsync(o => o.IdOrdenCompra == idOrden);

            if (orden == null)
                return NotFound(new { message = "Orden de compra no encontrada" });
            if (orden.DetalleOrdenCompras == null)
                return NotFound(new { message = "Orden de compra sin detalles" });

            DetalleOrdenCompra? detalle = orden.DetalleOrdenCompras
                .FirstOrDefault(d => d.IdInsumo == idInsumo);

            if (detalle == null)
                return NotFound(new { message = "Detalle no encontrado" });

            _context.DetalleOrdenesCompras.Remove(detalle);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Detalle eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar detalle", error = ex.Message });
            }
        }
        [HttpPost("detalles/{id}")]
        public async Task<IActionResult> CreateDetallesOrdenCompra(int id, [FromBody] List<DetalleOrdenCompraCreateReq> detalles)
        {
            var orden = await _context.OrdenCompras
                .Include(o => o.DetalleOrdenCompras)
                .FirstOrDefaultAsync(o => o.IdOrdenCompra == id);

            if (orden == null)
                return NotFound(new { message = "Orden de compra no encontrada" });

            var nuevosDetalles = new List<DetalleOrdenCompra>();

            foreach (var item in detalles)
            {
                // Validación básica
                if (item.Cantidad <= 0 || item.CostoUnitario < 0)
                    return BadRequest(new { message = "Cantidad o costo inválido" });

                // Validar duplicados (opcional)
                if (orden.DetalleOrdenCompras != null)
                {
                    var existe = orden.DetalleOrdenCompras
                    .Any(d => d.IdInsumo == item.IdInsumo);

                    if (existe)
                        return Ok(new { message = "Ya existe este Insumo" });
                }

                var detalle = new DetalleOrdenCompra
                {
                    IdOrdenCompra = id,
                    IdInsumo = item.IdInsumo,
                    DescripcionFac = item.DescripcionFac,
                    Cantidad = item.Cantidad,
                    Um = item.Um,
                    CostoUnitario = item.CostoUnitario,
                    CostoTotal = item.CostoTotal,
                    IdCreador = item.IdCreador,
                    IdModificador = item.IdCreador,
                    FechaModificacion = DateTime.UtcNow
                };

                nuevosDetalles.Add(detalle);
            }

            if (!nuevosDetalles.Any())
                return BadRequest(new { message = "No hay detalles válidos para insertar" });

            await _context.DetalleOrdenesCompras.AddRangeAsync(nuevosDetalles);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Detalles creados correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear detalles", error = ex.Message });
            }
        }
        
    }
}
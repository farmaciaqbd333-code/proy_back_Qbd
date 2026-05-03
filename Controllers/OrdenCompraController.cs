using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace proy_back_Qbd.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class OrdenCompraController : ControllerBase
        {

                private readonly IOrdenCompraService _service;
                public OrdenCompraController(IOrdenCompraService service)
                {
                        _service = service;
                }

                [HttpGet]
                [SwaggerResponse(200, "Obtención exitosa exitosa", typeof(IEnumerable<ListaOrdenesYComprasRes>))]
                public async Task<ActionResult<IEnumerable<ListaOrdenesYComprasRes>>> ListarComprasYOrdenes()
                {

                        IEnumerable<ListaOrdenesYComprasRes> response = await _service.ListarOrdenesYCompras();

                        if (!response.Any())
                                return NotFound("No se han encontrado ordenes de compra y compras");

                        return Ok(response);
                }

                // [HttpGet("{id}")]
                // public async Task<ActionResult<DetalleOrdenCompraRes>> GetOrdenCompraById(int id)
                // {
                //     return await GetDetalleOrdenesCompra(id);
                // }
                // [HttpGet("detalle/{id}")]
                // public async Task<ActionResult<DetalleOrdenCompraRes>> GetDetalleOrdenesCompra(int id)
                // {

                //     DetalleOrdenCompraRes? orden = await _context.Compras
                //         .Where(w => w.Id == id)
                //         .Select(s => new DetalleOrdenCompraRes
                //         {
                //             Modalidad = s.Modalidad,
                //             TC = s.TipoCambio.ToString(),
                //             Moneda = s.Moneda ?? "PEN",
                //             FechaCotizacion = s.FechaCotizacion,
                //             Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                //             Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                //             Responsable = s.Sede != null ? (_context.Personas.Where(p => p.Id.ToString() == s.Sede.Encargado).Select(p => p.NombreCompleto).FirstOrDefault() ?? s.Sede.Encargado) : "",
                //             CodigoProveedor = s.Proveedor == null || s.Proveedor.CodigoProvedor == null ? "" : s.Proveedor.CodigoProvedor,
                //             Ruc = s.Proveedor != null ? s.Proveedor.CodigoProv : "",
                //             RazonSocial = s.Proveedor != null ? s.Proveedor.Datos : "",
                //             TipoOperacion = s.TipoOperacion,
                //             IncluyeImpuesto = s.IncluyeImpuesto,
                //             Observaciones = s.Observaciones,
                //             DetalleOrdenCompras = s.DetalleOrdenCompras == null
                //                                     ? null :
                //                                     s.DetalleOrdenCompras.Select(s2 => new DetalleOrdenCompra2
                //                                     {
                //                                         Id = s2.Id,
                //                                         IdInsumo = s2.IdInsumo,
                //                                         Codigo = s2.IdInsumo.ToString(),
                //                                         DescripcionQBD = s2.Insumo == null || s2.Insumo.Descripcion == null ? "" : s2.Insumo.Descripcion,
                //                                         DescripcionFactura = s2.DescripcionFac,
                //                                         Cantidad = s2.CantidadSolicitada.ToString(),
                //                                         UM = s2.Um,
                //                                         CUnitario = s2.CostoUnitario.ToString(),
                //                                         CTotal = s2.CostoTotal.ToString()
                //                                     })
                //                                     .ToList()

                //         }).FirstOrDefaultAsync();
                //     if (orden == null)
                //     {
                //         return NotFound("No encontrado");

                //     }

                //     return Ok(orden);
                // }

                // [HttpPost]
                // public async Task<ActionResult> CrearDetalleOrdenesCompra(OrdenCompraCreateReq request)
                // {
                //     Compra ordenCompra = new Compra
                //     {
                //         IdProveedor = request.IdProveedor,
                //         Modalidad = request.Modalidad,
                //         Moneda = request.Moneda,
                //         TipoCambio = request.TipoCambio,
                //         Igv = request.Impuesto,
                //         Observaciones = request.Observaciones,
                //         IdFamilia = request.IdFamilia,
                //         IdSede = request.IdSede,
                //         TipoOperacion = request.TipoOperacion ?? "GRAVADO",
                //         IncluyeImpuesto = request.IncluyeImpuesto,
                //         EstadoPago = "PEN",
                //         Estado = "",
                //         IdCreador = request.CreadorId,
                //         IdModificador = request.ModificadorId,
                //         FechaModificacion = DateTime.Now,
                //         FechaCotizacion = request.FechaEmision,
                //         TipoTributario = request.TipoTributario,
                //         EstadoMeson = "PENDIENTE"
                //     };
                //     _context.Compras.Add(ordenCompra);
                //     await _context.SaveChangesAsync();

                //     List<DetalleCompra> detalleOrdenCompras = request.Detalle.Select(s => new DetalleCompra
                //     {
                //         IdInsumo = s.IdInsumo,
                //         DescripcionFac = s.DescripcionFac,
                //         CantidadSolicitada = s.Cantidad,
                //         Um = s.UM,
                //         CostoUnitario = s.CUnitario,
                //         CostoTotal = s.CTotal,
                //         IdCompra = ordenCompra.Id,
                //         FechaModificacion = ordenCompra.FechaCreacion,
                //         IdCreador = request.CreadorId,
                //         IdModificador = request.ModificadorId // Asignar el modificador desde el request
                //     }).ToList();

                //     _context.DetalleOrdenesCompras.AddRange(detalleOrdenCompras);
                //     await _context.SaveChangesAsync();

                //     ordenCompra.DetalleOrdenCompras = detalleOrdenCompras;
                //     return Ok(ordenCompra);
                // }
                // [HttpPut("{id}")]
                // public async Task<IActionResult> UpdateOrdenCompra(int id, [FromBody] OrdenCompraUpdateReq req)
                // {
                //     Compra? orden = await _context.Compras.FindAsync(id);

                //     if (orden == null)
                //     {
                //         return NotFound(new { message = "Orden de compra no encontrada" });
                //     }

                //     Mapear campos
                //     orden.IdProveedor = req.IdProveedor;
                //     orden.Modalidad = req.Modalidad;
                //     orden.Moneda = req.Moneda;
                //     orden.TipoCambio = req.TipoCambio;
                //     orden.Igv = req.Impuesto;
                //     orden.FechaCotizacion = req.FechaEmision;
                //     orden.Observaciones = req.Observaciones;
                //     orden.IdFamilia = req.IdFamilia;
                //     orden.IdSede = req.IdSede;
                //     orden.TipoOperacion = req.TipoOperacion;
                //     orden.IncluyeImpuesto = req.IncluyeImpuesto;
                //     orden.TipoTributario = req.TipoTributario;
                //     orden.IdModificador = req.ModificadorId;
                //     orden.FechaModificacion = DateTime.Now;

                //     try
                //     {
                //         await _context.SaveChangesAsync();
                //         return Ok(new { message = "Orden de compra actualizada correctamente" });
                //     }
                //     catch (Exception ex)
                //     {
                //         return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
                //     }
                // }

                // [HttpDelete("{id}")]
                // public async Task<IActionResult> DeleteOrdenCompra(int id)
                // {
                //     var orden = await _context.Compras
                //         .Include(o => o.DetalleOrdenCompras)
                //         .Include(o => o.Compra)
                //         .FirstOrDefaultAsync(o => o.Id == id);

                //     if (orden == null)
                //     {
                //         return NotFound(new { message = "Orden de compra no encontrada" });
                //     }

                //     if (orden.Compra != null)
                //     {
                //         return BadRequest(new { message = "No se puede eliminar una orden que ya tiene una factura registrada" });
                //     }

                //     if (orden.DetalleOrdenCompras != null && orden.DetalleOrdenCompras.Any())
                //     {
                //         _context.DetalleOrdenesCompras.RemoveRange(orden.DetalleOrdenCompras);
                //     }

                //     _context.Compras.Remove(orden);

                //     try
                //     {
                //         await _context.SaveChangesAsync();
                //         return Ok(new { message = "Orden de compra eliminada correctamente" });
                //     }
                //     catch (Exception ex)
                //     {
                //         return StatusCode(500, new { message = "Error al eliminar la orden", error = ex.Message });
                //     }
                // }
                // [HttpPatch("detalles/{id}")]
                // public async Task<IActionResult> PatchDetallesOrdenCompra(int id, [FromBody] List<DetalleOrdenCompraPatchReq> detallesPatch)
                // {
                //     var orden = await _context.Compras
                //         .Include(o => o.DetalleOrdenCompras)
                //             .ThenInclude(d => d.Insumo)
                //         .FirstOrDefaultAsync(o => o.Id == id);

                //     if (orden == null)
                //         return NotFound(new { message = "Orden de compra no encontrada" });

                //     if (orden.DetalleOrdenCompras == null || !orden.DetalleOrdenCompras.Any())
                //         return NotFound(new { message = "No hay detalles para esta orden" });

                //     foreach (var patch in detallesPatch)
                //     {
                //         Buscar por ID primario de la fila, no por el insumo
                //         var detalle = orden.DetalleOrdenCompras
                //             .FirstOrDefault(d => d.Id == patch.Id);

                //         if (detalle == null)
                //             continue;

                //         CAMBIAR EL INSUMO (si se seleccionó otro)
                //         if (patch.IdInsumo.HasValue && patch.IdInsumo.Value > 0)
                //         {
                //             detalle.IdInsumo = patch.IdInsumo.Value;
                //         }

                //         Actualizar Descripción QBD (Maestro de Insumos)
                //         if (patch.DescripcionQbd != null && detalle.Insumo != null)
                //         {
                //             detalle.Insumo.Descripcion = patch.DescripcionQbd;
                //         }

                //         Actualizar Descripción Factura (Local de la Orden)
                //         if (patch.DescripcionFac != null)
                //         {
                //             detalle.DescripcionFac = patch.DescripcionFac;
                //         }

                //         if (patch.Cantidad.HasValue)
                //             detalle.CantidadSolicitada = patch.Cantidad.Value;

                //         if (patch.Um != null)
                //             detalle.Um = patch.Um;

                //         if (patch.CostoUnitario.HasValue)
                //             detalle.CostoUnitario = patch.CostoUnitario.Value;

                //         if (patch.CostoTotal.HasValue)
                //             detalle.CostoTotal = patch.CostoTotal.Value;

                //         detalle.IdModificador = patch.ModificadorId;
                //         detalle.FechaModificacion = DateTime.UtcNow;
                //     }
                //     try
                //     {
                //         await _context.SaveChangesAsync();
                //         return Ok(new { message = "Detalles actualizados correctamente" });
                //     }
                //     catch (Exception ex)
                //     {
                //         return StatusCode(500, new { message = "Error al actualizar detalles", error = ex.Message });
                //     }
                // }
                // [HttpDelete("detalles/{idOrden}/{idInsumo}")]
                // public async Task<IActionResult> DeleteDetalleOrdenCompra(int idOrden, int idInsumo)
                // {
                //     var detalle = await _context.DetalleOrdenesCompras
                //         .FirstOrDefaultAsync(d => d.IdCompra == idOrden && d.IdInsumo == idInsumo);

                //     if (detalle == null)
                //         return NotFound(new { message = "Detalle no encontrado" });

                //     _context.DetalleOrdenesCompras.Remove(detalle);

                //     try
                //     {
                //         await _context.SaveChangesAsync();
                //         return Ok(new { message = "Detalle eliminado correctamente" });
                //     }
                //     catch (Exception ex)
                //     {
                //         return StatusCode(500, new { message = "Error al eliminar detalle", error = ex.Message });
                //     }
                // }
                // [HttpPost("detalles/{id}")]
                // public async Task<IActionResult> CreateDetallesOrdenCompra(int id, [FromBody] List<DetalleOrdenCompraCreateReq> detalles)
                // {
                //     var orden = await _context.Compras
                //         .Include(o => o.DetalleOrdenCompras)
                //         .FirstOrDefaultAsync(o => o.Id == id);

                //     if (orden == null)
                //         return NotFound(new { message = "Orden de compra no encontrada" });

                //     var nuevosDetalles = new List<DetalleCompra>();

                //     foreach (var item in detalles)
                //     {
                //         Validación básica
                //         if (item.Cantidad <= 0 || item.CostoUnitario < 0)
                //             return BadRequest(new { message = "Cantidad o costo inválido" });

                //         Validar duplicados (opcional)
                //         if (orden.DetalleOrdenCompras != null)
                //         {
                //             var existe = orden.DetalleOrdenCompras
                //             .Any(d => d.IdInsumo == item.IdInsumo);

                //             if (existe)
                //                 return Ok(new { message = "Ya existe este Insumo" });
                //         }

                //         var detalle = new DetalleCompra
                //         {
                //             IdCompra = id,
                //             IdInsumo = item.IdInsumo,
                //             DescripcionFac = item.DescripcionFac,
                //             CantidadSolicitada = item.Cantidad,
                //             Um = item.Um,
                //             CostoUnitario = item.CostoUnitario,
                //             CostoTotal = item.CostoTotal,
                //             IdCreador = item.CreadorId,
                //             IdModificador = item.CreadorId,
                //             FechaModificacion = DateTime.UtcNow
                //         };

                //         nuevosDetalles.Add(detalle);
                //     }

                //     if (!nuevosDetalles.Any())
                //         return BadRequest(new { message = "No hay detalles válidos para insertar" });

                //     await _context.DetalleOrdenesCompras.AddRangeAsync(nuevosDetalles);

                //     try
                //     {
                //         await _context.SaveChangesAsync();
                //         return Ok(new { message = "Detalles creados correctamente" });
                //     }
                //     catch (Exception ex)
                //     {
                //         return StatusCode(500, new { message = "Error al crear detalles", error = ex.Message });
                //     }
                // }

                // [HttpPatch("meson/{id}")]
                // public async Task<IActionResult> PatchMesonOrdenCompra(int id, [FromBody] PatchMesonDto request)
                // {
                //     var orden = await _context.Compras.FindAsync(id);
                //     if (orden == null) return NotFound(new { message = "Orden no encontrada" });

                //     orden.EstadoMeson = request.EstadoMeson;
                //     await _context.SaveChangesAsync();
                //     return Ok(new { message = "Estado mesón actualizado" });
                // }

                // [HttpPatch("pago/{id}")]
                // public async Task<IActionResult> PatchEstadoPago(int id, [FromBody] PatchPagoDto request)
                // {
                //     var orden = await _context.Compras.FindAsync(id);
                //     if (orden == null) return NotFound(new { message = "Orden no encontrada" });

                //     orden.Modalidad = request.EstadoPago;
                //     await _context.SaveChangesAsync();
                //     return Ok(new { message = "Estado de pago actualizado" });
                // }

        }
}
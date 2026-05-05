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

                private readonly IOrdenCompraService _serviceOC;
                public OrdenCompraController(IOrdenCompraService service)
                {
                        _serviceOC = service;
                }
                /// <summary>
                /// Listar ordenes de compra y compras
                /// </summary>
                [HttpGet]
                [SwaggerResponse(200, "Obtención exitosa", typeof(IEnumerable<OrdenesYComprasRes>))]
                public async Task<ActionResult<IEnumerable<OrdenesYComprasRes>>> ListarComprasYOrdenes()
                {

                        IEnumerable<OrdenesYComprasRes> response = await _serviceOC.ListaOrdenesYCompras();

                        if (!response.Any())
                                return NotFound("No se han encontrado ordenes de compra y compras");

                        return Ok(response);
                }

                /// <summary>
                /// Obtener detalle de orden de compra o compra
                /// </summary>
                [HttpGet("detalle/{id}")]
                [SwaggerResponse(200, "Obtención de detalle exitosa", typeof(ObtenerOrdenOCompraRes))]
                public async Task<ActionResult<ObtenerOrdenOCompraRes>> ObtenerOrdenCompra(int id)
                {

                        ObtenerOrdenOCompraRes? response = await _serviceOC.ObtenerDetalleOrdenOCompra(id);

                        if (response == null)
                                return NotFound("No se ha encontrado el detalle de la orden o compra");

                        return Ok(response);
                }

                /// <summary>
                /// Crear orden de compra
                /// </summary>
                [HttpPost]
                [SwaggerResponse(200, "Creación exitosa", typeof(OrdenesYComprasRes))]
                public async Task<ActionResult<OrdenesYComprasRes>> CrearOrdenesCompra(OrdenCompraCreateReq request)
                {
                        int? id = await _serviceOC.CrearOrdenDeCompra(request);
                        if (id == null)
                                return StatusCode(500, "Hubo un error al crear la orden de compra");

                        OrdenesYComprasRes? response = await _serviceOC.ObtenerOrdenOCompra(id.Value);
                        if (response == null)
                                return NotFound("No se ha encontrado la orden de compra");

                        return Created("Se ha creado la orden de compra", response);
                }

                [HttpPatch("{id}")]
                [SwaggerResponse(200, "Actualización exitosa", typeof(OrdenesYComprasRes))]
                public async Task<ActionResult<OrdenesYComprasRes>> ActualizarOrdenCompra(int id, [FromBody] OrdenCompraUpdateReq request)
                {
                        OrdenesYComprasRes? response = await _serviceOC.ActualizarOrdenDeCompra(id, request);

                        if (response == null)
                                return NotFound("No se ha encontrado la orden de compra o compra");

                        return Ok(response);
                }

                /// <summary>
                /// Eliminar orden de compra o compra
                /// </summary>
                [HttpDelete("{id}")]
                public async Task<IActionResult> DeleteOrdenCompra(int id)
                {
                        string? response = await _serviceOC.EliminarOrdenOCompraOCompra(id);
                        if (response == null)
                                return NotFound("Orden no encontrado");

                        return Ok(response);

                }
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
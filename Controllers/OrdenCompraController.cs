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
                /// <summary>
                /// Actualizar orden de compra
                /// </summary>
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

                /// <summary>
                /// Convertir a compra
                /// </summary>
                [HttpPatch("meson/{ordenCompraId}")]
                [SwaggerResponse(200, "Creación exitosa", typeof(OrdenesYComprasRes))]
                public async Task<ActionResult<OrdenesYComprasRes>> ConvertirCompra(int ordenCompraId, ConvertirCompraReq request)
                {
                        OrdenesYComprasRes? response = await _serviceOC.ConvertirCompra(ordenCompraId, request);
                        if (response == null) return NotFound(new { message = "Compra no encontrada" });

                        return Ok(response);
                }

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

                /// <summary>
                /// Jalar descripcion de factura segun proveedor
                /// </summary>
                [HttpGet("descripciones/{proveedorId}")]
                public async Task<ActionResult<DescripcionFacturaRes>> DescripcionFactura(int proveedorId)
                {
                        DescripcionFacturaRes response = await _serviceOC.DescripcionFactura(proveedorId);
                        if (!response.DescripcionFactura.Any())
                                return NotFound(new { message = "No se encontro alguna descripcion" });

                        return Ok(response);
                }

        }
}
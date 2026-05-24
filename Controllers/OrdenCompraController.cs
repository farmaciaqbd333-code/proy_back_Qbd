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
                /// Listar ordenes de compra
                /// </summary>
                [HttpGet]
                [SwaggerResponse(200, "Obtención exitosa", typeof(IEnumerable<OrdenesYComprasRes>))]
                public async Task<ActionResult<IEnumerable<OrdenesYComprasRes>>> ListarComprasYOrdenes()
                {

                        IEnumerable<OrdenesYComprasRes> response = await _serviceOC.ListaOrdenesDeCompras();
                        return Ok(response);
                }

                /// <summary>
                /// Obtener detalle de orden de compra
                /// </summary>
                [HttpGet("detalle/{id}")]
                [SwaggerResponse(200, "Obtención de detalle exitosa", typeof(OrdenCompraGetRes))]
                public async Task<ActionResult<OrdenCompraGetRes>> ObtenerOrdenCompra(int id)
                {

                        OrdenCompraGetRes? response = await _serviceOC.ObtenerOrdenCompra(id);

                        if (response == null)
                                return NotFound("No se ha encontrado el detalle de la orden o compra");

                        return Ok(response);
                }

                /// <summary>
                /// Crear orden de compra
                /// </summary>
                [HttpPost]
                [SwaggerResponse(200, "Creación exitosa", typeof(OrdenesYComprasRes))]
                public async Task<ActionResult<OrdenesYComprasRes>> CrearOrdenesCompra(OrdenCreateReq request)
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
                public async Task<ActionResult<OrdenesYComprasRes>> ActualizarOrdenCompra(int id, [FromBody] OrdenUpdateReq request)
                {
                        OrdenesYComprasRes? response = await _serviceOC.ActualizarOrdenDeCompra(id, request);

                        if (response == null)
                                return NotFound("No se ha encontrado la orden de compra o compra");

                        return Ok(response);
                }

                /// <summary>
                /// Eliminar orden de compra
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
                public async Task<ActionResult<OrdenesYComprasRes>> ConvertirCompra(int ordenCompraId, ConvertirACompraReq request)
                {
                        OrdenesYComprasRes? response = await _serviceOC.ConvertirCompra(ordenCompraId, request);
                        if (response == null) return NotFound(new { message = "Compra no encontrada" });

                        return Ok(response);
                }


                /// <summary>
                /// Actualizar estado de compra
                /// </summary>
                [HttpPatch("estado/{idOrdenCompra}")]
                public async Task<IActionResult> CambiarEstadoCompra(int idOrdenCompra, CambiarEstadoReq request)
                {
                        bool response = await _serviceOC.ActualizarEstadoCompra(idOrdenCompra, request);
                        if (!response) return StatusCode(500, "No se actualizo");

                        return Ok(new { message = "Estado de compra actualizado" });
                }

                /// <summary>
                /// Actualizar ruta de la factura
                /// </summary>
                [HttpPatch("ruta-factura/{id}")]
                public async Task<IActionResult> ActualizarRutaFactura(int id, UpdateRutaFacturaReq request)
                {
                        bool response = await _serviceOC.ActualizarRutaFactura(id, request);
                        if (!response) return NotFound(new { message = "Orden no encontrada" });

                        return Ok(new { message = "Ruta de factura actualizada" });
                }

                /// <summary>
                /// Jalar descripciones de los insumos segun el proveedor
                /// </summary>
                [HttpGet("descripciones/{proveedorId}")]
                public async Task<ActionResult<DescripcionFacturaRes>> DescripcionFactura(int proveedorId)
                {
                        DescripcionFacturaRes response = await _serviceOC.DescripcionFactura(proveedorId);
                        if (!response.DescripcionFactura.Any())
                                return NotFound(new { message = "No se encontro alguna descripcion" });

                        return Ok(response);
                }
                /// <summary>
                /// Obtener datos para meson
                /// </summary>
                [HttpGet("meson/{ordenId}")]
                public async Task<ActionResult<OrdenMesonRes>> ObtenerDatosMeson(int ordenId)
                {
                        OrdenMesonRes? response = await _serviceOC.ObtenerCompraMeson(ordenId);

                        if (response == null)
                                return NotFound(new { message = "No se encontro alguna descripcion" });

                        return Ok(response);
                }

                /// <summary>
                /// Listar facturas por familia (ej: MP, PT, etc.)
                /// </summary>
                [HttpGet("facturas-por-familia/{familia}")]
                [SwaggerResponse(200, "Obtención exitosa", typeof(IEnumerable<OrdenesYComprasRes>))]
                public async Task<ActionResult<IEnumerable<OrdenesYComprasRes>>> ListarFacturasPorFamilia(string familia)
                {
                        if (string.IsNullOrWhiteSpace(familia))
                                return BadRequest(new { message = "Debe indicar una familia" });

                        IEnumerable<OrdenesYComprasRes> response = await _serviceOC.ListaFacturasPorFamilia(familia);

                        if (!response.Any())
                                return NotFound(new { message = $"No se encontraron facturas para la familia '{familia}'" });

                        return Ok(response);
                }

        }
}
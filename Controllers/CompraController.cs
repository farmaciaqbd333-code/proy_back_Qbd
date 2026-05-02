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

// namespace proy_back_Qbd.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class OrdenCompraController : ControllerBase
//     {
//         private readonly ApiContext _context;
//         public OrdenCompraController(ApiContext context)
//         {
//             _context = context;
//         }


//         [HttpGet]
//         public async Task<ActionResult<IEnumerable<ListadoOrdenCompra>>> GetOrdenesCompra()
//         {

//             var data = await _context.Compras
//                 .Include(i => i.Proveedor)
//                 .Include(i => i.Compra)
//                 .Include(i => i.DetalleOrdenCompras)
//                 .Include(i => i.Sede)
//                 .Include(i => i.Familia)
//                 .Include(i => i.Modificador)
//                 .OrderByDescending(o => o.Id)
//                 .ToListAsync();

//             var ordenes = data.Select(s => new ListadoOrdenCompra
//             {
//                 Id = s.Id,
//                 CUO = "OC-" + s.Id,
//                 Fecha = s.FechaCreacion.ToString("dd/MM/yyyy"),

//                 Serie = s.Compra != null && !string.IsNullOrEmpty(s.Compra.CodFactura) && s.Compra.CodFactura.Contains('-') ? s.Compra.CodFactura.Split('-')[0] : "",
//                 Numero = s.Compra != null && !string.IsNullOrEmpty(s.Compra.CodFactura) ? (s.Compra.CodFactura.Contains('-') ? s.Compra.CodFactura.Split('-')[1] : s.Compra.CodFactura) : "",
//                 RUC = s.Proveedor?.CodigoProv ?? "",
//                 Denominacion = s.Proveedor?.Datos ?? "",
//                 Valor = s.DetalleOrdenCompras != null ? s.DetalleOrdenCompras.Sum(sm => sm.CostoTotal).ToString("F2") : "0.00",
//                 Total = s.DetalleOrdenCompras != null ? (s.DetalleOrdenCompras.Sum(sm => sm.CostoTotal) * 1.18m).ToString("F2") : "0.00",
//                 Moneda = s.Moneda ?? "PEN",
//                 Estado = s.Modalidad,
//                 CodFac = s.Compra?.CodFactura ?? "",
//                 Familia = s.Familia != null ? (s.Familia.Nombre == "Insumos" ? "MP" : s.Familia.Nombre) : "N/A",
//                 Factura = s.Compra != null ? "SI" : "NO",
//                 Modalidad = s.Modalidad,
//                 EstadoOrdenCompra = s.Estado ?? "PEN",
//                 Usuario = s.Modificador != null ? (s.Modificador.Codigo ?? s.ModificadorId.ToString()) : "N/A",
//                 EstadoMeson = s.EstadoMeson
//             }).ToList();

//             return Ok(ordenes);
//         }
//         [HttpGet("{id}")]
//         public async Task<ActionResult<DetalleOrdenCompraRes>> GetOrdenCompraById(int id)
//         {
//             return await GetDetalleOrdenesCompra(id);
//         }
//         [HttpGet("detalle/{id}")]
//         public async Task<ActionResult<DetalleOrdenCompraRes>> GetDetalleOrdenesCompra(int id)
//         {

//             DetalleOrdenCompraRes? orden = await _context.Compras
//                 .Where(w => w.Id == id)
//                 .Select(s => new DetalleOrdenCompraRes
//                 {
//                     Modalidad = s.Modalidad,
//                     TC = s.TipoCambio.ToString(),
//                     Moneda = s.Moneda ?? "PEN",
//                     FechaCotizacion = s.FechaCotizacion,
//                     Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
//                     Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
//                     Responsable = s.Sede != null ? (_context.Personas.Where(p => p.Id.ToString() == s.Sede.Encargado).Select(p => p.NombreCompleto).FirstOrDefault() ?? s.Sede.Encargado) : "",
//                     CodigoProveedor = s.Proveedor == null || s.Proveedor.CodigoProvedor == null ? "" : s.Proveedor.CodigoProvedor,
//                     Ruc = s.Proveedor != null ? s.Proveedor.CodigoProv : "",
//                     RazonSocial = s.Proveedor != null ? s.Proveedor.Datos : "",
//                     TipoOperacion = s.TipoOperacion,
//                     IncluyeImpuesto = s.IncluyeImpuesto,
//                     Observaciones = s.Observaciones,
//                     DetalleOrdenCompras = s.DetalleOrdenCompras == null
//                                             ? null :
//                                             s.DetalleOrdenCompras.Select(s2 => new DetalleOrdenCompra2
//                                             {
//                                                 Id = s2.Id,
//                                                 IdInsumo = s2.IdInsumo,
//                                                 Codigo = s2.IdInsumo.ToString(),
//                                                 DescripcionQBD = s2.Insumo == null || s2.Insumo.Descripcion == null ? "" : s2.Insumo.Descripcion,
//                                                 DescripcionFactura = s2.DescripcionFac,
//                                                 Cantidad = s2.Cantidad.ToString(),
//                                                 UM = s2.Um,
//                                                 CUnitario = s2.CostoUnitario.ToString(),
//                                                 CTotal = s2.CostoTotal.ToString()
//                                             })
//                                             .ToList()

//                 }).FirstOrDefaultAsync();
//             if (orden == null)
//             {
//                 return NotFound("No encontrado");

//             }

//             return Ok(orden);
//         }

//         [HttpPost]
//         public async Task<ActionResult> CrearDetalleOrdenesCompra(OrdenCompraCreateReq request)
//         {
//             Compra ordenCompra = new Compra
//             {
//                 IdProveedor = request.IdProveedor,
//                 Modalidad = request.Modalidad,
//                 Moneda = request.Moneda,
//                 TipoCambio = request.TipoCambio,
//                 Igv = request.Impuesto,
//                 Observaciones = request.Observaciones,
//                 IdFamilia = request.IdFamilia,
//                 IdSede = request.IdSede,
//                 TipoOperacion = request.TipoOperacion ?? "GRAVADO",
//                 IncluyeImpuesto = request.IncluyeImpuesto,
//                 EstadoPago = "PEN",
//                 Estado = "",
//                 IdCreador = request.CreadorId,
//                 IdModificador = request.ModificadorId,
//                 FechaModificacion = DateTime.Now,
//                 FechaCotizacion = request.FechaEmision,
//                 TipoTributario = request.TipoTributario,
//                 EstadoMeson = "PENDIENTE"
//             };
//             _context.Compras.Add(ordenCompra);
//             await _context.SaveChangesAsync();

//             List<DetalleCompra> detalleOrdenCompras = request.Detalle.Select(s => new DetalleCompra
//             {
//                 IdInsumo = s.IdInsumo,
//                 DescripcionFac = s.DescripcionFac,
//                 Cantidad = s.Cantidad,
//                 Um = s.UM,
//                 CostoUnitario = s.CUnitario,
//                 CostoTotal = s.CTotal,
//                 IdCompra = ordenCompra.Id,
//                 FechaModificacion = ordenCompra.FechaCreacion,
//                 IdCreador = request.CreadorId,
//                 IdModificador = request.ModificadorId // Asignar el modificador desde el request
//             }).ToList();

//             _context.DetalleOrdenesCompras.AddRange(detalleOrdenCompras);
//             await _context.SaveChangesAsync();

//             ordenCompra.DetalleOrdenCompras = detalleOrdenCompras;
//             return Ok(ordenCompra);
//         }
//         [HttpPut("{id}")]
//         public async Task<IActionResult> UpdateOrdenCompra(int id, [FromBody] OrdenCompraUpdateReq req)
//         {
//             Compra? orden = await _context.Compras.FindAsync(id);

//             if (orden == null)
//             {
//                 return NotFound(new { message = "Orden de compra no encontrada" });
//             }

//             // Mapear campos
//             orden.IdProveedor = req.IdProveedor;
//             orden.Modalidad = req.Modalidad;
//             orden.Moneda = req.Moneda;
//             orden.TipoCambio = req.TipoCambio;
//             orden.Igv = req.Impuesto;
//             orden.FechaCotizacion = req.FechaEmision;
//             orden.Observaciones = req.Observaciones;
//             orden.IdFamilia = req.IdFamilia;
//             orden.IdSede = req.IdSede;
//             orden.TipoOperacion = req.TipoOperacion;
//             orden.IncluyeImpuesto = req.IncluyeImpuesto;
//             orden.TipoTributario = req.TipoTributario;
//             orden.IdModificador = req.ModificadorId;
//             orden.FechaModificacion = DateTime.Now;

//             try
//             {
//                 await _context.SaveChangesAsync();
//                 return Ok(new { message = "Orden de compra actualizada correctamente" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
//             }
//         }

//         [HttpDelete("{id}")]
//         public async Task<IActionResult> DeleteOrdenCompra(int id)
//         {
//             var orden = await _context.Compras
//                 .Include(o => o.DetalleOrdenCompras)
//                 .Include(o => o.Compra)
//                 .FirstOrDefaultAsync(o => o.Id == id);

//             if (orden == null)
//             {
//                 return NotFound(new { message = "Orden de compra no encontrada" });
//             }

//             if (orden.Compra != null)
//             {
//                 return BadRequest(new { message = "No se puede eliminar una orden que ya tiene una factura registrada" });
//             }

//             if (orden.DetalleOrdenCompras != null && orden.DetalleOrdenCompras.Any())
//             {
//                 _context.DetalleOrdenesCompras.RemoveRange(orden.DetalleOrdenCompras);
//             }

//             _context.Compras.Remove(orden);

//             try
//             {
//                 await _context.SaveChangesAsync();
//                 return Ok(new { message = "Orden de compra eliminada correctamente" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { message = "Error al eliminar la orden", error = ex.Message });
//             }
//         }
//         [HttpPatch("detalles/{id}")]
//         public async Task<IActionResult> PatchDetallesOrdenCompra(int id, [FromBody] List<DetalleOrdenCompraPatchReq> detallesPatch)
//         {
//             var orden = await _context.Compras
//                 .Include(o => o.DetalleOrdenCompras)
//                     .ThenInclude(d => d.Insumo)
//                 .FirstOrDefaultAsync(o => o.Id == id);

//             if (orden == null)
//                 return NotFound(new { message = "Orden de compra no encontrada" });

//             if (orden.DetalleOrdenCompras == null || !orden.DetalleOrdenCompras.Any())
//                 return NotFound(new { message = "No hay detalles para esta orden" });

//             foreach (var patch in detallesPatch)
//             {
//                 // Buscar por ID primario de la fila, no por el insumo
//                 var detalle = orden.DetalleOrdenCompras
//                     .FirstOrDefault(d => d.Id == patch.Id);

//                 if (detalle == null)
//                     continue;

//                 // CAMBIAR EL INSUMO (si se seleccionó otro)
//                 if (patch.IdInsumo.HasValue && patch.IdInsumo.Value > 0)
//                 {
//                     detalle.IdInsumo = patch.IdInsumo.Value;
//                 }

//                 // Actualizar Descripción QBD (Maestro de Insumos)
//                 if (patch.DescripcionQbd != null && detalle.Insumo != null)
//                 {
//                     detalle.Insumo.Descripcion = patch.DescripcionQbd;
//                 }

//                 // Actualizar Descripción Factura (Local de la Orden)
//                 if (patch.DescripcionFac != null)
//                 {
//                     detalle.DescripcionFac = patch.DescripcionFac;
//                 }

//                 if (patch.Cantidad.HasValue)
//                     detalle.Cantidad = patch.Cantidad.Value;

//                 if (patch.Um != null)
//                     detalle.Um = patch.Um;

//                 if (patch.CostoUnitario.HasValue)
//                     detalle.CostoUnitario = patch.CostoUnitario.Value;

//                 if (patch.CostoTotal.HasValue)
//                     detalle.CostoTotal = patch.CostoTotal.Value;

//                 detalle.IdModificador = patch.ModificadorId;
//                 detalle.FechaModificacion = DateTime.UtcNow;
//             }
//             try
//             {
//                 await _context.SaveChangesAsync();
//                 return Ok(new { message = "Detalles actualizados correctamente" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { message = "Error al actualizar detalles", error = ex.Message });
//             }
//         }
//         [HttpDelete("detalles/{idOrden}/{idInsumo}")]
//         public async Task<IActionResult> DeleteDetalleOrdenCompra(int idOrden, int idInsumo)
//         {
//             var detalle = await _context.DetalleOrdenesCompras
//                 .FirstOrDefaultAsync(d => d.IdCompra == idOrden && d.IdInsumo == idInsumo);

//             if (detalle == null)
//                 return NotFound(new { message = "Detalle no encontrado" });

//             _context.DetalleOrdenesCompras.Remove(detalle);

//             try
//             {
//                 await _context.SaveChangesAsync();
//                 return Ok(new { message = "Detalle eliminado correctamente" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { message = "Error al eliminar detalle", error = ex.Message });
//             }
//         }
//         [HttpPost("detalles/{id}")]
//         public async Task<IActionResult> CreateDetallesOrdenCompra(int id, [FromBody] List<DetalleOrdenCompraCreateReq> detalles)
//         {
//             var orden = await _context.Compras
//                 .Include(o => o.DetalleOrdenCompras)
//                 .FirstOrDefaultAsync(o => o.Id == id);

//             if (orden == null)
//                 return NotFound(new { message = "Orden de compra no encontrada" });

//             var nuevosDetalles = new List<DetalleCompra>();

//             foreach (var item in detalles)
//             {
//                 // Validación básica
//                 if (item.Cantidad <= 0 || item.CostoUnitario < 0)
//                     return BadRequest(new { message = "Cantidad o costo inválido" });

//                 // Validar duplicados (opcional)
//                 if (orden.DetalleOrdenCompras != null)
//                 {
//                     var existe = orden.DetalleOrdenCompras
//                     .Any(d => d.IdInsumo == item.IdInsumo);

//                     if (existe)
//                         return Ok(new { message = "Ya existe este Insumo" });
//                 }

//                 var detalle = new DetalleCompra
//                 {
//                     IdCompra = id,
//                     IdInsumo = item.IdInsumo,
//                     DescripcionFac = item.DescripcionFac,
//                     Cantidad = item.Cantidad,
//                     Um = item.Um,
//                     CostoUnitario = item.CostoUnitario,
//                     CostoTotal = item.CostoTotal,
//                     IdCreador = item.CreadorId,
//                     IdModificador = item.CreadorId,
//                     FechaModificacion = DateTime.UtcNow
//                 };

//                 nuevosDetalles.Add(detalle);
//             }

//             if (!nuevosDetalles.Any())
//                 return BadRequest(new { message = "No hay detalles válidos para insertar" });

//             await _context.DetalleOrdenesCompras.AddRangeAsync(nuevosDetalles);

//             try
//             {
//                 await _context.SaveChangesAsync();
//                 return Ok(new { message = "Detalles creados correctamente" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { message = "Error al crear detalles", error = ex.Message });
//             }
//         }

//         [HttpPatch("meson/{id}")]
//         public async Task<IActionResult> PatchMesonOrdenCompra(int id, [FromBody] PatchMesonDto request)
//         {
//             var orden = await _context.Compras.FindAsync(id);
//             if (orden == null) return NotFound(new { message = "Orden no encontrada" });

//             orden.EstadoMeson = request.EstadoMeson;
//             await _context.SaveChangesAsync();
//             return Ok(new { message = "Estado mesón actualizado" });
//         }

//         [HttpPatch("pago/{id}")]
//         public async Task<IActionResult> PatchEstadoPago(int id, [FromBody] PatchPagoDto request)
//         {
//             var orden = await _context.Compras.FindAsync(id);
//             if (orden == null) return NotFound(new { message = "Orden no encontrada" });

//             orden.Modalidad = request.EstadoPago;
//             await _context.SaveChangesAsync();
//             return Ok(new { message = "Estado de pago actualizado" });
//         }
// [HttpPost]
// public async Task<ActionResult> CrearCompra(CompraCreateReq request)
// {
//     Compra compra = new()
//     {
//         CodFactura = request.CodFactura,
//         Guia = request.Guia,
//         CodFacturaQBD = request.CodFacturaQBD,
//         Id = request.IdOrdenCompra,
//         IdCreador = request.IdUsuario,
//         IdModificador = request.IdUsuario,
//         FechaFactura = request.FechaFactura,
//         ImgFactura = request.ImgFactura,
//     };
//     _context.Compras.Add(compra);
//     await _context.SaveChangesAsync();

//     if (compra.Id != null)
//     {
//         List<DetalleCompra> detallesCompra = request.Detalle.Select(s => new DetalleCompra
//         {
//             Lote = s.Lote,
//             FechaFab = s.FechaFab,
//             FechaVec = s.FechaVec,
//             Coa = s.Coa,
//             RegistroSanitario = s.RegistroSanitario,
//             Conformidad = s.Conformidad,
//             IdCreador = s.IdCreador,

//         }).ToList();
//     }

//     return Ok();
// }
// [HttpGet]
// public async Task<ActionResult<List<CompraGetRes>>> ListarCompra()
// {
//     List<CompraGetRes> lista = await _context.Compras.Select(s => new CompraGetRes
//     {
//         IdCompra = s.Id,
//         CodCompra = "BDCO-" + s.Id,
//         FechaCreacion = s.FechaCreacion.ToString(),
//         CodFactura = s.CodFactura,
//         Guia = s.Guia,
//         CodFacturaQBD = s.CodFacturaQBD,
//         RUC = s.OrdenCompra.Proveedor.CodigoProv,
//         Proveedor = s.OrdenCompra.Proveedor.Datos,
//         Estado = "LABORATORIO",
//         OrdenCompra = "OC01-" + s.OrdenCompra.Id,
//         Usuario = s.Creador.Persona.NombreCompleto,
//         Familia = s.OrdenCompra.Familia.Nombre,
//     }).ToListAsync();

//     return lista;
// }
// [HttpPatch]
// public async Task<ActionResult> EditarDetalleCompra()
// {

// }
// [HttpPatch("{id}")]
// public async Task<IActionResult> ActualizarDetalleCompra(int id, PatchDetalleCompraReq request)
// {
//     DetalleCompra? detalleCompra = await _context.DetalleCompras.FindAsync(id);
//     if (detalleCompra == null)
//         return NotFound(new { message = "Detalle de compra no encontrado" });

//     // Actualización parcial (solo si vienen valores)
//     if (request.Coa != null)
//         detalleCompra.Coa = request.Coa;

//     if (request.Lote != null)
//         detalleCompra.Lote = request.Lote;

//     if (request.Cantidad.HasValue)
//         detalleCompra.CantidadPaquete = request.Cantidad.Value;

//     if (request.PotenciaPorcentaje != null)
//         detalleCompra.Potencia = request.PotenciaPorcentaje;

//     if (request.FechaFabricacion.HasValue)
//         detalleCompra.FechaFab = request.FechaFabricacion.Value;

//     if (request.FechaVencimiento.HasValue)
//         detalleCompra.FechaVec = request.FechaVencimiento.Value;

//     if (request.CondicionAlmacenamiento != null)
//         detalleCompra.CondicionAlmacenamiento = request.CondicionAlmacenamiento;

//     try
//     {
//         await _context.SaveChangesAsync();
//         return Ok(new { message = "Detalle de compra actualizado correctamente" });
//     }
//     catch (Exception ex)
//     {
//         return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
//     }

//     return Ok();
// }
//     }
// }
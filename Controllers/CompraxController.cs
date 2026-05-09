// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;

// namespace proy_back_Qbd.Controllers
// {
//     [Route("[controller]")]
//     public class aController : Controller
//     {
//         private readonly ILogger<aController> _logger;

//         public aController(ILogger<aController> logger)
//         {
//             _logger = logger;
//         }

//         [HttpPost]
//         public async Task<ActionResult> CrearCompra(CompraCreateReq request)
//         {
//             Compra compra = new()
//             {
//                 CodFactura = request.CodFactura,
//                 Guia = request.Guia,
//                 CodFacturaQBD = request.CodFacturaQBD,
//                 Id = request.IdOrdenCompra,
//                 IdCreador = request.IdUsuario,
//                 IdModificador = request.IdUsuario,
//                 FechaFactura = request.FechaFactura,
//                 ImgFactura = request.ImgFactura,
//             };
//             _context.Compras.Add(compra);
//             await _context.SaveChangesAsync();

//             if (compra.Id != null)
//             {
//                 List<DetalleCompra> detallesCompra = request.Detalle.Select(s => new DetalleCompra
//                 {
//                     Lote = s.Lote,
//                     FechaFab = s.FechaFab,
//                     FechaVec = s.FechaVec,
//                     Coa = s.Coa,
//                     RegistroSanitario = s.RegistroSanitario,
//                     Conformidad = s.Conformidad,
//                     IdCreador = s.IdCreador,

//                 }).ToList();
//             }

//             return Ok();
//         }
//         [HttpGet]
//         public async Task<ActionResult<List<CompraGetRes>>> ListarCompra()
//         {
//             List<CompraGetRes> lista = await _context.Compras.Select(s => new CompraGetRes
//             {
//                 IdCompra = s.Id,
//                 CodCompra = "BDCO-" + s.Id,
//                 FechaCreacion = s.FechaCreacion.ToString(),
//                 CodFactura = s.CodFactura,
//                 Guia = s.Guia,
//                 CodFacturaQBD = s.CodFacturaQBD,
//                 RUC = s.OrdenCompra.Proveedor.CodigoProv,
//                 Proveedor = s.OrdenCompra.Proveedor.Datos,
//                 Estado = "LABORATORIO",
//                 OrdenCompra = "OC01-" + s.OrdenCompra.Id,
//                 Usuario = s.Creador.Persona.NombreCompleto,
//                 Familia = s.OrdenCompra.Familia.Nombre,
//             }).ToListAsync();

//             return lista;
//         }
//         [HttpPatch]
//         public async Task<ActionResult> EditarDetalleCompra()
//         {

//         }
//         [HttpPatch("{id}")]
//         public async Task<IActionResult> ActualizarDetalleCompra(int id, PatchDetalleCompraReq request)
//         {
//             DetalleCompra? detalleCompra = await _context.DetalleCompras.FindAsync(id);
//             if (detalleCompra == null)
//                 return NotFound(new { message = "Detalle de compra no encontrado" });

//             // Actualización parcial (solo si vienen valores)
//             if (request.Coa != null)
//                 detalleCompra.Coa = request.Coa;

//             if (request.Lote != null)
//                 detalleCompra.Lote = request.Lote;

//             if (request.Cantidad.HasValue)
//                 detalleCompra.CantidadPaquete = request.Cantidad.Value;

//             if (request.PotenciaPorcentaje != null)
//                 detalleCompra.Potencia = request.PotenciaPorcentaje;

//             if (request.FechaFabricacion.HasValue)
//                 detalleCompra.FechaFab = request.FechaFabricacion.Value;

//             if (request.FechaVencimiento.HasValue)
//                 detalleCompra.FechaVec = request.FechaVencimiento.Value;

//             if (request.CondicionAlmacenamiento != null)
//                 detalleCompra.CondicionAlmacenamiento = request.CondicionAlmacenamiento;

//             try
//             {
//                 await _context.SaveChangesAsync();
//                 return Ok(new { message = "Detalle de compra actualizado correctamente" });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { message = "Error al actualizar", error = ex.Message });
//             }

//             return Ok();
//         }
//     }
// }
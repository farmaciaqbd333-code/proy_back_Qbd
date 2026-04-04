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

            List<ListadoOrdenCompra> ordenes = await _context.OrdenCompras
                .Select(s => new ListadoOrdenCompra
                {
                    CUO = "BDOC-" + s.IdOrdenCompra,
                    Fecha = s.FechaCreacion.ToString("d"),
                    RUC = s.Proveedor == null || s.Proveedor.CodigoProv == null ? "" : s.Proveedor.CodigoProv,
                    Denominacion = s.Proveedor == null || s.Proveedor.Datos == null ? "" : s.Proveedor.Datos,
                    Valor = s.DetalleOrdenCompras == null ? "" : (s.DetalleOrdenCompras.Sum(sm => sm.Cantidad) * s.DetalleOrdenCompras.Sum(sm => sm.CostoUnitario)).ToString(),
                    Total = s.DetalleOrdenCompras == null ? "" : s.DetalleOrdenCompras.Sum(sm => sm.CostoTotal).ToString(),
                    Moneda = s.Moneda,
                    Estado = s.Compra != null ? "PRO" : "PEN",
                    CodFac = s.Compra != null ? s.Compra.CodFac : "",
                    Familia = s.Familia,
                    Factura = s.Compra != null ? s.Compra.Factura : "",
                    EstadoOrdenCompra = s.Estado,
                    Usuario = s.Usuario == null || s.Usuario.Persona == null ? "" : s.Usuario.Persona.NombreCompleto ?? ""
                })
                .ToListAsync();

            return Ok(ordenes);
        }
        [HttpGet("detalle/{id}")]
        public async Task<ActionResult<DetalleOrdenCompraRes>> GetDetalleOrdenesCompra(int id)
        {

            DetalleOrdenCompraRes? orden = await _context.OrdenCompras
                .Where(w => w.IdOrdenCompra == id)
                .Select(s => new DetalleOrdenCompraRes
                {
                    Modalidad = s.TipoCambio.ToString(),
                    TC = s.TipoCambio.ToString(),
                    Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                    Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                    DetalleOrdenCompras = s.DetalleOrdenCompras == null
                                            ? null :
                                            s.DetalleOrdenCompras.Select(s2 => new DetalleOrdenCompra2
                                            {
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
                IdUsuario = request.IdCreador,
                FechaEmision = request.FechaEmision,
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
                IdCreador = request.IdCreador

            }).ToList();
            
            _context.DetalleOrdenesCompras.AddRange(detalleOrdenCompras);
            await _context.SaveChangesAsync();

            ordenCompra.DetalleOrdenCompras = detalleOrdenCompras;
            return Ok(ordenCompra);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Kardex.Stock
{
    /// <summary>Stock de la lista principal del Kardex para la familia PT (productos terminados).</summary>
    public class ProductoTerminadoStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<ProductoTerminadoStockStrategy> _logger;

        public ProductoTerminadoStockStrategy(ApiContext context, ILogger<ProductoTerminadoStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "PT";

        public async Task<List<StockRes>> ObtenerStock(int idSede)
        {
            return await _context.Productos.AsNoTracking()
                .GroupBy(g => new { g.Id })
                .Select(s => new StockRes()
                {
                    Codigo = s.Key.Id + "",
                    Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                    Um = "UND",
                    Entradas =
                        s.Sum(x => x.CompraProductos!.Where(w => w.Compra.IdSede == idSede).Sum(ci => ci.CantidadRecibida ?? ci.CantidadSolicitada)) +
                        s.Sum(x => x.CompraProductos.Sum(x2 => x2.NotaSalidaProductos.Where(w => w.NotaSalida != null && w.NotaSalida.IdSedeDestino == idSede && (w.NotaSalida.Estado == "RECIBIDO" || w.NotaSalida.Estado == "RECEPCIONADO" || w.NotaSalida.FechaRecepcion != null || w.CantidadRecibida > 0)).Sum(x3 => x3.CantidadRecibida > 0 ? x3.CantidadRecibida : x3.Cantidad))),
                    Salidas =
                        s.Sum(x => x.CompraProductos.Sum(s2 => s2.NotaSalidaProductos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.Cantidad))),
                    Ajustes =
                        s.Sum(x => x.CompraProductos.Where(w => w.Compra.IdSede == idSede).Sum(cp => cp.StockProductoTerminados.Sum(sp => sp.AjusteProductos.Sum(a => a.Ajuste)))),
                    Baja = s.Sum(x => x.CompraProductos
                        .Where(cp => cp.FechaVencimiento < DateTime.UtcNow)
                        .Sum(cp => cp.StockProductoTerminados.Where(w => w.IdSede == idSede).Sum(sp => sp.StockDisponible))),
                    Tipo = null,
                    CodigoUbicacion = s.Select(s => s.ProductoSedes.Where(w => w.IdSite == idSede).Select(s => s.Location).FirstOrDefault()).FirstOrDefault(),
                    Limite = s.Select(s => s.ProductoSedes.Where(w => w.IdSite == idSede).Select(s => s.Limite).FirstOrDefault()).FirstOrDefault()
                }).ToListAsync();
        }
    }
}

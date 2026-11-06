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
    /// <summary>Stock de la lista principal del Kardex para la familia ME (material de empaque).</summary>
    public class MaterialEmpaqueStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<MaterialEmpaqueStockStrategy> _logger;

        public MaterialEmpaqueStockStrategy(ApiContext context, ILogger<MaterialEmpaqueStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "ME";

        public async Task<List<StockRes>> ObtenerStock(int idSede)
        {
            var salidasLaboratorioPorEmpaque = await _context.Laboratorios
                .AsNoTracking()
                .Where(l => l.SedeId == idSede && l.EmpaqueId != null)
                .GroupBy(l => l.EmpaqueId!.Value)
                .Select(g => new { EmpaqueId = g.Key, Total = (decimal)g.Count() })
                .ToDictionaryAsync(g => g.EmpaqueId, g => g.Total);

            var lista = await _context.Empaques.AsNoTracking()
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = "UND",
                            Entradas =
                            //Suma de cantidad recibida de compraEmpaque
                            s.Sum(x => x.CompraEmpaques!.Where(w => w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)).Sum(ci => (ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0 ? ci.CantidadRecibida.Value : ci.CantidadSolicitada))) +
                            //Suma de cantidad recibida de nota de salidas
                            s.Sum(x => x.CompraEmpaques.Sum(x2 => x2.NotaSalidaEmpaques.Where(w => w.NotaSalida != null && w.NotaSalida.IdSedeDestino == idSede && (w.NotaSalida.Estado == "RECIBIDO" || w.NotaSalida.Estado == "RECEPCIONADO" || w.NotaSalida.FechaRecepcion != null || w.CantidadRecibida > 0)).Sum(x3 => x3.CantidadRecibida > 0 ? x3.CantidadRecibida : x3.Cantidad))),
                            // adjuntar en pi, fm y nota de salida
                            Salidas =
                            //Suma de Productos Intermedios
                            s.Sum(s => s.EmpaqueProductoIntermedios.Count(w => w.ProductoIntermedio.IdSede == idSede)) +
                            //Suma de Notas de Salida
                            s.Sum(s => s.CompraEmpaques.Sum(s2 => s2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.Cantidad))),
                            Ajustes =
                            //Suma de Ajustes hechas a compra empaques
                             s.Sum(s => s.CompraEmpaques.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEmpaques.Sum(s => s.AjusteEmpaques.Sum(s => s.Ajuste)))),
                            //Suma de Empaques vencidos
                            Baja = s.Sum(x => x.CompraEmpaques
                                    .Where(ce => ce.FechaVencimiento < DateTimeOffset.UtcNow)
                                    .Sum(ce => ce.StockEmpaques.Where(w => w.IdSede == idSede).Sum(sm => sm.StockDisponible))),
                            Tipo = null,
                            CodigoUbicacion = s.Select(s => s.EmpaqueSedes.Where(w => w.IdSite == idSede).Select(s => s.Location).FirstOrDefault()).FirstOrDefault() ?? s.Select(x => x.CodigoUbicacion).FirstOrDefault(),
                            Limite = s.Select(s => s.EmpaqueSedes.Where(w => w.IdSite == idSede).Select(s => s.Limite).FirstOrDefault()).FirstOrDefault()
                        }).ToListAsync();

            foreach (var item in lista)
            {
                if (int.TryParse(item.Codigo, out int empId) && salidasLaboratorioPorEmpaque.TryGetValue(empId, out decimal cantFM))
                {
                    item.Salidas = (item.Salidas ?? 0m) + cantFM;
                    item.Stock = (item.Entradas ?? 0m) - (item.Salidas ?? 0m) + (item.Ajustes ?? 0m) - (item.Baja ?? 0m);
                    item.Saldo = item.Stock;
                }
            }

            return lista;
        }
    }
}

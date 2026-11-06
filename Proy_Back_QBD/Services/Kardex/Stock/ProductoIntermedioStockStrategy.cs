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
    /// <summary>Stock de la lista principal del Kardex para la familia PI (productos intermedios).</summary>
    public class ProductoIntermedioStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<ProductoIntermedioStockStrategy> _logger;

        public ProductoIntermedioStockStrategy(ApiContext context, ILogger<ProductoIntermedioStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "PI";

        public async Task<List<StockRes>> ObtenerStock(int idSede)
        {
            var insumos = await _context.Insumos.AsNoTracking()
                .Where(i => i.Clasificacion == "PI")
                .ToListAsync();

            var elaboraciones = await _context.ProductosIntermedios.AsNoTracking()
                .Where(w => w.IdSede == idSede)
                .ToListAsync();

            var allPIs = await _context.ProductosIntermedios.AsNoTracking()
                .Select(p => new { p.Id, p.IdInsumo, p.Lote, p.FechaEmision, p.FechaVencimiento, p.FechaCreacion })
                .ToListAsync();

            var piByLote = allPIs
                .Where(p => !string.IsNullOrEmpty(p.Lote))
                .GroupBy(p => p.Lote!.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.First());

            var notasSalidaDestino = await _context.NotaSalidaInsumos.AsNoTracking()
                .Include(nsi => nsi.NotaSalida)
                .Where(nsi => !nsi.IdCompraInsumo.HasValue &&
                              nsi.NotaSalida != null &&
                              nsi.NotaSalida.IdSedeDestino == idSede &&
                              (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                .ToListAsync();

            var notasSalidaOrigen = await _context.NotaSalidaInsumos.AsNoTracking()
                .Include(nsi => nsi.NotaSalida)
                .Where(nsi => !nsi.IdCompraInsumo.HasValue &&
                              nsi.NotaSalida != null &&
                              nsi.NotaSalida.IdSedeOrigen == idSede)
                .ToListAsync();

            var salidasConsumoPI = await _context.InsumoProductoIntermedios.AsNoTracking()
                .Include(ipi => ipi.ProductoIntermedio)
                .Where(ipi => ipi.ProductoIntermedio != null && ipi.ProductoIntermedio.IdSede == idSede)
                .GroupBy(ipi => ipi.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(s3 => (s3.Practica.HasValue && s3.Practica.Value > 0) ? s3.Practica.Value : s3.CantidadLote)
                })
                .ToListAsync();

            var salidasConsumoFormula = await _context.FormulasCC.AsNoTracking()
                .Where(f => f.SedeId == idSede)
                .GroupBy(f => f.InsumoId)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(s => s.CantidadL)
                })
                .ToListAsync();

            var ubicaciones = await _context.SiteSupplies.AsNoTracking()
                .Where(w => w.IdSite == idSede)
                .ToListAsync();

            var insumoIds = insumos.Select(i => i.Id).ToList();

            var ajustesPI = await _context.AjusteInsumos
                .AsNoTracking()
                .Where(a => a.StockInsumo != null &&
                            (idSede == 0 || a.StockInsumo.IdSede == idSede) &&
                            a.StockInsumo.ProductoIntermedio != null &&
                            insumoIds.Contains(a.StockInsumo.ProductoIntermedio.IdInsumo))
                .GroupBy(a => a.StockInsumo!.ProductoIntermedio!.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(a => a.Ajuste)
                })
                .ToListAsync();

            var ajustesCompraPI = await _context.AjusteInsumos
                .AsNoTracking()
                .Where(a => a.StockInsumo != null &&
                            (idSede == 0 || a.StockInsumo.IdSede == idSede) &&
                            a.StockInsumo.CompraInsumo != null &&
                            insumoIds.Contains(a.StockInsumo.CompraInsumo.IdInsumo))
                .GroupBy(a => a.StockInsumo!.CompraInsumo!.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(a => a.Ajuste)
                })
                .ToListAsync();

            var ahora = DateTime.UtcNow;

            var bajasPI = await _context.ProductosIntermedios
                .AsNoTracking()
                .Where(pi =>
                    insumoIds.Contains(pi.IdInsumo) &&
                    (idSede == 0 || pi.IdSede == idSede) &&
                    pi.FechaVencimiento < ahora &&
                    pi.StockInsumo != null &&
                    pi.StockInsumo.StockDisponible > 0)
                .GroupBy(pi => pi.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(pi => pi.StockInsumo!.StockDisponible)
                })
                .ToListAsync();

            var bajasCompraPI = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.FechaVencimiento < ahora &&
                    ci.StockInsumos.Any(si => (idSede == 0 || si.IdSede == idSede) && si.StockDisponible > 0))
                .SelectMany(ci => ci.StockInsumos
                    .Where(si => (idSede == 0 || si.IdSede == idSede) && si.StockDisponible > 0)
                    .Select(si => new
                    {
                        ci.IdInsumo,
                        si.StockDisponible
                    }))
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.StockDisponible)
                })
                .ToListAsync();

            var bajasNS = await _context.StockInsumos
                .AsNoTracking()
                .Include(si => si.NotaSalidaInsumo)
                .Where(si =>
                    si.Tipo == "PI" &&
                    (idSede == 0 || si.IdSede == idSede) &&
                    si.StockDisponible > 0 &&
                    si.IdNotaSalidaInsumo.HasValue &&
                    si.NotaSalidaInsumo != null &&
                    !string.IsNullOrEmpty(si.NotaSalidaInsumo.Lote))
                .ToListAsync();

            var bajasNSTotales = new Dictionary<int, decimal>();
            if (bajasNS.Any())
            {
                foreach (var sns in bajasNS)
                {
                    var loteKey = (sns.NotaSalidaInsumo?.Lote ?? "").Trim().ToUpper();
                    if (piByLote.TryGetValue(loteKey, out var piMatch))
                    {
                        if (piMatch.FechaVencimiento.HasValue && piMatch.FechaVencimiento.Value < ahora)
                        {
                            if (!bajasNSTotales.ContainsKey(piMatch.IdInsumo))
                                bajasNSTotales[piMatch.IdInsumo] = 0m;
                            bajasNSTotales[piMatch.IdInsumo] += sns.StockDisponible;
                        }
                    }
                }
            }

            var resultado = new List<StockRes>();

            foreach (var insumo in insumos)
            {
                decimal entradasElab = elaboraciones
                    .Where(w => w.IdInsumo == insumo.Id)
                    .Sum(s2 => (s2.TipoUso == "PI-FMG" || s2.Um == "UND")
                        ? (s2.LoteEstandar ?? 0)
                        : ((s2.LoteEstTotal.HasValue && s2.LoteEstTotal.Value > 0)
                            ? s2.LoteEstTotal.Value
                            : (s2.LoteEstandar ?? 0)));

                decimal entradasNS = 0m;
                foreach (var nsi in notasSalidaDestino)
                {
                    int idInsumoItem = 0;
                    if (!string.IsNullOrEmpty(nsi.Lote))
                    {
                        var key = nsi.Lote.Trim().ToUpper();
                        if (piByLote.TryGetValue(key, out var foundPi))
                        {
                            idInsumoItem = foundPi.IdInsumo;
                        }
                    }
                    if (idInsumoItem == insumo.Id)
                    {
                        decimal cant = (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad;
                        string um = (nsi.Um ?? "G").Trim().ToUpper();
                        if (um == "KG" || um == "KILOGRAMOS" || um == "Kg") cant *= 1000m;
                        entradasNS += cant;
                    }
                }

                decimal salidasNS = 0m;
                foreach (var nsi in notasSalidaOrigen)
                {
                    int idInsumoItem = 0;
                    if (!string.IsNullOrEmpty(nsi.Lote))
                    {
                        var key = nsi.Lote.Trim().ToUpper();
                        if (piByLote.TryGetValue(key, out var foundPi))
                        {
                            idInsumoItem = foundPi.IdInsumo;
                        }
                    }
                    if (idInsumoItem == insumo.Id)
                    {
                        decimal cant = nsi.Cantidad;
                        string um = (nsi.Um ?? "G").Trim().ToUpper();
                        if (um == "KG" || um == "KILOGRAMOS" || um == "Kg") cant *= 1000m;
                        salidasNS += cant;
                    }
                }

                decimal salidasConsumo = (salidasConsumoPI.FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m)
                                       + (salidasConsumoFormula.FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m);

                decimal ajusteTotal = (ajustesPI.FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m)
                                    + (ajustesCompraPI.FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m);

                decimal bajaTotal = (bajasPI.FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m)
                                  + (bajasCompraPI.FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m)
                                  + (bajasNSTotales.ContainsKey(insumo.Id) ? bajasNSTotales[insumo.Id] : 0m);

                decimal entradasTotal = entradasElab + entradasNS;
                decimal salidasTotal = salidasConsumo + salidasNS;
                decimal saldo = entradasTotal - salidasTotal + ajusteTotal - bajaTotal;
                if (saldo < 0) saldo = 0;

                var siteSupply = ubicaciones.FirstOrDefault(w => w.IdSupply == insumo.Id);

                resultado.Add(new StockRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(insumo.Id),
                    Descripcion = insumo.Descripcion,
                    Um = insumo.UnidadMedida ?? "G",
                    Entradas = entradasTotal,
                    Salidas = salidasTotal,
                    Ajustes = ajusteTotal,
                    Baja = bajaTotal,
                    Stock = saldo,
                    Saldo = saldo,
                    Tipo = insumo.Tipo,
                    CodigoUbicacion = siteSupply?.Location,
                    Limite = siteSupply?.Limite
                });
            }

            return resultado;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class KardexService : IKardexService
    {
        private readonly ApiContext _context;
        private readonly ILogger<KardexService> _logger;
        private static readonly List<string> FamiliasAptas = ["MP", "ME"];
        
        public KardexService(ApiContext context, ILogger<KardexService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int idInsumo, int idSede)
        {
            var resultado = new List<DetalleInsumoRes>();

            resultado = await _context.CompraInsumos
                .Include(w => w.Compra)
                .Include(w => w.StockInsumos)
                .Include(w => w.PaqueteInsumos!)
                .ThenInclude(p => p.Paquete)
                .Where(w => w.IdInsumo == idInsumo)
                .Select(s => new DetalleInsumoRes
                {
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = s.StockInsumos.Any(w => w.IdSede == idSede)
                        ? s.StockInsumos.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible)
                        : (s.StockInsumos.Any()
                            ? s.StockInsumos.Sum(s2 => s2.StockDisponible)
                            : s.PaqueteInsumos.Sum(p => p.Paquete != null ? p.Paquete.CantidadPaquete * p.Paquete.PesoUnitario : 0)),
                    FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.Observacion
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId, int idSede)
        {
            var resultado = new List<DetalleEmpaqueRes>();
            resultado = await _context.CompraEmpaques
                .Include(w => w.Compra)
                .Include(w => w.StockEmpaques)
                .Include(w => w.PaqueteEmpaques!)
                .ThenInclude(p => p.Paquete)
                .Where(w => w.IdEmpaque == empaqueId)
                .Select(s => new DetalleEmpaqueRes
                {
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = s.StockEmpaques.Any(w => w.IdSede == idSede)
                        ? s.StockEmpaques.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible)
                        : (s.StockEmpaques.Any()
                            ? s.StockEmpaques.Sum(s2 => s2.StockDisponible)
                            : s.PaqueteEmpaques.Sum(p => p.Paquete != null ? p.Paquete.CantidadPaquete * p.Paquete.PesoUnitario : 0)),
                    FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.Observacion
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<List<StockRes>> StockListaPrincipal(string familia, int idSede)
        {
            List<StockRes> responseMP = familia switch
            {
                "MP" => await ObtenerMateriaPrima(idSede),
                "PI" => await ObtenerProductosIntermedios(idSede),
                "ME" => await ObtenerMateriaEmpaque(idSede),
                "ECO" => await ObtenerEconomato(idSede),
                "PT" => await ObtenerProductosIntermedios(idSede),
                _ => throw new BadRequestException("FAMILIA NO VALIDA")

            };

            return responseMP;
        }

        public async Task<List<ComprasVencidasRes>> ObtenerComprasVencidas(string familia, int idSede)
        {
            List<ComprasVencidasRes> response = new();
            if (FamiliasAptas.Contains(familia))
            {
                if (familia == "MP")
                {
                    response = await _context.CompraInsumos.Select(s => new ComprasVencidasRes()
                    {
                        Registro = Alfanumerico.ConvertToBase36(s.Id),
                        Codigo = s.Insumo.Id.ToString("d4"),
                        Descripcion = s.Insumo.Descripcion,
                        Estado = (DateTime.UtcNow > s.FechaVencimiento) ? "VENCIDO" : s.FechaVencimiento <= DateTime.UtcNow.AddDays(7) ? "POR VENCER" : "VIGENTE",
                        Lote = s.Lote,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Saldo = s.StockInsumos.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible),
                        Cantidad = s.PaqueteInsumos.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario)

                    }).ToListAsync();
                }
                if (familia == "ME")
                {
                    response = await _context.CompraEmpaques.Select(s => new ComprasVencidasRes()
                    {
                        Registro = Alfanumerico.ConvertToBase36(s.Id),
                        Codigo = s.Empaque.Id.ToString("d4"),
                        Descripcion = s.Empaque.Descripcion,
                        Estado = (DateTime.UtcNow > s.FechaVencimiento) ? "VENCIDO" : s.FechaVencimiento <= DateTime.UtcNow.AddDays(7) ? "POR VENCER" : "VIGENTE",
                        Lote = s.Lote,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Saldo = s.StockEmpaques.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible),
                        Cantidad = s.PaqueteEmpaques.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario)
                    }).ToListAsync();
                }
                return response;
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }

        }
        // Fusionar 2 listas para entradas compras y notas de salida
        private async Task<List<StockRes>> ObtenerMateriaPrima(int idSede)
        {
            _logger.LogDebug("--- Iniciando ObtenerMateriaPrima para Sede {Sede} ---", idSede);

            // 1. Obtener los insumos con sus relaciones en memoria para poder debuggear cada paso
            var insumosDb = await _context.Insumos
                .Include(i => i.CompraInsumos)
                    .ThenInclude(ci => ci.Compra)
                .Include(i => i.CompraInsumos)
                    .ThenInclude(ci => ci.NotaSalidaInsumos)
                        .ThenInclude(nsi => nsi.NotaSalida)
                .Include(i => i.CompraInsumos)
                    .ThenInclude(ci => ci.StockInsumos)
                        .ThenInclude(si => si.AjusteInsumos)
                .Include(i => i.InsumoProductoIntermedio)
                    .ThenInclude(ipi => ipi.ProductoIntermedio)
                .Include(i => i.FormulasCC)
                    .ThenInclude(fcc => fcc.Formula)
                .Where(i => (i.Clasificacion == "MP" || i.Clasificacion == null) 
                    && i.CompraInsumos.Any(s => s.Compra != null && s.Compra.IdSede == idSede))
                .ToListAsync();

            var resultados = new List<StockRes>();

            foreach (var insumo in insumosDb)
            {
                _logger.LogDebug("--- Procesando Insumo ID: {Id}, Descripcion: {Desc} ---", insumo.Id, insumo.Descripcion);

                // ENTRADAS
                decimal? entradasCompras = insumo.CompraInsumos
                    .Where(w => w.Compra != null && w.Compra.IdSede == idSede)
                    .Sum(ci => (decimal?)ci.CantidadRecibida);
                
                decimal? entradasNotasSalida = insumo.CompraInsumos
                    .Sum(ci => ci.NotaSalidaInsumos
                        .Where(w => w.NotaSalida != null && w.NotaSalida.IdSedeDestino == idSede)
                        .Sum(nsi => (decimal?)nsi.CantidadRecibida));
                
                decimal totalEntradas = (entradasCompras ?? 0m) + (entradasNotasSalida ?? 0m);
                _logger.LogDebug("Entradas: Compras = {E1}, NotasSalida = {E2}, TotalEntradas = {Total}", entradasCompras, entradasNotasSalida, totalEntradas);

                // SALIDAS
                decimal salidasProdIntermedio = insumo.InsumoProductoIntermedio
                    .Where(w => w.ProductoIntermedio != null && w.ProductoIntermedio.IdSede == idSede)
                    .Sum(ipi => ipi.CantidadLote);
                
                decimal salidasFormulas = insumo.FormulasCC
                    .Where(w => w.Formula != null && w.Formula.SedeId == idSede)
                    .Sum(fcc => fcc.CantidadL);
                
                decimal? salidasNotasSalida = insumo.CompraInsumos
                    .Sum(ci => ci.NotaSalidaInsumos
                        .Where(w => w.NotaSalida != null && w.NotaSalida.IdSedeOrigen == idSede)
                        .Sum(nsi => nsi.CantidadRecibida));
                
                decimal totalSalidas = salidasProdIntermedio + salidasFormulas + (salidasNotasSalida ?? 0m);
                _logger.LogDebug("Salidas: ProdInter = {S1}, Formulas = {S2}, NotasSalida = {S3}, TotalSalidas = {Total}", salidasProdIntermedio, salidasFormulas, salidasNotasSalida, totalSalidas);

                // AJUSTES
                decimal? ajustes = insumo.CompraInsumos
                    .Sum(ci => ci.StockInsumos
                        .Sum(si => si.AjusteInsumos
                            .Sum(ai => ai.Ajuste)));
                decimal totalAjustes = ajustes ?? 0m;
                _logger.LogDebug("Ajustes: Total = {Ajustes}", totalAjustes);

                // BAJAS
                decimal? bajas = insumo.CompraInsumos
                    .Where(ci => ci.FechaVencimiento < DateTime.UtcNow)
                    .Sum(ci => ci.StockInsumos
                        .Where(si => si.IdSede == idSede)
                        .Sum(si => si.StockDisponible));
                decimal totalBajas = bajas ?? 0m;
                _logger.LogDebug("Bajas (Vencidos): Total = {Bajas}", totalBajas);

                resultados.Add(new StockRes
                {
                    Codigo = insumo.Id.ToString(),
                    Descripcion = insumo.Descripcion ?? "",
                    Um = insumo.UnidadMedida ?? string.Empty,
                    Entradas = totalEntradas,
                    Salidas = totalSalidas,
                    Ajustes = totalAjustes,
                    Baja = totalBajas,
                    Tipo = insumo.Tipo,
                    CodigoUbicacion = insumo.CodigoUbicacion
                });
            }

            _logger.LogDebug("--- Fin de ObtenerMateriaPrima, retornando {Count} registros ---", resultados.Count);
            return resultados;
        }
        private async Task<List<StockRes>> ObtenerProductosIntermedios(int idSede)
        {
            return await _context.Insumos
            .Where(i => i.Clasificacion == "PI" && i.ProductoIntermedio.FirstOrDefault(s => s.IdSede == idSede) != null)
            .GroupBy(g => new { g.Id })
            .Select(s => new StockRes()
            {
                Codigo = UtilFamilia.CodigoInsumo(s.Key.Id),
                Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                Um = s.Select(x => x.UnidadMedida).FirstOrDefault() ?? string.Empty,
                Entradas = s.Sum(s => s!.ProductoIntermedio!
                    .Where(w => w.IdSede == idSede)
                    .Sum(s2 => (s2.PesoUnidad.HasValue && s2.PesoUnidad.Value > 0) 
                        ? s2.PesoUnidad.Value 
                        : ((s2.LoteEstTotal.HasValue && s2.LoteEstTotal.Value > 0) 
                            ? s2.LoteEstTotal.Value 
                            : (s2.LoteEstandar ?? 0)))),
                Salidas = 0,
                Ajustes = 0,
                Baja = s.Sum(x => x.ProductoIntermedio!
                    .Where(ci => ci.FechaVencimiento < DateTime.UtcNow && ci.IdSede == idSede)
                    .Sum(s2 => (s2.PesoUnidad.HasValue && s2.PesoUnidad.Value > 0) 
                        ? s2.PesoUnidad.Value 
                        : ((s2.LoteEstTotal.HasValue && s2.LoteEstTotal.Value > 0) 
                            ? s2.LoteEstTotal.Value 
                            : (s2.LoteEstandar ?? 0)))),
                Tipo = s.Select(x => x.Tipo).FirstOrDefault(),
                CodigoUbicacion = s.Select(x => x.CodigoUbicacion).FirstOrDefault()
            }).ToListAsync();
        }
        private async Task<List<StockRes>> ObtenerMateriaEmpaque(int idSede)
        {
            return await _context.Empaques
                        .Where(i => i.CompraEmpaques.FirstOrDefault(s => s.Compra.IdSede == idSede) != null)
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = "UND",
                            Entradas =
                            //Suma de cantidad recibida de compraEmpaque
                            s.Sum(x => x.CompraEmpaques!.Where(w => w.Compra.IdSede == idSede).Sum(ci => ci.CantidadRecibida)) +
                            //Suma de cantidad recibida de nota de salidas
                            s.Sum(x => x.CompraEmpaques.Sum(x2 => x2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeDestino == idSede).Sum(x3 => x3.CantidadRecibida))),
                            // adjuntar en pi, fm y nota de salida
                            Salidas =
                            //Suma de Productos Intermedios
                            s.Sum(s => s.EmpaqueProductoIntermedios.Count(w => w.ProductoIntermedio.IdSede == idSede)) +
                            //Suma de Laboratorios
                            s.Sum(s => s.Laboratorios.Count(w => w.SedeId == idSede)) +
                            //Suma de Notas de Salida
                            s.Sum(s => s.CompraEmpaques.Sum(s2 => s2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.CantidadRecibida))),
                            Ajustes =
                            //Suma de Ajustes hechas a compra empaques
                             s.Sum(s => s.CompraEmpaques.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEmpaques.Sum(s => s.AjusteEmpaques.Sum(s => s.Ajuste)))),
                            //Suma de Empaques vencidos
                            Baja = s.Sum(x => x.CompraEmpaques
                                    .Where(ce => ce.FechaVencimiento < DateTimeOffset.UtcNow)
                                    .Sum(ce => ce.StockEmpaques.Where(w => w.IdSede == idSede).Sum(sm => sm.StockDisponible))),
                            Tipo = null,
                            CodigoUbicacion = s.Select(x => x.CodigoUbicacion).FirstOrDefault()
                        }).ToListAsync();
        }
        private async Task<List<StockRes>> ObtenerEconomato(int idSede)
        {
            return await _context.Economatos
                        .Where(i => i.CompraEconomatos.FirstOrDefault(s => s.Compra.IdSede == idSede) != null)
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = s.Select(s => s.UnidadMedida).FirstOrDefault() ?? "Und",
                            Entradas = s.Sum(s => s.CompraEconomatos.Sum(ce => ce.CantidadSolicitada)),
                            //nota de salida
                            Salidas = 0,
                            Ajustes = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEconomatos.Sum(s => s.AjusteEconomatos.Sum(s => s.Ajuste)))),
                            Baja = 0
                        }).ToListAsync();
        }
    }


}
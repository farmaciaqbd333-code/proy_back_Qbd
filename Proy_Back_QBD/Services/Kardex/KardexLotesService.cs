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

namespace proy_back_Qbd.Services.Kardex
{
    /// <summary>Resuelve qué lotes se usan para los insumos de una fórmula.</summary>
    public class KardexLotesService : IKardexLotesService
    {
        private readonly ApiContext _context;
        private readonly IKardexDetalleService _detalle;
        private readonly ILogger<KardexLotesService> _logger;

        public KardexLotesService(ApiContext context, IKardexDetalleService detalle, ILogger<KardexLotesService> logger)
        {
            _context = context;
            _detalle = detalle;
            _logger = logger;
        }

        public async Task<List<LoteAsignadoRes>> ResolverLotesFormula(ResolverLotesFormulaReq request)
        {
            var resultado = new List<LoteAsignadoRes>();
            if (request == null || request.Insumos == null || !request.Insumos.Any())
            {
                return resultado;
            }

            int idSede = request.IdSede;
            var hoy = DateTime.UtcNow.Date;

            // Cache de lotes por IdInsumo para evitar consultas duplicadas
            var lotesPorInsumo = new Dictionary<int, (List<DetalleInsumoRes> lotes, string tipo)>();
            // Rastreo de saldo consumido por lote/registro para esta fórmula
            var saldoConsumidoPorRegistro = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in request.Insumos)
            {
                var resItem = new LoteAsignadoRes
                {
                    Index = item.Index,
                    IdInsumo = item.IdInsumo,
                    Codigo = item.Codigo,
                    Descripcion = item.Descripcion,
                    Registro = "---",
                    Lote = "---",
                    StockDisponible = 0m,
                    TipoInsumo = item.Tipo ?? "MP"
                };

                if (item.IdInsumo <= 0)
                {
                    resultado.Add(resItem);
                    continue;
                }

                // 1. Obtener lotes si no están en caché
                if (!lotesPorInsumo.ContainsKey(item.IdInsumo))
                {
                    string tipoDetectado = item.Tipo ?? "MP";
                    List<DetalleInsumoRes> listaLotes = new();

                    try
                    {
                        if (tipoDetectado.StartsWith("PI", StringComparison.OrdinalIgnoreCase))
                        {
                            listaLotes = await _detalle.ObtenerDetallePI(item.IdInsumo, idSede);
                            if (listaLotes == null || !listaLotes.Any(l => (l.StockDisponible ?? l.Saldo ?? 0) > 0))
                            {
                                var fallbackMp = await _detalle.ObtenerDetalleInsumo(item.IdInsumo, idSede);
                                if (fallbackMp != null && fallbackMp.Any(l => (l.StockDisponible ?? l.Saldo ?? 0) > 0))
                                {
                                    listaLotes = fallbackMp;
                                    tipoDetectado = "MP";
                                }
                            }
                        }
                        else
                        {
                            listaLotes = await _detalle.ObtenerDetalleInsumo(item.IdInsumo, idSede);
                            if (listaLotes == null || !listaLotes.Any(l => (l.StockDisponible ?? l.Saldo ?? 0) > 0))
                            {
                                var fallbackPi = await _detalle.ObtenerDetallePI(item.IdInsumo, idSede);
                                if (fallbackPi != null && fallbackPi.Any(l => (l.StockDisponible ?? l.Saldo ?? 0) > 0))
                                {
                                    listaLotes = fallbackPi;
                                    tipoDetectado = "PI";
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error obteniendo detalle para insumo {IdInsumo}", item.IdInsumo);
                    }

                    lotesPorInsumo[item.IdInsumo] = (listaLotes ?? new List<DetalleInsumoRes>(), tipoDetectado);
                }

                var (rawLotes, tipoFinal) = lotesPorInsumo[item.IdInsumo];
                resItem.TipoInsumo = tipoFinal;

                // 2. Filtrar únicamente lotes con stock > 0 y NO vencidos
                var conStock = rawLotes.Where(d =>
                {
                    decimal stock = d.StockDisponible ?? d.Saldo ?? 0m;
                    if (stock <= 0m) return false;
                    if (d.FechaVencimiento.HasValue && d.FechaVencimiento.Value.UtcDateTime.Date < hoy) return false;
                    return true;
                }).ToList();

                // 3. Ordenamiento FIFO puro
                conStock.Sort((a, b) =>
                {
                    DateTime fcA = a.FechaCompra ?? (a.FechaFabricacion.HasValue ? a.FechaFabricacion.Value.UtcDateTime : DateTime.MaxValue);
                    DateTime fcB = b.FechaCompra ?? (b.FechaFabricacion.HasValue ? b.FechaFabricacion.Value.UtcDateTime : DateTime.MaxValue);
                    if (fcA != fcB) return fcA.CompareTo(fcB);

                    DateTime fvA = a.FechaVencimiento.HasValue ? a.FechaVencimiento.Value.UtcDateTime : DateTime.MaxValue;
                    DateTime fvB = b.FechaVencimiento.HasValue ? b.FechaVencimiento.Value.UtcDateTime : DateTime.MaxValue;
                    if (fvA != fvB) return fvA.CompareTo(fvB);

                    int idA = Alfanumerico.ConvertFromBase36(a.Registro?.Replace("MP", "").Replace("PI", "").Replace("ME", "").Replace("PT", "").Replace("ECO", "").Trim() ?? "");
                    int idB = Alfanumerico.ConvertFromBase36(b.Registro?.Replace("MP", "").Replace("PI", "").Replace("ME", "").Replace("PT", "").Replace("ECO", "").Trim() ?? "");
                    return idA.CompareTo(idB);
                });

                resItem.LotesDisponibles = conStock;

                // 4. Buscar el primer lote que aún tenga saldo disponible
                DetalleInsumoRes loteAsignado = null;
                decimal saldoRestanteLote = 0m;

                foreach (var l in conStock)
                {
                    string regKey = (l.Registro ?? "").Trim();
                    decimal totalLote = l.StockDisponible ?? l.Saldo ?? 0m;
                    decimal yaConsumido = saldoConsumidoPorRegistro.ContainsKey(regKey) ? saldoConsumidoPorRegistro[regKey] : 0m;
                    decimal disponible = totalLote - yaConsumido;

                    if (disponible > 0m)
                    {
                        loteAsignado = l;
                        saldoRestanteLote = disponible;
                        break;
                    }
                }

                if (loteAsignado != null)
                {
                    string regKey = (loteAsignado.Registro ?? "").Trim();
                    resItem.Registro = regKey;
                    resItem.Lote = loteAsignado.Lote ?? "";
                    resItem.StockDisponible = saldoRestanteLote;

                    decimal cantConsumir = item.CantidadRequerida > 0 ? item.CantidadRequerida : 0m;
                    if (!saldoConsumidoPorRegistro.ContainsKey(regKey))
                    {
                        saldoConsumidoPorRegistro[regKey] = 0m;
                    }
                    saldoConsumidoPorRegistro[regKey] += cantConsumir;
                }

                resultado.Add(resItem);
            }

            return resultado;
        }
    }
}

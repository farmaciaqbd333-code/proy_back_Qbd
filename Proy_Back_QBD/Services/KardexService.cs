using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
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

namespace proy_back_Qbd.Services
{
    public class KardexService : IKardexService
    {
        private readonly ApiContext _context;
        private static readonly List<string> FamiliasAptas = ["MP", "ME"];
        private readonly ISupplyRepository _repository;
        private readonly IUnitOfWork _unitWork;
        private readonly ILogger<KardexService> _logger;
        public KardexService(
            ApiContext context,
            ISupplyRepository repository,
            IUnitOfWork unitWork,
            ILogger<KardexService> logger)
        {
            _context = context;
            _repository = repository;
            _unitWork = unitWork;
            _logger = logger;
        }

        public async Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int idInsumo, int idSede)
        {
            var compraInsumos = await _context.CompraInsumos
                .Include(w => w.Compra)
                    .ThenInclude(c => c!.Proveedor)
                .Include(w => w.Compra)
                    .ThenInclude(c => c!.Sede)
                .Include(w => w.StockInsumos)
                    .ThenInclude(si => si.AjusteInsumos)
                .Include(w => w.NotaSalidaInsumos)
                    .ThenInclude(nsi => nsi.NotaSalida)
                        .ThenInclude(ns => ns!.SedeOrigen)
                .Include(w => w.NotaSalidaInsumos)
                    .ThenInclude(nsi => nsi.NotaSalida)
                        .ThenInclude(ns => ns!.SedeDestino)
                .Where(w =>
                    w.IdInsumo == idInsumo &&
                    (
                        (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                        w.NotaSalidaInsumos.Any(nsi =>
                            nsi.NotaSalida != null &&
                            nsi.NotaSalida.IdSedeDestino == idSede &&
                            (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0))
                        )
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var resultado = new List<DetalleInsumoRes>();

            _logger.LogInformation(
                "INICIO cálculo de stock de insumos | IdSede: {IdSede} | CantidadCompraInsumos: {CantidadCompraInsumos}",
                idSede,
                compraInsumos?.Count ?? 0
            );

            var compraIds = compraInsumos.Select(c => c.Id).ToList();

            var ajustesInsumoQuery = await _context.AjusteInsumos
                .Include(a => a.StockInsumo)
                .Where(a =>
                    (a.StockInsumo != null && a.StockInsumo.IdCompraInsumo.HasValue && compraIds.Contains(a.StockInsumo.IdCompraInsumo.Value) && (idSede == 0 || a.StockInsumo.IdSede == idSede)) ||
                    compraIds.Contains(a.IdStockInsumo)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompra = ajustesInsumoQuery
                .GroupBy(a => (a.StockInsumo != null && a.StockInsumo.IdCompraInsumo.HasValue && a.StockInsumo.IdCompraInsumo.Value > 0) ? a.StockInsumo.IdCompraInsumo.Value : a.IdStockInsumo)
                .ToDictionary(g => g.Key, g => g.ToList());

            var salidasList = await ObtenerSalidasInsumo(idInsumo, idSede);
            var salidasPorReg = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in salidasList)
            {
                if (!string.IsNullOrEmpty(s.RegistroLoteInsumo))
                {
                    var key = s.RegistroLoteInsumo.Trim().ToUpper();
                    decimal cant = s.Cantidad;
                    if (s.Um == "KG" || s.Um == "KILOGRAMOS" || s.Um == "Kg" || s.Um == "Kgs")
                    {
                        cant *= 1000m;
                    }
                    if (!salidasPorReg.ContainsKey(key)) salidasPorReg[key] = 0m;
                    salidasPorReg[key] += cant;
                }
            }

            foreach (var compraInsumo in compraInsumos)
            {
                bool esKg = compraInsumo.Um != null && (
                    string.Equals(compraInsumo.Um.Trim(), "KG", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(compraInsumo.Um.Trim(), "KILOGRAMO", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(compraInsumo.Um.Trim(), "KILOGRAMOS", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(compraInsumo.Um.Trim(), "KGS", StringComparison.OrdinalIgnoreCase)
                );

                // 1. Entrada por compra (si esta sede fue quien compró a proveedor)
                bool esCompraEstaSede = compraInsumo.Compra != null &&
                    compraInsumo.Compra.IdSede == idSede &&
                    (idSede == 15 || compraInsumo.Compra.FechaLab != null);

                decimal entradasCompra = 0m;
                if (esCompraEstaSede)
                {
                    if (esKg)
                    {
                        if (compraInsumo.CantidadRecibida.HasValue && compraInsumo.CantidadRecibida.Value > 0)
                        {
                            if (compraInsumo.CantidadRecibida.Value <= compraInsumo.CantidadSolicitada * 2 && compraInsumo.CantidadRecibida.Value < 500)
                            {
                                entradasCompra = compraInsumo.CantidadRecibida.Value * 1000m;
                            }
                            else
                            {
                                entradasCompra = compraInsumo.CantidadRecibida.Value;
                            }
                        }
                        else
                        {
                            entradasCompra = compraInsumo.CantidadSolicitada * 1000m;
                        }
                    }
                    else
                    {
                        entradasCompra = (compraInsumo.CantidadRecibida.HasValue && compraInsumo.CantidadRecibida.Value > 0)
                            ? compraInsumo.CantidadRecibida.Value
                            : compraInsumo.CantidadSolicitada;
                    }
                }

                // 2. Entradas por Notas de Salida recibidas que tienen como sede destino esta sede
                var notasSalidaDestino = compraInsumo.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida != null &&
                        nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                    .OrderBy(nsi => nsi.NotaSalida != null ? (nsi.NotaSalida.FechaSalida != default ? nsi.NotaSalida.FechaSalida : nsi.NotaSalida.FechaCreacion) : DateTime.MinValue)
                    .ToList();

                // Omitir si no tiene entradas en esta sede
                if (entradasCompra == 0 && !notasSalidaDestino.Any())
                {
                    continue;
                }

                var registro = "MP" + Alfanumerico.ConvertToBase36(compraInsumo.Id);

                decimal salidasReales = 0m;
                if (salidasPorReg.TryGetValue(registro, out var sVal))
                {
                    salidasReales = sVal;
                }
                else
                {
                    salidasReales = compraInsumo.NotaSalidaInsumos
                        .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeOrigen == idSede)
                        .Sum(nsi => ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * nsi.Cantidad);
                }

                var stockSede = compraInsumo.StockInsumos
                    .Where(si => si.IdSede == idSede)
                    .ToList();

                var ajustesDeCompra = ajustesPorCompra.ContainsKey(compraInsumo.Id) ? ajustesPorCompra[compraInsumo.Id] : new List<AjusteInsumo>();
                bool tieneAjuste = ajustesDeCompra.Any();
                decimal totalAjuste = ajustesDeCompra.Sum(a => a.Ajuste);

                string umDefecto = stockSede.FirstOrDefault()?.UnidadMedida ?? (compraInsumo.Insumo != null ? compraInsumo.Insumo.UnidadMedida : (compraInsumo.Um ?? "G"));
                string? numeroFacturaCompra = ExtraerNumeroFactura(compraInsumo.Compra?.SerieComprobante, compraInsumo.Compra?.NumeroComprobante, compraInsumo.Compra?.CodFacQBD);


                // 1. DISTRIBUCION FIFO DE SALIDAS ENTRE ENTRADAS DE ESTE REGISTRO
                decimal salidasRestantes = salidasReales;

                // 1. ENTRADA POR COMPRA (si corresponde a esta sede)
                if (esCompraEstaSede && entradasCompra > 0)
                {
                    var stockPrincipal = stockSede.FirstOrDefault(w => w.IdNotaSalidaInsumo == null || w.IdNotaSalidaInsumo == 0);
                    decimal salidasCompra = Math.Min(entradasCompra, salidasRestantes);
                    salidasRestantes -= salidasCompra;

                    decimal saldoCompra = tieneAjuste
                        ? (ajustesDeCompra.FirstOrDefault()?.StockNuevo ?? Math.Max(0m, entradasCompra - salidasCompra + totalAjuste))
                        : Math.Max(0m, entradasCompra - salidasCompra);

                    string sedeOrigenCompra = compraInsumo.Compra?.Proveedor?.Datos ?? compraInsumo.Compra?.Sede?.Nombre ?? "COMPRA";
                    string cuoCompra = compraInsumo.Compra != null ? ((compraInsumo.Compra.IdSede == 1 ? "OCL" : (compraInsumo.Compra.IdSede == 2 ? "OCPJ" : (compraInsumo.Compra.IdSede == 3 ? "OCP" : (compraInsumo.Compra.IdSede == 4 ? "OCT" : (compraInsumo.Compra.IdSede == 12 ? "OCCS" : "OC"))))) + compraInsumo.Compra.Id) : "";
                    string docOrigenCompra = !string.IsNullOrEmpty(cuoCompra) ? cuoCompra : (compraInsumo.Compra?.NumeroComprobante ?? compraInsumo.Compra?.CodFacQBD ?? (compraInsumo.Compra?.Id != 0 ? $"OC-{compraInsumo.Compra.Id}" : ""));
                    // Factura ya obtenida en numeroFacturaCompra
                    DateTime? fechaIngresoCompra = compraInsumo.Compra != null ? (compraInsumo.Compra.FechaLab ?? compraInsumo.Compra.FechaFactura) : null;

                    resultado.Add(new DetalleInsumoRes
                    {
                        Registro = registro,
                        Lote = compraInsumo.Lote ?? "",
                        CantidadIngresada = entradasCompra,
                        CantidadSolicitada = esKg ? compraInsumo.CantidadSolicitada * 1000m : compraInsumo.CantidadSolicitada,
                        Salidas = salidasCompra,
                        Ajuste = tieneAjuste ? totalAjuste : (decimal?)null,
                        TieneAjuste = tieneAjuste,
                        Saldo = saldoCompra,
                        StockDisponible = saldoCompra,
                        Um = esKg ? "G" : (stockPrincipal?.UnidadMedida ?? umDefecto),
                        FechaCompra = fechaIngresoCompra,
                        FechaFabricacion = compraInsumo.FechaFabricacion,
                        FechaVencimiento = compraInsumo.FechaVencimiento,
                        Observacion = compraInsumo.Observacion,
                        TipoOrigen = "Orden de Compra",
                        SedeOrigen = sedeOrigenCompra,
                        DocumentoOrigen = docOrigenCompra,
                        NumeroFactura = numeroFacturaCompra,
                        IdCompra = compraInsumo.Id,
                        CUO = !string.IsNullOrEmpty(cuoCompra) ? cuoCompra : null
                    });
                }

                // 2. ENTRADAS POR NOTA DE SALIDA (traslados recibidos en esta sede)
                foreach (var nsi in notasSalidaDestino)
                {
                    decimal cantRecibida = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) *
                        ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad);

                    decimal salidasNS = Math.Min(cantRecibida, salidasRestantes);
                    salidasRestantes -= salidasNS;

                    var stockNS = stockSede.FirstOrDefault(w => w.IdNotaSalidaInsumo == nsi.Id);
                    var ajustesNS = stockNS?.AjusteInsumos?.ToList();
                    bool tieneAjusteNS = ajustesNS != null && ajustesNS.Any();
                    decimal totalAjusteNS = tieneAjusteNS ? ajustesNS!.Sum(a => a.Ajuste) : 0m;

                    decimal saldoNS = tieneAjusteNS
                        ? (ajustesNS!.OrderByDescending(a => a.FechaCreacion).FirstOrDefault()?.StockNuevo ?? Math.Max(0m, cantRecibida - salidasNS + totalAjusteNS))
                        : Math.Max(0m, cantRecibida - salidasNS);

                    DateTime? fechaNS = nsi.NotaSalida != null
                        ? (nsi.NotaSalida.FechaSalida != default ? nsi.NotaSalida.FechaSalida : nsi.NotaSalida.FechaCreacion)
                        : null;

                    string sedeOrigenNS = nsi.NotaSalida?.SedeOrigen?.Nombre ?? "SEDE ORIGEN";
                    string docOrigenNS = nsi.NotaSalida != null ? $"NT{nsi.NotaSalida.Id}" : (nsi.IdNotaSalida != 0 ? $"NT{nsi.IdNotaSalida}" : "NOTA SALIDA");
                    string? numFacturaNS = (!string.IsNullOrEmpty(numeroFacturaCompra) && numeroFacturaCompra != "-") ? numeroFacturaCompra : null;
                    string obsNS = !string.IsNullOrEmpty(nsi.Observacion) ? nsi.Observacion : (nsi.NotaSalida?.Observacion ?? compraInsumo.Observacion ?? "");

                    resultado.Add(new DetalleInsumoRes
                    {
                        Registro = registro,
                        Lote = compraInsumo.Lote ?? "",
                        CantidadIngresada = cantRecibida,
                        CantidadSolicitada = nsi.Cantidad,
                        Salidas = salidasNS,
                        Ajuste = tieneAjusteNS ? totalAjusteNS : (decimal?)null,
                        TieneAjuste = tieneAjusteNS,
                        Saldo = saldoNS,
                        StockDisponible = saldoNS,
                        Um = stockNS?.UnidadMedida ?? umDefecto,
                        FechaCompra = fechaNS,
                        FechaFabricacion = compraInsumo.FechaFabricacion,
                        FechaVencimiento = compraInsumo.FechaVencimiento,
                        Observacion = obsNS,
                        TipoOrigen = "Nota de Salida",
                        SedeOrigen = sedeOrigenNS,
                        DocumentoOrigen = docOrigenNS,
                        NumeroFactura = numFacturaNS,
                        IdCompra = compraInsumo.Id,
                        CUO = docOrigenNS
                    });
                }
            }

            return resultado;
        }
        public async Task<List<DetalleInsumoRes>> ObtenerDetallePI(int idInsumo, int idSede)
        {
            var resultado = new List<DetalleInsumoRes>();

            // 1. Elaboraciones directas en esta sede
            var piList = await _context.ProductosIntermedios
                .Include(pi => pi.StockInsumo)
                    .ThenInclude(si => si.AjusteInsumos)
                .Where(w => w.IdInsumo == idInsumo && w.IdSede == idSede)
                .OrderByDescending(o => o.FechaCreacion)
                .ThenByDescending(o => o.Id)
                .ToListAsync();

            foreach (var s in piList)
            {
                decimal cantIngresada = (s.TipoUso == "PI-FMG" || s.Um == "UND")
                    ? (s.LoteEstandar ?? 0)
                    : (s.LoteEstTotal ?? s.LoteEstandar ?? 0);

                var ajustesDePI = s.StockInsumo?.AjusteInsumos?.ToList() ?? new List<AjusteInsumo>();
                bool tieneAjuste = ajustesDePI.Any();
                decimal totalAjuste = ajustesDePI.Sum(a => a.Ajuste);

                decimal saldo = s.StockInsumo != null ? s.StockInsumo.StockDisponible : cantIngresada;
                decimal salidas = Math.Max(0m, cantIngresada - saldo + totalAjuste);

                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "PI" + Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    CantidadIngresada = cantIngresada,
                    Salidas = salidas,
                    Ajuste = tieneAjuste ? totalAjuste : (decimal?)null,
                    TieneAjuste = tieneAjuste,
                    Saldo = saldo,
                    StockDisponible = saldo,
                    FechaCompra = s.FechaCreacion,
                    FechaFabricacion = s.FechaCreacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.StockInsumo?.AjusteInsumos?.FirstOrDefault()?.Observacion ?? "",
                    IdCompra = s.Id
                });
            }

            // 2. Transferencias recibidas por Nota de Salida en esta sede
            var lotesDelInsumo = await _context.ProductosIntermedios.AsNoTracking()
                .Where(p => p.IdInsumo == idInsumo && !string.IsNullOrEmpty(p.Lote))
                .ToListAsync();

            var loteDict = lotesDelInsumo
                .GroupBy(p => p.Lote!.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.First());

            var notasSalidaDestino = await _context.NotaSalidaInsumos.AsNoTracking()
                .Include(nsi => nsi.NotaSalida)
                    .ThenInclude(ns => ns!.SedeOrigen)
                .Where(nsi => !nsi.IdCompraInsumo.HasValue &&
                              nsi.NotaSalida != null &&
                              nsi.NotaSalida.IdSedeDestino == idSede &&
                              (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                .OrderByDescending(nsi => nsi.NotaSalida!.FechaRecepcion ?? nsi.NotaSalida!.FechaSalida)
                .ToListAsync();

            foreach (var nsi in notasSalidaDestino)
            {
                if (string.IsNullOrEmpty(nsi.Lote)) continue;
                var key = nsi.Lote.Trim().ToUpper();
                if (!loteDict.TryGetValue(key, out var piRef)) continue;

                var stockNS = await _context.StockInsumos.AsNoTracking()
                    .Include(s => s.AjusteInsumos)
                    .FirstOrDefaultAsync(s => s.IdSede == idSede && s.IdNotaSalidaInsumo == nsi.Id);

                decimal cantRecibida = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) *
                    ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad);

                var ajustesNS = stockNS?.AjusteInsumos?.ToList() ?? new List<AjusteInsumo>();
                bool tieneAjusteNS = ajustesNS.Any();
                decimal totalAjusteNS = tieneAjusteNS ? ajustesNS.Sum(a => a.Ajuste) : 0m;

                decimal saldo = stockNS != null ? stockNS.StockDisponible : cantRecibida;
                decimal salidas = Math.Max(0m, cantRecibida - saldo + totalAjusteNS);

                DateTime? fechaNS = nsi.NotaSalida != null
                    ? (nsi.NotaSalida.FechaRecepcion ?? (nsi.NotaSalida.FechaSalida != default ? nsi.NotaSalida.FechaSalida : nsi.NotaSalida.FechaCreacion))
                    : null;

                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "PI" + Alfanumerico.ConvertToBase36(piRef.Id),
                    Lote = nsi.Lote ?? piRef.Lote ?? "",
                    CantidadIngresada = cantRecibida,
                    Salidas = salidas,
                    Ajuste = tieneAjusteNS ? totalAjusteNS : (decimal?)null,
                    TieneAjuste = tieneAjusteNS,
                    Saldo = saldo,
                    StockDisponible = saldo,
                    FechaCompra = fechaNS ?? piRef.FechaCreacion,
                    FechaFabricacion = piRef.FechaEmision ?? piRef.FechaCreacion,
                    FechaVencimiento = piRef.FechaVencimiento,
                    Observacion = nsi.Observacion ?? stockNS?.AjusteInsumos?.FirstOrDefault()?.Observacion ?? "",
                    IdCompra = piRef.Id,
                    CUO = nsi.NotaSalida != null ? $"NT{nsi.NotaSalida.Id}" : null,
                    TipoOrigen = "Nota de Salida",
                    SedeOrigen = nsi.NotaSalida?.SedeOrigen?.Nombre ?? "CENTRAL"
                });
            }

            return resultado;
        }

        public async Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId, int idSede)
        {
            var compras = await _context.CompraEmpaques
                .Include(w => w.Compra)
                    .ThenInclude(c => c!.Proveedor)
                .Include(w => w.Compra)
                    .ThenInclude(c => c!.Sede)
                .Include(w => w.StockEmpaques)
                    .ThenInclude(se => se.AjusteEmpaques)
                .Include(w => w.NotaSalidaEmpaques)
                    .ThenInclude(nse => nse.NotaSalida)
                        .ThenInclude(ns => ns!.SedeOrigen)
                .Where(w => w.IdEmpaque == empaqueId && ((w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) || w.NotaSalidaEmpaques.Any(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede && (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0)))))
                .ToListAsync();

            var compraEmpaqueIds = compras.Select(c => c.Id).ToList();
            var ajustesEmpaqueQuery = await _context.AjusteEmpaques
                .Include(a => a.StockEmpaque)
                .Where(a =>
                    (a.StockEmpaque != null && compraEmpaqueIds.Contains(a.StockEmpaque.IdCompraEmpaque) && (idSede == 0 || a.StockEmpaque.IdSede == idSede)) ||
                    compraEmpaqueIds.Contains(a.IdStockEmpaque)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorEmpaque = ajustesEmpaqueQuery
                .GroupBy(a => (a.StockEmpaque != null && a.StockEmpaque.IdCompraEmpaque > 0) ? a.StockEmpaque.IdCompraEmpaque : a.IdStockEmpaque)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultado = new List<DetalleEmpaqueRes>();

            foreach (var s in compras)
            {
                var stockSede = s.StockEmpaques.Where(w => w.IdSede == idSede).ToList();
                var ajustesDeEmpaque = ajustesPorEmpaque.ContainsKey(s.Id) ? ajustesPorEmpaque[s.Id] : new List<AjusteEmpaque>();
                bool tieneAjuste = ajustesDeEmpaque.Any();
                decimal totalAjuste = ajustesDeEmpaque.Sum(a => a.Ajuste);

                decimal salidasNS = s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nse => nse.Cantidad);

                // Auto-corrección si Compra 159 en CENTRAL sufrió doble deducción por confirmación
                if (s.Id == 159 && idSede == 15)
                {
                    var stockPrincipal159 = stockSede.FirstOrDefault(w => w.IdNotaSalidaEmpaque == null);
                    if (stockPrincipal159 != null && stockPrincipal159.StockDisponible < 85m)
                    {
                        stockPrincipal159.StockDisponible = 85m;
                        try { _context.SaveChanges(); } catch { }
                    }
                }

                // 1. ENTRADA POR ORDEN DE COMPRA (si esta sede fue quien compró a proveedor)
                bool esCompraEstaSede = s.Compra != null && s.Compra.IdSede == idSede && (idSede == 15 || s.Compra.FechaLab != null);
                decimal cantCompra = esCompraEstaSede ? (s.CantidadRecibida.HasValue && s.CantidadRecibida.Value > 0 ? s.CantidadRecibida.Value : s.CantidadSolicitada) : 0m;

                if (esCompraEstaSede && cantCompra > 0)
                {
                    var stockPrincipal = stockSede.FirstOrDefault(w => w.IdNotaSalidaEmpaque == null);
                    decimal saldoCompra = 0m;
                    if (stockPrincipal != null)
                    {
                        saldoCompra = stockPrincipal.StockDisponible;
                    }
                    else if (tieneAjuste)
                    {
                        saldoCompra = ajustesDeEmpaque.First().StockNuevo;
                    }
                    else
                    {
                        saldoCompra = Math.Max(0m, cantCompra - salidasNS);
                    }

                    resultado.Add(new DetalleEmpaqueRes
                    {
                        Registro = "ME" + Alfanumerico.ConvertToBase36(s.Id),
                        Lote = s.Lote ?? "",
                        CantidadIngresada = cantCompra,
                        Salidas = salidasNS,
                        Ajuste = tieneAjuste ? totalAjuste : (decimal?)null,
                        TieneAjuste = tieneAjuste,
                        Saldo = saldoCompra,
                        FechaCompra = s.Compra != null ? (s.Compra.FechaLab ?? s.Compra.FechaFactura) : null,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Observacion = s.Observacion,
                        TipoOrigen = "Orden de Compra",
                        SedeOrigen = s.Compra?.Proveedor?.Datos ?? s.Compra?.Sede?.Nombre ?? "PROVEEDOR",
                        DocumentoOrigen = (s.Compra != null ? ((s.Compra.IdSede == 1 ? "OCL" : (s.Compra.IdSede == 2 ? "OCPJ" : (s.Compra.IdSede == 3 ? "OCP" : (s.Compra.IdSede == 4 ? "OCT" : (s.Compra.IdSede == 12 ? "OCCS" : "OC"))))) + s.Compra.Id) : "") is string cuoEmp && !string.IsNullOrEmpty(cuoEmp) ? cuoEmp : (s.Compra?.NumeroComprobante ?? s.Compra?.CodFacQBD ?? (s.Compra?.Id != 0 ? $"OC-{s.Compra?.Id}" : "")),
                        NumeroFactura = (s.Compra?.SerieComprobante ?? "") + (string.IsNullOrEmpty(s.Compra?.SerieComprobante) || string.IsNullOrEmpty(s.Compra?.NumeroComprobante) ? "" : "-") + (s.Compra?.NumeroComprobante ?? ""),
                        IdCompra = s.Id
                    });
                }

                // 2. ENTRADAS POR NOTA DE SALIDA (traslados recibidos en esta sede)
                var notasSalidaDestino = s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede && (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0)))
                    .OrderBy(nse => nse.NotaSalida != null ? nse.NotaSalida.FechaCreacion : DateTime.MinValue)
                    .ToList();

                foreach (var nse in notasSalidaDestino)
                {
                    decimal cantRecibida = nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad;
                    var stockNS = stockSede.FirstOrDefault(w => w.IdNotaSalidaEmpaque == nse.Id);

                    decimal saldoNS = 0m;
                    if (stockNS != null)
                    {
                        saldoNS = stockNS.StockDisponible;
                    }
                    else if (!esCompraEstaSede && stockSede.Any())
                    {
                        saldoNS = stockSede.Sum(se => se.StockDisponible);
                    }
                    else
                    {
                        saldoNS = cantRecibida;
                    }

                    decimal salidasDeEsteItem = !esCompraEstaSede ? salidasNS : 0m;

                    resultado.Add(new DetalleEmpaqueRes
                    {
                        Registro = "ME" + Alfanumerico.ConvertToBase36(s.Id),
                        Lote = s.Lote ?? "",
                        CantidadIngresada = cantRecibida,
                        Salidas = salidasDeEsteItem,
                        Ajuste = null,
                        TieneAjuste = false,
                        Saldo = saldoNS,
                        FechaCompra = nse.NotaSalida != null ? (nse.NotaSalida.FechaSalida != default ? nse.NotaSalida.FechaSalida : nse.NotaSalida.FechaCreacion) : null,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Observacion = !string.IsNullOrEmpty(nse.Observacion) ? nse.Observacion : (nse.NotaSalida?.Observacion ?? ""),
                        TipoOrigen = "Nota de Salida",
                        SedeOrigen = nse.NotaSalida?.SedeOrigen?.Nombre ?? "SEDE ORIGEN",
                        DocumentoOrigen = nse.NotaSalida != null ? $"NT{nse.NotaSalida.Id}" : (nse.IdNotaSalida != 0 ? $"NT{nse.IdNotaSalida}" : "NOTA SALIDA"),
                        NumeroFactura = (!string.IsNullOrEmpty(s.Compra?.SerieComprobante) && !string.IsNullOrEmpty(s.Compra?.NumeroComprobante)) ? $"{s.Compra.SerieComprobante}-{s.Compra.NumeroComprobante}" : (!string.IsNullOrEmpty(s.Compra?.NumeroComprobante) ? s.Compra.NumeroComprobante : s.Compra?.CodFacQBD),
                        IdCompra = s.Id
                    });
                }
            }

            return resultado;
        }
        public async Task<List<DetalleInsumoRes>> ObtenerDetallePT(int idProducto, int idSede)
        {
            var compras = await _context.CompraProductos
                .Include(w => w.Compra)
                .Include(w => w.StockProductoTerminados)
                    .ThenInclude(sp => sp.AjusteProductos)
                .Include(w => w.NotaSalidaProductos)
                    .ThenInclude(nsp => nsp.NotaSalida)
                        .ThenInclude(ns => ns!.SedeOrigen)
                .Where(w => w.IdProducto == idProducto)
                .ToListAsync();

            var compraPtIds = compras.Select(c => c.Id).ToList();
            var ajustesPtQuery = await _context.AjusteProductoTerminados
                .Include(a => a.StockProducto)
                .Where(a =>
                    (a.StockProducto != null && compraPtIds.Contains(a.StockProducto.IdCompraProducto) && (idSede == 0 || a.StockProducto.IdSede == idSede)) ||
                    compraPtIds.Contains(a.IdStockProducto)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorPt = ajustesPtQuery
                .GroupBy(a => (a.StockProducto != null && a.StockProducto.IdCompraProducto > 0) ? a.StockProducto.IdCompraProducto : a.IdStockProducto)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultado = new List<DetalleInsumoRes>();

            foreach (var s in compras)
            {
                bool esCompraEstaSede = s.Compra != null && s.Compra.IdSede == idSede;
                decimal cantCompra = esCompraEstaSede
                    ? (s.CantidadRecibida.HasValue && s.CantidadRecibida.Value > 0 ? s.CantidadRecibida.Value : s.CantidadSolicitada)
                    : 0m;

                var notasSalidaDestino = s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeDestino == idSede && (nsp.NotaSalida.Estado == "RECIBIDO" || nsp.NotaSalida.Estado == "RECEPCIONADO" || nsp.NotaSalida.FechaRecepcion != null || (nsp.CantidadRecibida > 0)))
                    .OrderBy(nsp => nsp.NotaSalida != null ? (nsp.NotaSalida.FechaSalida != default ? nsp.NotaSalida.FechaSalida : nsp.NotaSalida.FechaCreacion) : DateTime.MinValue)
                    .ToList();

                if (cantCompra == 0 && !notasSalidaDestino.Any())
                {
                    continue;
                }

                decimal salidasNS = s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nsp => nsp.Cantidad);

                var stockSede = s.StockProductoTerminados.Where(w => w.IdSede == idSede).ToList();
                var ajustesDePt = ajustesPorPt.ContainsKey(s.Id) ? ajustesPorPt[s.Id] : new List<AjusteProducto>();
                bool tieneAjuste = ajustesDePt.Any();
                decimal totalAjuste = ajustesDePt.Sum(a => a.Ajuste);

                if (esCompraEstaSede && cantCompra > 0)
                {
                    var stockPrincipal = stockSede.FirstOrDefault(w => w.IdNotaSalidaProducto == null || w.IdNotaSalidaProducto == 0);
                    decimal saldoCompra = stockPrincipal != null
                        ? stockPrincipal.StockDisponible
                        : (tieneAjuste ? ajustesDePt.First().StockNuevo : Math.Max(0m, cantCompra - salidasNS));

                    resultado.Add(new DetalleInsumoRes
                    {
                        Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
                        Lote = s.Lote ?? "",
                        CantidadIngresada = cantCompra,
                        Salidas = salidasNS,
                        Ajuste = tieneAjuste ? totalAjuste : (decimal?)null,
                        TieneAjuste = tieneAjuste,
                        Saldo = saldoCompra,
                        StockDisponible = saldoCompra,
                        FechaCompra = s.Compra != null ? (s.Compra.FechaLab ?? s.Compra.FechaFactura) : null,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Observacion = s.Observacion,
                        TipoOrigen = "Orden de Compra",
                        SedeOrigen = s.Compra?.Proveedor?.Datos ?? s.Compra?.Sede?.Nombre ?? "PROVEEDOR",
                        DocumentoOrigen = s.Compra?.NumeroComprobante ?? s.Compra?.CodFacQBD ?? (s.Compra?.Id != 0 ? $"OC-{s.Compra?.Id}" : ""),
                        NumeroFactura = (s.Compra?.SerieComprobante ?? "") + (string.IsNullOrEmpty(s.Compra?.SerieComprobante) || string.IsNullOrEmpty(s.Compra?.NumeroComprobante) ? "" : "-") + (s.Compra?.NumeroComprobante ?? ""),
                        IdCompra = s.Id
                    });
                }

                foreach (var nsp in notasSalidaDestino)
                {
                    decimal cantRecibida = nsp.CantidadRecibida > 0 ? nsp.CantidadRecibida : nsp.Cantidad;
                    var stockNS = stockSede.FirstOrDefault(w => w.IdNotaSalidaProducto == nsp.Id);
                    decimal saldoNS = stockNS != null
                        ? stockNS.StockDisponible
                        : (!esCompraEstaSede && stockSede.Any() ? stockSede.Sum(sp => sp.StockDisponible) : cantRecibida);

                    decimal salidasDeEsteItem = !esCompraEstaSede ? salidasNS : Math.Max(0m, cantRecibida - saldoNS);

                    resultado.Add(new DetalleInsumoRes
                    {
                        Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
                        Lote = s.Lote ?? "",
                        CantidadIngresada = cantRecibida,
                        Salidas = salidasDeEsteItem,
                        Ajuste = null,
                        TieneAjuste = false,
                        Saldo = saldoNS,
                        StockDisponible = saldoNS,
                        FechaCompra = nsp.NotaSalida != null ? (nsp.NotaSalida.FechaSalida != default ? nsp.NotaSalida.FechaSalida : nsp.NotaSalida.FechaCreacion) : null,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Observacion = !string.IsNullOrEmpty(nsp.Observacion) ? nsp.Observacion : (nsp.NotaSalida?.Observacion ?? s.Observacion ?? ""),
                        TipoOrigen = "Nota de Salida",
                        SedeOrigen = nsp.NotaSalida?.SedeOrigen?.Nombre ?? "SEDE ORIGEN",
                        DocumentoOrigen = nsp.NotaSalida != null ? $"NT{nsp.NotaSalida.Id}" : (nsp.IdNotaSalida != 0 ? $"NT{nsp.IdNotaSalida}" : "NOTA SALIDA"),
                        NumeroFactura = (!string.IsNullOrEmpty(s.Compra?.SerieComprobante) && !string.IsNullOrEmpty(s.Compra?.NumeroComprobante)) ? $"{s.Compra.SerieComprobante}-{s.Compra.NumeroComprobante}" : (!string.IsNullOrEmpty(s.Compra?.NumeroComprobante) ? s.Compra.NumeroComprobante : s.Compra?.CodFacQBD),
                        IdCompra = s.Id
                    });
                }
            }

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
                "PT" => await ObtenerProductosTerminados(idSede),
                _ => throw new BadRequestException("FAMILIA NO VALIDA")

            };
            foreach (var item in responseMP)
            {
                if (item.Stock == null || item.Saldo == null)
                {
                    var calc = (item.Entradas ?? 0) - (item.Salidas ?? 0) + (item.Ajustes ?? 0) - (item.Baja ?? 0);
                    item.Stock ??= calc;
                    item.Saldo ??= calc;
                }
            }


            return responseMP;
        }

        public async Task<List<ComprasVencidasRes>> ObtenerComprasVencidas(string familia, int idSede)
        {
            List<ComprasVencidasRes> response = new();
            if (FamiliasAptas.Contains(familia))
            {
                if (familia == "MP")
                {
                    response = await _context.CompraInsumos.Where(w => w.Compra != null && w.Compra.IdSede == idSede && w.Compra.FechaLab != null).Select(s => new ComprasVencidasRes()
                    {
                        Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
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
                    response = await _context.CompraEmpaques.Where(w => w.Compra != null && w.Compra.IdSede == idSede && w.Compra.FechaLab != null).Select(s => new ComprasVencidasRes()
                    {
                        Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
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
                response = response.OrderBy(x =>
                {
                    var clean = System.Text.RegularExpressions.Regex.Replace(x.Codigo ?? "0", @"[^\d]", "");
                    return int.TryParse(clean, out int num) ? num : 0;
                }).ToList();
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
            _logger.LogInformation(
                "INICIO ObtenerMateriaPrima | IdSede: {IdSede}",
                idSede
            );

            // ============================================================
            // 1. OBTENER INSUMOS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 1: Obteniendo insumos de materia prima | IdSede: {IdSede}",
                idSede
            );

            var insumos = await _context.Insumos
                .AsNoTracking()
                .Where(i => i.Clasificacion == "MP" || i.Clasificacion == null)
                .Select(i => new
                {
                    i.Id,
                    i.Descripcion,
                    i.UnidadMedida,
                    i.Tipo
                })
                .OrderBy(o => o.Id)
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 1 FINALIZADA | IdSede: {IdSede} | CantidadInsumos: {CantidadInsumos}",
                idSede,
                insumos.Count
            );

            if (!insumos.Any())
            {
                _logger.LogWarning(
                    "No se encontraron insumos de materia prima | IdSede: {IdSede}",
                    idSede
                );

                return new List<StockRes>();
            }

            var insumoIds = insumos
                .Select(i => i.Id)
                .ToList();

            // ============================================================
            // 2. OBTENER ENTRADAS POR COMPRAS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 2: Obteniendo entradas por compras | IdSede: {IdSede}",
                idSede
            );

            var entradasCompra = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.Compra != null &&
                    ci.Compra.IdSede == idSede &&
                    (idSede == 15 || ci.Compra.FechaLab != null))
                .GroupBy(ci => ci.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(ci => ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0 ? (ci.CantidadRecibida.Value <= ci.CantidadSolicitada * 2 && ci.CantidadRecibida.Value < 500 && (ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? ci.CantidadRecibida.Value * 1000m : ci.CantidadRecibida.Value) : (ci.CantidadSolicitada * ((ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? 1000m : 1m)))
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 2 FINALIZADA | IdSede: {IdSede} | InsumosConEntradasCompra: {Cantidad}",
                idSede,
                entradasCompra.Count
            );

            foreach (var item in entradasCompra)
            {
                _logger.LogDebug(
                    "Entrada compra | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 3. ENTRADAS POR NOTAS DE SALIDA RECIBIDAS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 3: Obteniendo entradas por notas de salida recibidas | IdSede: {IdSede}",
                idSede
            );

            var entradasNotaSalida = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.NotaSalidaInsumos.Any(nsi =>
                        nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0))))
                .SelectMany(ci => ci.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                    .Select(nsi => new
                    {
                        ci.IdInsumo,
                        Cantidad = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0 ? nsi.CantidadRecibida.Value : nsi.Cantidad)
                    }))
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.Cantidad)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 3 FINALIZADA | IdSede: {IdSede} | InsumosConEntradasNS: {Cantidad}",
                idSede,
                entradasNotaSalida.Count
            );

            foreach (var item in entradasNotaSalida)
            {
                _logger.LogDebug(
                    "Entrada nota salida | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 4. SALIDAS A PRODUCTO INTERMEDIO
            // ============================================================

            _logger.LogInformation(
                "Solicitud 4: Obteniendo salidas a Producto Intermedio | IdSede: {IdSede}",
                idSede
            );

            var salidasProductoIntermedio = await _context.InsumoProductoIntermedios
                .AsNoTracking()
                .Where(x =>
                    insumoIds.Contains(x.IdInsumo) &&
                    x.ProductoIntermedio.IdSede == idSede)
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => (x.Practica.HasValue && x.Practica.Value > 0) ? x.Practica.Value : x.CantidadLote)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 4 FINALIZADA | IdSede: {IdSede} | InsumosConSalidasPI: {Cantidad}",
                idSede,
                salidasProductoIntermedio.Count
            );

            foreach (var item in salidasProductoIntermedio)
            {
                _logger.LogDebug(
                    "Salida Producto Intermedio | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 5. SALIDAS A FORMULAS MAGISTRALES
            // ============================================================

            _logger.LogInformation(
                "Solicitud 5: Obteniendo salidas a Fórmulas Magistrales | IdSede: {IdSede}",
                idSede
            );

            var salidasFormula = await _context.FormulasCC
                .AsNoTracking()
                .Where(x =>
                    insumoIds.Contains(x.InsumoId) &&
                    x.Formula.SedeId == idSede)
                .GroupBy(x => x.InsumoId)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.CantidadL)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 5 FINALIZADA | IdSede: {IdSede} | InsumosConSalidasFormula: {Cantidad}",
                idSede,
                salidasFormula.Count
            );

            foreach (var item in salidasFormula)
            {
                _logger.LogDebug(
                    "Salida Fórmula Magistral | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 6. SALIDAS POR NOTAS DE SALIDA DESPACHADAS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 6: Obteniendo salidas por notas de salida | IdSede: {IdSede}",
                idSede
            );

            var salidasNotaSalida = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.NotaSalidaInsumos.Any(nsi =>
                        nsi.NotaSalida.IdSedeOrigen == idSede))
                .SelectMany(ci => ci.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida.IdSedeOrigen == idSede)
                    .Select(nsi => new
                    {
                        ci.IdInsumo,
                        Cantidad = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * nsi.Cantidad
                    }))
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.Cantidad)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 6 FINALIZADA | IdSede: {IdSede} | InsumosConSalidasNS: {Cantidad}",
                idSede,
                salidasNotaSalida.Count
            );

            foreach (var item in salidasNotaSalida)
            {
                _logger.LogDebug(
                    "Salida NotaSalida | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 7. AJUSTES
            // ============================================================

            _logger.LogInformation(
                "Solicitud 7: Obteniendo ajustes de stock | IdSede: {IdSede}",
                idSede
            );

            var ajustes = await _context.AjusteInsumos
                .AsNoTracking()
                .Where(a =>
                    a.StockInsumo.IdSede == idSede &&
                    insumoIds.Contains(a.StockInsumo.CompraInsumo.IdInsumo))
                .GroupBy(a => a.StockInsumo.CompraInsumo.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(a => a.Ajuste)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 7 FINALIZADA | IdSede: {IdSede} | InsumosConAjustes: {Cantidad}",
                idSede,
                ajustes.Count
            );

            foreach (var item in ajustes)
            {
                _logger.LogDebug(
                    "Ajuste | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 8. BAJAS POR VENCIMIENTO
            // ============================================================

            var ahora = DateTime.UtcNow;

            _logger.LogInformation(
                "Solicitud 8: Obteniendo bajas por vencimiento | IdSede: {IdSede} | FechaActualUTC: {FechaActualUTC}",
                idSede,
                ahora
            );

            var bajas = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.FechaVencimiento < ahora &&
                    ci.StockInsumos.Any(si => si.IdSede == idSede))
                .SelectMany(ci => ci.StockInsumos
                    .Where(si => si.IdSede == idSede)
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

            _logger.LogInformation(
                "Solicitud 8 FINALIZADA | IdSede: {IdSede} | InsumosConBajas: {Cantidad}",
                idSede,
                bajas.Count
            );

            foreach (var item in bajas)
            {
                _logger.LogDebug(
                    "Baja vencimiento | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 9. UBICACIONES
            // ============================================================

            _logger.LogInformation(
                "Solicitud 9: Obteniendo ubicaciones | IdSede: {IdSede}",
                idSede
            );

            var ubicaciones = await _context.SiteSupplies
                .AsNoTracking()
                .Where(x =>
                    x.IdSite == idSede &&
                    insumoIds.Contains(x.IdSupply))
                .Select(x => new
                {
                    x.IdSupply,
                    x.Location,
                    x.Limite,
                    x.CantidadEnviar
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 9 FINALIZADA | IdSede: {IdSede} | Ubicaciones: {Cantidad}",
                idSede,
                ubicaciones.Count
            );

            // ============================================================
            // 10. ARMAR RESULTADO
            // ============================================================

            _logger.LogInformation(
                "Armando resultado final | IdSede: {IdSede} | Insumos: {Cantidad}",
                idSede,
                insumos.Count
            );

            var resultado = new List<StockRes>();

            foreach (var insumo in insumos)
            {
                var entradaCompra = entradasCompra
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var entradaNS = entradasNotaSalida
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var salidaPI = salidasProductoIntermedio
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var salidaFormula = salidasFormula
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var salidaNS = salidasNotaSalida
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var ajuste = ajustes
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var baja = bajas
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var entradas = entradaCompra + entradaNS;

                var salidas = salidaPI + salidaFormula + salidaNS;

                var saldo = entradas - salidas + ajuste - baja;

                var siteSupply = ubicaciones
                    .FirstOrDefault(x => x.IdSupply == insumo.Id);
                var ubicacion = siteSupply?.Location;
                var limite = siteSupply?.Limite;
                var cantidadEnviar = siteSupply?.CantidadEnviar;

                _logger.LogInformation(
                    "Cálculo Insumo | IdSede: {IdSede} | IdInsumo: {IdInsumo} | Descripcion: {Descripcion} | " +
                    "EntradaCompra: {EntradaCompra} | EntradaNS: {EntradaNS} | EntradasTotal: {Entradas} | " +
                    "SalidaPI: {SalidaPI} | SalidaFormula: {SalidaFormula} | SalidaNS: {SalidaNS} | SalidasTotal: {Salidas} | " +
                    "Ajuste: {Ajuste} | Baja: {Baja} | Saldo: {Saldo} | Ubicacion: {Ubicacion} | Limite: {Limite}",
                    idSede,
                    insumo.Id,
                    insumo.Descripcion,
                    entradaCompra,
                    entradaNS,
                    entradas,
                    salidaPI,
                    salidaFormula,
                    salidaNS,
                    salidas,
                    ajuste,
                    baja,
                    saldo,
                    ubicacion,
                    limite
                );

                resultado.Add(new StockRes
                {
                    Codigo = insumo.Id.ToString(),
                    Descripcion = insumo.Descripcion ?? "",
                    Um = insumo.UnidadMedida ?? string.Empty,
                    Entradas = entradas,
                    Salidas = salidas,
                    Ajustes = ajuste,
                    Baja = baja,
                    Stock = saldo,
                    Saldo = saldo,
                    Tipo = insumo.Tipo,
                    CodigoUbicacion = ubicacion,
                    Limite = limite,
                    CantidadEnviar = cantidadEnviar
                });
            }

            _logger.LogInformation(
                "FIN ObtenerMateriaPrima | IdSede: {IdSede} | CantidadResultados: {CantidadResultados}",
                idSede,
                resultado.Count
            );

            return resultado;
        }

        private async Task<List<StockRes>> ObtenerProductosIntermedios(int idSede)
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

                decimal entradasTotal = entradasElab + entradasNS;
                decimal salidasTotal = salidasConsumo + salidasNS;
                decimal saldo = entradasTotal - salidasTotal + ajusteTotal;
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
                    Baja = 0,
                    Stock = saldo,
                    Saldo = saldo,
                    Tipo = insumo.Tipo,
                    CodigoUbicacion = siteSupply?.Location,
                    Limite = siteSupply?.Limite
                });
            }

            return resultado;
        }
        private async Task<List<StockRes>> ObtenerMateriaEmpaque(int idSede)
        {
            return await _context.Empaques.AsNoTracking()
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
        }
        private async Task<List<StockRes>> ObtenerEconomato(int idSede)
        {
            return await _context.Economatos.AsNoTracking()
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = s.Select(s => s.UnidadMedida).FirstOrDefault() ?? "Und",
                            Entradas = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(ce => ce.CantidadSolicitada)) +
                                       s.Sum(x => x.CompraEconomatos.Sum(x2 => x2.NotaSalidaEconomatos.Where(w => w.NotaSalida != null && w.NotaSalida.IdSedeDestino == idSede && (w.NotaSalida.Estado == "RECIBIDO" || w.NotaSalida.Estado == "RECEPCIONADO" || w.NotaSalida.FechaRecepcion != null || w.CantidadRecibida > 0)).Sum(x3 => x3.CantidadRecibida > 0 ? x3.CantidadRecibida : x3.Cantidad))),
                            Salidas = s.Sum(s => s.CompraEconomatos.Sum(s2 => s2.NotaSalidaEconomatos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.Cantidad))),
                            Ajustes = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEconomatos.Sum(s => s.AjusteEconomatos.Sum(s => s.Ajuste)))),
                            Baja = 0,
                            Tipo = null,
                            CodigoUbicacion = s.Select(s => s.EconomatoSedes.Where(w => w.IdSite == idSede).Select(s => s.Location).FirstOrDefault()).FirstOrDefault(),
                            Limite = s.Select(s => s.EconomatoSedes.Where(w => w.IdSite == idSede).Select(s => s.Limite).FirstOrDefault()).FirstOrDefault()
                        }).ToListAsync();
        }

        private async Task<List<StockRes>> ObtenerProductosTerminados(int idSede)
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

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasInsumo(int idInsumo, int idSede, string? familia = null)
        {
            var fam = (familia ?? "").ToUpper().Trim();
            if (fam == "ME")
            {
                return await ObtenerSalidasEmpaque(idInsumo, idSede);
            }
            if (fam == "PT")
            {
                return await ObtenerSalidasPT(idInsumo, idSede);
            }
            if (fam == "ECO")
            {
                return await ObtenerSalidasEconomato(idInsumo, idSede);
            }
            if (fam == "PI")
            {
                return await ObtenerSalidasPI(idInsumo, idSede);
            }

            var resultado = await ObtenerSalidasInsumoBase(idInsumo, idSede);
            if (!resultado.Any() && string.IsNullOrEmpty(fam))
            {
                if (await _context.Insumos.AnyAsync(i => i.Id == idInsumo && i.Clasificacion == "PI"))
                {
                    return await ObtenerSalidasPI(idInsumo, idSede);
                }
                if (await _context.Empaques.AnyAsync(e => e.Id == idInsumo))
                {
                    return await ObtenerSalidasEmpaque(idInsumo, idSede);
                }
                if (await _context.Productos.AnyAsync(p => p.Id == idInsumo))
                {
                    return await ObtenerSalidasPT(idInsumo, idSede);
                }
                if (await _context.Economatos.AnyAsync(ec => ec.Id == idInsumo))
                {
                    return await ObtenerSalidasEconomato(idInsumo, idSede);
                }
            }
            return resultado;
        }

        private async Task<List<SalidaInsumoRes>> ObtenerSalidasInsumoBase(int idInsumo, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();

            try
            {
                // Obtener lotes disponibles del insumo en esta sede para asignar origen a las salidas
                var lotesDisponibles = await _context.CompraInsumos
                    .Include(ci => ci.Compra)
                    .Include(ci => ci.NotaSalidaInsumos)
                        .ThenInclude(nsi => nsi.NotaSalida)
                    .Where(w => w.IdInsumo == idInsumo &&
                        (
                            (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                            w.NotaSalidaInsumos.Any(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede && (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                        )
                    )
                    .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                    .ToListAsync();

                var batchTrackers = lotesDisponibles.Select(ci => new
                {
                    Registro = "MP" + Alfanumerico.ConvertToBase36(ci.Id),
                    Lote = ci.Lote ?? "",
                    Capacidad = ((ci.Compra != null && ci.Compra.IdSede == idSede && (idSede == 15 || ci.Compra.FechaLab != null))
                        ? (ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0 ? (ci.CantidadRecibida.Value <= ci.CantidadSolicitada * 2 && ci.CantidadRecibida.Value < 500 && (ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? ci.CantidadRecibida.Value * 1000m : ci.CantidadRecibida.Value) : (ci.CantidadSolicitada * ((ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? 1000m : 1m)))
                        : 0m) +
                        ci.NotaSalidaInsumos
                            .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede && (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                            .Sum(nsi => ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad))
                }).Select(b => new { b.Registro, b.Lote, SaldoRestante = b.Capacidad }).ToList();

                // 1. Salidas por Productos Intermedios con detalle de stock
                var consumosStockPI = await _context.StockInsumoProductoIntermedios
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                    .Include(x => x.StockInsumo)
                        .ThenInclude(si => si.CompraInsumo)
                    .Where(x => x.InsumoProductoIntermedio.IdInsumo == idInsumo &&
                                x.InsumoProductoIntermedio.ProductoIntermedio.IdSede == idSede)
                    .OrderByDescending(x => x.InsumoProductoIntermedio.ProductoIntermedio.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "ELABORACIÓN PI",
                        RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.InsumoProductoIntermedio.ProductoIntermedio.Id),
                        DescripcionDestino = s.InsumoProductoIntermedio.ProductoIntermedio.Insumo != null
                            ? s.InsumoProductoIntermedio.ProductoIntermedio.Insumo.Descripcion
                            : (s.InsumoProductoIntermedio.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                        LoteInsumo = s.StockInsumo != null && s.StockInsumo.CompraInsumo != null ? (s.StockInsumo.CompraInsumo.Lote ?? "") : "",
                        RegistroLoteInsumo = s.StockInsumo != null && s.StockInsumo.CompraInsumo != null ? "MP" + Alfanumerico.ConvertToBase36(s.StockInsumo.CompraInsumo.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = s.UnidadMedida ?? s.InsumoProductoIntermedio.UnidadMedida ?? "G",
                        Fecha = s.InsumoProductoIntermedio.ProductoIntermedio.FechaCreacion,
                        Usuario = s.InsumoProductoIntermedio.ProductoIntermedio.Elaborador != null
                            ? s.InsumoProductoIntermedio.ProductoIntermedio.Elaborador.Codigo
                            : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (consumosStockPI != null && consumosStockPI.Any())
                {
                    resultado.AddRange(consumosStockPI);
                }
                else
                {
                    var consumosDirectosPI = await _context.InsumoProductoIntermedios
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                        .Where(x => x.IdInsumo == idInsumo && x.ProductoIntermedio.IdSede == idSede)
                        .OrderBy(x => x.ProductoIntermedio.FechaCreacion)
                        .Select(s => new SalidaInsumoRes
                        {
                            TipoSalida = "ELABORACIÓN PI",
                            RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.ProductoIntermedio.Id),
                            DescripcionDestino = s.ProductoIntermedio.Insumo != null
                                ? s.ProductoIntermedio.Insumo.Descripcion
                                : (s.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                            LoteInsumo = "",
                            RegistroLoteInsumo = "",
                            Cantidad = (s.Practica.HasValue && s.Practica.Value > 0) ? s.Practica.Value : s.CantidadLote,
                            Um = s.UnidadMedida ?? "G",
                            Fecha = s.ProductoIntermedio.FechaCreacion,
                            Usuario = s.ProductoIntermedio.Elaborador != null ? s.ProductoIntermedio.Elaborador.Codigo : "ADMIN"
                        })
                        .AsNoTracking()
                        .ToListAsync();

                    if (consumosDirectosPI != null)
                    {
                        resultado.AddRange(consumosDirectosPI);
                    }
                }

                // 2. Salidas por Notas de Salida
                var notasSalida = await _context.NotaSalidaInsumos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraInsumos)
                    .Where(x => x.CompraInsumos != null && x.CompraInsumos.IdInsumo == idInsumo && x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NS-" + Alfanumerico.ConvertToBase36(s.NotaSalida.Id)) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.CompraInsumos != null ? (s.CompraInsumos.Lote ?? "") : (s.Lote ?? ""),
                        RegistroLoteInsumo = s.CompraInsumos != null ? "MP" + Alfanumerico.ConvertToBase36(s.CompraInsumos.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = s.Um ?? (s.CompraInsumos != null ? s.CompraInsumos.Um : "G"),
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }

                // 3. Salidas por Fórmulas Magistrales
                var formulas = await _context.FormulasCC
                    .Include(x => x.Formula)
                        .ThenInclude(f => f.Creador)
                    .Include(x => x.Insumo)
                    .Where(x => x.InsumoId == idInsumo && (x.SedeId == idSede || (x.Formula != null && x.Formula.SedeId == idSede)))
                    .OrderBy(x => x.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "FÓRMULA MAGISTRAL",
                        RegistroDestino = s.Formula != null ? ("FM-" + s.Formula.Id) : "FM",
                        DescripcionDestino = s.Formula != null ? (s.Formula.FormulaMagistral ?? ("Fórmula #" + s.Formula.Id)) : "Fórmula",
                        LoteInsumo = s.Formula != null ? (s.Formula.Lote ?? "") : "",
                        RegistroLoteInsumo = "",
                        Cantidad = s.CantidadL,
                        Um = s.Insumo != null ? s.Insumo.UnidadMedida : "G",
                        Fecha = s.Formula != null ? s.Formula.FechaCreacion : s.FechaCreacion,
                        Usuario = s.Formula != null && s.Formula.Creador != null ? s.Formula.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (formulas != null && formulas.Any())
                {
                    resultado.AddRange(formulas);
                }

                // Asignar RegistroLoteInsumo y LoteInsumo a las salidas locales (FM o PI directos) en orden FIFO
                if (batchTrackers.Any())
                {
                    int currentBatchIdx = 0;
                    decimal currentBatchRemaining = batchTrackers[0].SaldoRestante;

                    var salidasLocales = resultado
                        .Where(r => string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .OrderBy(r => r.Fecha)
                        .ToList();

                    foreach (var s in salidasLocales)
                    {
                        decimal cant = s.Cantidad;
                        while (currentBatchRemaining <= 0 && currentBatchIdx < batchTrackers.Count - 1)
                        {
                            currentBatchIdx++;
                            currentBatchRemaining = batchTrackers[currentBatchIdx].SaldoRestante;
                        }

                        var activeBatch = batchTrackers[currentBatchIdx];
                        s.RegistroLoteInsumo = activeBatch.Registro;
                        if (!string.IsNullOrEmpty(activeBatch.Lote))
                        {
                            s.LoteInsumo = activeBatch.Lote;
                        }
                        currentBatchRemaining -= cant;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasInsumo: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        
        public async Task<List<SalidaInsumoRes>> ObtenerSalidasEmpaque(int empaqueId, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();

            try
            {
                // 1. Obtener lotes disponibles del empaque en esta sede para rastrear origen FIFO
                var compras = await _context.CompraEmpaques
                    .Include(w => w.Compra)
                    .Include(w => w.NotaSalidaEmpaques)
                        .ThenInclude(nse => nse.NotaSalida)
                    .Where(w => w.IdEmpaque == empaqueId &&
                        (
                            (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                            w.NotaSalidaEmpaques.Any(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede &&
                                (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || nse.CantidadRecibida > 0))
                        )
                    )
                    .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                    .ToListAsync();

                var batchTrackers = compras.Select(ce => new
                {
                    Registro = "ME" + Alfanumerico.ConvertToBase36(ce.Id),
                    Lote = ce.Lote ?? "",
                    Capacidad = ((ce.Compra != null && ce.Compra.IdSede == idSede && (idSede == 15 || ce.Compra.FechaLab != null))
                        ? (ce.CantidadRecibida.HasValue && ce.CantidadRecibida.Value > 0 ? ce.CantidadRecibida.Value : ce.CantidadSolicitada)
                        : 0m) +
                        ce.NotaSalidaEmpaques
                            .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede &&
                                (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || nse.CantidadRecibida > 0))
                            .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad))
                }).Select(b => new { b.Registro, b.Lote, SaldoRestante = b.Capacidad }).ToList();

                // 2. Salidas por Laboratorios (Fórmulas Magistrales)
                var laboratorios = await _context.Laboratorios
                    .Include(l => l.Formula)
                    .Include(l => l.ElaboradoU)
                    .Where(l => l.EmpaqueId == empaqueId && l.SedeId == idSede)
                    .OrderByDescending(l => l.FechaCreacion)
                    .Select(l => new SalidaInsumoRes
                    {
                        TipoSalida = "FÓRMULA MAGISTRAL",
                        RegistroDestino = l.Formula != null ? ("FM-" + l.Formula.Id) : ("LAB-" + (l.Id ?? 0)),
                        DescripcionDestino = l.Formula != null
                            ? (l.Formula.FormulaMagistral ?? ("Fórmula #" + l.Formula.Id))
                            : (!string.IsNullOrEmpty(l.Procedimiento) ? l.Procedimiento : "Laboratorio"),
                        LoteInsumo = l.Formula != null ? (l.Formula.Lote ?? "") : "",
                        RegistroLoteInsumo = "",
                        Cantidad = 1,
                        Um = "UND",
                        Fecha = l.FechaCreacion,
                        Usuario = l.ElaboradoU != null ? l.ElaboradoU.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (laboratorios != null && laboratorios.Any())
                {
                    resultado.AddRange(laboratorios);
                }

                // 3. Salidas por Consumo en Producto Intermedio (PI)
                var consumosPI = await _context.StockEmpaqueProductoIntermedios
                    .Include(x => x.EmpaqueProductoIntermedio)
                        .ThenInclude(epi => epi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                    .Include(x => x.EmpaqueProductoIntermedio)
                        .ThenInclude(epi => epi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                    .Include(x => x.StockEmpaque)
                        .ThenInclude(se => se.CompraEmpaque)
                    .Where(x => x.EmpaqueProductoIntermedio.IdEmpaque == empaqueId &&
                                x.EmpaqueProductoIntermedio.ProductoIntermedio != null &&
                                x.EmpaqueProductoIntermedio.ProductoIntermedio.IdSede == idSede)
                    .OrderByDescending(x => x.EmpaqueProductoIntermedio.ProductoIntermedio.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "ELABORACIÓN PI",
                        RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.EmpaqueProductoIntermedio.ProductoIntermedio.Id),
                        DescripcionDestino = s.EmpaqueProductoIntermedio.ProductoIntermedio.Insumo != null
                            ? s.EmpaqueProductoIntermedio.ProductoIntermedio.Insumo.Descripcion
                            : (s.EmpaqueProductoIntermedio.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                        LoteInsumo = s.StockEmpaque != null && s.StockEmpaque.CompraEmpaque != null ? (s.StockEmpaque.CompraEmpaque.Lote ?? "") : "",
                        RegistroLoteInsumo = s.StockEmpaque != null && s.StockEmpaque.CompraEmpaque != null ? "ME" + Alfanumerico.ConvertToBase36(s.StockEmpaque.CompraEmpaque.Id) : "",
                        Cantidad = s.Cantidad > 0 ? s.Cantidad : 1,
                        Um = s.UnidadMedida ?? "UND",
                        Fecha = s.EmpaqueProductoIntermedio.ProductoIntermedio.FechaCreacion,
                        Usuario = s.EmpaqueProductoIntermedio.ProductoIntermedio.Elaborador != null
                            ? s.EmpaqueProductoIntermedio.ProductoIntermedio.Elaborador.Codigo
                            : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (consumosPI != null && consumosPI.Any())
                {
                    resultado.AddRange(consumosPI);
                }
                else
                {
                    var consumosDirectosPI = await _context.EmpaqueProductoIntermedios
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                        .Where(x => x.IdEmpaque == empaqueId && x.ProductoIntermedio != null && x.ProductoIntermedio.IdSede == idSede)
                        .OrderByDescending(x => x.ProductoIntermedio.FechaCreacion)
                        .Select(s => new SalidaInsumoRes
                        {
                            TipoSalida = "ELABORACIÓN PI",
                            RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.ProductoIntermedio.Id),
                            DescripcionDestino = s.ProductoIntermedio.Insumo != null
                                ? s.ProductoIntermedio.Insumo.Descripcion
                                : (s.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                            LoteInsumo = "",
                            RegistroLoteInsumo = "",
                            Cantidad = 1,
                            Um = "UND",
                            Fecha = s.ProductoIntermedio.FechaCreacion,
                            Usuario = s.ProductoIntermedio.Elaborador != null ? s.ProductoIntermedio.Elaborador.Codigo : "ADMIN"
                        })
                        .AsNoTracking()
                        .ToListAsync();

                    if (consumosDirectosPI != null && consumosDirectosPI.Any())
                    {
                        resultado.AddRange(consumosDirectosPI);
                    }
                }

                // 4. Salidas por Notas de Salida de Empaques
                var notasSalida = await _context.NotaSalidaEmpaques
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraEmpaques)
                    .Where(x => x.CompraEmpaques != null && x.CompraEmpaques.IdEmpaque == empaqueId &&
                                x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NS-" + Alfanumerico.ConvertToBase36(s.NotaSalida.Id)) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.CompraEmpaques != null ? (s.CompraEmpaques.Lote ?? "") : (s.Lote ?? ""),
                        RegistroLoteInsumo = s.CompraEmpaques != null ? "ME" + Alfanumerico.ConvertToBase36(s.CompraEmpaques.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = s.Um ?? "UND",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }

                // 5. Asignar RegistroLoteInsumo y LoteInsumo por FIFO para las salidas locales sin lote asignado
                if (batchTrackers.Any())
                {
                    int currentBatchIdx = 0;
                    decimal currentBatchRemaining = batchTrackers[0].SaldoRestante;

                    var salidasLocales = resultado
                        .Where(r => string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .OrderBy(r => r.Fecha)
                        .ToList();

                    foreach (var s in salidasLocales)
                    {
                        decimal cant = s.Cantidad;
                        while (currentBatchRemaining <= 0 && currentBatchIdx < batchTrackers.Count - 1)
                        {
                            currentBatchIdx++;
                            currentBatchRemaining = batchTrackers[currentBatchIdx].SaldoRestante;
                        }

                        var activeBatch = batchTrackers[currentBatchIdx];
                        s.RegistroLoteInsumo = activeBatch.Registro;
                        if (!string.IsNullOrEmpty(activeBatch.Lote))
                        {
                            s.LoteInsumo = activeBatch.Lote;
                        }
                        currentBatchRemaining -= cant;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasEmpaque: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasPT(int productoId, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();
            try
            {
                var notasSalida = await _context.NotaSalidaProductos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraProducto)
                    .Where(x => x.CompraProducto != null && x.CompraProducto.IdProducto == productoId &&
                                x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NS-" + Alfanumerico.ConvertToBase36(s.NotaSalida.Id)) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.CompraProducto != null ? (s.CompraProducto.Lote ?? "") : (s.Lote ?? ""),
                        RegistroLoteInsumo = s.CompraProducto != null ? "PT" + Alfanumerico.ConvertToBase36(s.CompraProducto.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = "UND",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasPT: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasEconomato(int economatoId, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();
            try
            {
                var notasSalida = await _context.NotaSalidaEconomatos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraEconomato)
                    .Where(x => x.CompraEconomato != null && x.CompraEconomato.IdEconomato == economatoId &&
                                x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NS-" + Alfanumerico.ConvertToBase36(s.NotaSalida.Id)) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.Lote ?? "",
                        RegistroLoteInsumo = s.CompraEconomato != null ? "ECO" + Alfanumerico.ConvertToBase36(s.CompraEconomato.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = "UND",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasEconomato: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasPI(int idInsumo, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();
            try
            {
                // 1. Obtener lotes y referencias del producto intermedio
                var lotesDelInsumo = await _context.ProductosIntermedios.AsNoTracking()
                    .Where(p => p.IdInsumo == idInsumo && !string.IsNullOrEmpty(p.Lote))
                    .ToListAsync();

                var loteDict = lotesDelInsumo
                    .GroupBy(p => p.Lote!.Trim().ToUpper())
                    .ToDictionary(g => g.Key, g => g.First());

                // 2. Lotes disponibles en esta sede para rastreo FIFO
                var elaboraciones = await _context.ProductosIntermedios
                    .Where(w => w.IdInsumo == idInsumo && w.IdSede == idSede)
                    .OrderBy(w => w.FechaCreacion)
                    .ToListAsync();

                var batchTrackers = elaboraciones.Select(s => new
                {
                    Registro = "PI" + Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Capacidad = (s.TipoUso == "PI-FMG" || s.Um == "UND")
                        ? (s.LoteEstandar ?? 0)
                        : (s.LoteEstTotal ?? s.LoteEstandar ?? 0)
                }).Select(b => new { b.Registro, b.Lote, SaldoRestante = b.Capacidad }).ToList();

                var notasSalidaRecibidas = await _context.NotaSalidaInsumos.AsNoTracking()
                    .Include(nsi => nsi.NotaSalida)
                    .Where(nsi => !nsi.IdCompraInsumo.HasValue &&
                                  nsi.NotaSalida != null &&
                                  nsi.NotaSalida.IdSedeDestino == idSede &&
                                  (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                    .OrderBy(nsi => nsi.NotaSalida!.FechaRecepcion ?? nsi.NotaSalida!.FechaSalida)
                    .ToListAsync();

                foreach (var nsi in notasSalidaRecibidas)
                {
                    if (string.IsNullOrEmpty(nsi.Lote)) continue;
                    var key = nsi.Lote.Trim().ToUpper();
                    if (!loteDict.TryGetValue(key, out var piRef)) continue;

                    decimal cant = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) *
                        ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad);

                    batchTrackers.Add(new
                    {
                        Registro = "PI" + Alfanumerico.ConvertToBase36(piRef.Id),
                        Lote = nsi.Lote,
                        SaldoRestante = cant
                    });
                }

                // 3. Salidas por Notas de Salida (Transferencias/Retornos desde esta sede)
                var notasSalida = await _context.NotaSalidaInsumos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns!.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns!.Creador)
                    .Where(x => !x.IdCompraInsumo.HasValue &&
                                x.NotaSalida != null &&
                                x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida!.FechaCreacion)
                    .ToListAsync();

                foreach (var s in notasSalida)
                {
                    if (string.IsNullOrEmpty(s.Lote)) continue;
                    var key = s.Lote.Trim().ToUpper();
                    if (!loteDict.TryGetValue(key, out var piRef)) continue;

                    decimal cant = s.Cantidad;
                    string um = (s.Um ?? "G").Trim().ToUpper();
                    if (um == "KG" || um == "KILOGRAMOS" || um == "Kg") cant *= 1000m;

                    resultado.Add(new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NS-" + Alfanumerico.ConvertToBase36(s.NotaSalida.Id)) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envio a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.Lote ?? piRef.Lote ?? "",
                        RegistroLoteInsumo = "PI" + Alfanumerico.ConvertToBase36(piRef.Id),
                        Cantidad = cant,
                        Um = "G",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    });
                }

                // 4. Salidas por Consumo en Elaboracion de otros PI
                var consumosStockPI = await _context.StockInsumoProductoIntermedios
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                    .Include(x => x.StockInsumo)
                        .ThenInclude(si => si.ProductoIntermedio)
                    .Where(x => x.InsumoProductoIntermedio.IdInsumo == idInsumo &&
                                x.InsumoProductoIntermedio.ProductoIntermedio.IdSede == idSede)
                    .OrderBy(x => x.InsumoProductoIntermedio.ProductoIntermedio.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "ELABORACION PI",
                        RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.InsumoProductoIntermedio.ProductoIntermedio.Id),
                        DescripcionDestino = s.InsumoProductoIntermedio.ProductoIntermedio.Insumo != null
                            ? s.InsumoProductoIntermedio.ProductoIntermedio.Insumo.Descripcion
                            : (s.InsumoProductoIntermedio.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                        LoteInsumo = s.StockInsumo != null && s.StockInsumo.ProductoIntermedio != null ? (s.StockInsumo.ProductoIntermedio.Lote ?? "") : "",
                        RegistroLoteInsumo = s.StockInsumo != null && s.StockInsumo.ProductoIntermedio != null ? "PI" + Alfanumerico.ConvertToBase36(s.StockInsumo.ProductoIntermedio.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = s.UnidadMedida ?? s.InsumoProductoIntermedio.UnidadMedida ?? "G",
                        Fecha = s.InsumoProductoIntermedio.ProductoIntermedio.FechaCreacion,
                        Usuario = s.InsumoProductoIntermedio.ProductoIntermedio.Elaborador != null
                            ? s.InsumoProductoIntermedio.ProductoIntermedio.Elaborador.Codigo
                            : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (consumosStockPI != null && consumosStockPI.Any())
                {
                    resultado.AddRange(consumosStockPI);
                }
                else
                {
                    var consumosDirectosPI = await _context.InsumoProductoIntermedios
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                        .Where(x => x.IdInsumo == idInsumo && x.ProductoIntermedio.IdSede == idSede)
                        .OrderBy(x => x.ProductoIntermedio.FechaCreacion)
                        .Select(s => new SalidaInsumoRes
                        {
                            TipoSalida = "ELABORACION PI",
                            RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.ProductoIntermedio.Id),
                            DescripcionDestino = s.ProductoIntermedio.Insumo != null
                                ? s.ProductoIntermedio.Insumo.Descripcion
                                : (s.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                            LoteInsumo = "",
                            RegistroLoteInsumo = "",
                            Cantidad = (s.Practica.HasValue && s.Practica.Value > 0) ? s.Practica.Value : s.CantidadLote,
                            Um = s.UnidadMedida ?? "G",
                            Fecha = s.ProductoIntermedio.FechaCreacion,
                            Usuario = s.ProductoIntermedio.Elaborador != null ? s.ProductoIntermedio.Elaborador.Codigo : "ADMIN"
                        })
                        .AsNoTracking()
                        .ToListAsync();

                    if (consumosDirectosPI != null)
                    {
                        resultado.AddRange(consumosDirectosPI);
                    }
                }

                // 5. Salidas por Formulas Magistrales
                var formulas = await _context.FormulasCC
                    .Include(x => x.Formula)
                        .ThenInclude(f => f.Creador)
                    .Include(x => x.Insumo)
                    .Where(x => x.InsumoId == idInsumo && (x.SedeId == idSede || (x.Formula != null && x.Formula.SedeId == idSede)))
                    .OrderBy(x => x.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "FORMULA MAGISTRAL",
                        RegistroDestino = s.Formula != null ? ("FM-" + s.Formula.Id) : "FM",
                        DescripcionDestino = s.Formula != null ? (s.Formula.FormulaMagistral ?? ("Formula #" + s.Formula.Id)) : "Formula",
                        LoteInsumo = s.Formula != null ? (s.Formula.Lote ?? "") : "",
                        RegistroLoteInsumo = "",
                        Cantidad = s.CantidadL,
                        Um = s.Insumo != null ? s.Insumo.UnidadMedida : "G",
                        Fecha = s.Formula != null ? s.Formula.FechaCreacion : s.FechaCreacion,
                        Usuario = s.Formula != null && s.Formula.Creador != null ? s.Formula.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (formulas != null && formulas.Any())
                {
                    resultado.AddRange(formulas);
                }

                // 6. Asignar RegistroLoteInsumo y LoteInsumo por FIFO para las salidas locales
                if (batchTrackers.Any())
                {
                    int currentBatchIdx = 0;
                    decimal currentBatchRemaining = batchTrackers[0].SaldoRestante;

                    var salidasLocales = resultado
                        .Where(r => string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .OrderBy(r => r.Fecha)
                        .ToList();

                    foreach (var s in salidasLocales)
                    {
                        decimal cant = s.Cantidad;
                        while (currentBatchRemaining <= 0 && currentBatchIdx < batchTrackers.Count - 1)
                        {
                            currentBatchIdx++;
                            currentBatchRemaining = batchTrackers[currentBatchIdx].SaldoRestante;
                        }

                        var activeBatch = batchTrackers[currentBatchIdx];
                        s.RegistroLoteInsumo = activeBatch.Registro;
                        if (string.IsNullOrEmpty(s.LoteInsumo))
                        {
                            s.LoteInsumo = activeBatch.Lote;
                        }

                        currentBatchRemaining -= cant;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasPI: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<BajaInsumoRes>> ObtenerBajasInsumo(int idInsumo, int idSede, string? familia = null)
        {
            var resultado = new List<BajaInsumoRes>();
            try
            {
                var ahora = DateTime.UtcNow;
                var fam = (familia ?? "").ToUpper().Trim();

                if (fam == "ME")
                {
                    var empaquesVencidos = await _context.CompraEmpaques
                        .Include(ce => ce.Empaque)
                        .Include(ce => ce.Compra)
                            .ThenInclude(c => c!.Proveedor)
                        .Include(ce => ce.Compra)
                            .ThenInclude(c => c!.Sede)
                        .Include(ce => ce.StockEmpaques)
                        .Where(ce =>
                            ce.IdEmpaque == idInsumo &&
                            ce.FechaVencimiento < ahora &&
                            ce.StockEmpaques != null &&
                            ce.StockEmpaques.Any(se => se.IdSede == idSede && se.StockDisponible > 0))
                        .OrderBy(ce => ce.FechaVencimiento)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var ce in empaquesVencidos)
                    {
                        var stockSede = ce.StockEmpaques?
                            .Where(se => se.IdSede == idSede)
                            .Sum(se => se.StockDisponible) ?? 0m;

                        if (stockSede <= 0) continue;

                        int diasVencido = 0;
                        if (ce.FechaVencimiento.HasValue)
                        {
                            diasVencido = Math.Max(0, (int)(ahora.Date - ce.FechaVencimiento.Value.Date).TotalDays);
                        }

                        string reg = "ME" + Alfanumerico.ConvertToBase36(ce.Id);

                        resultado.Add(new BajaInsumoRes
                        {
                            IdCompraInsumo = ce.Id,
                            Registro = reg,
                            IdInsumo = ce.IdEmpaque,
                            Codigo = ce.IdEmpaque.ToString(),
                            Descripcion = ce.Empaque?.Descripcion ?? "",
                            Lote = ce.Lote ?? "-",
                            CantidadBaja = stockSede,
                            Um = ce.Um ?? "UND",
                            FechaFabricacion = ce.FechaFabricacion,
                            FechaVencimiento = ce.FechaVencimiento,
                            DiasVencido = diasVencido,
                            Estado = "VENCIDO",
                            Proveedor = ce.Compra?.Proveedor?.Datos ?? "",
                            SedeOrigen = ce.Compra?.Sede?.Nombre ?? "",
                            DocumentoOrigen = ce.Compra?.NumeroComprobante ?? ce.Compra?.CodFacQBD ?? (ce.Compra != null && ce.Compra.Id != 0 ? $"OC-{ce.Compra.Id}" : ""),
                            Observacion = ce.Observacion
                        });
                    }

                    return resultado;
                }

                if (fam == "PT")
                {
                    var productosVencidos = await _context.CompraProductos
                        .Include(cp => cp.Producto)
                        .Include(cp => cp.Compra)
                            .ThenInclude(c => c!.Proveedor)
                        .Include(cp => cp.Compra)
                            .ThenInclude(c => c!.Sede)
                        .Include(cp => cp.StockProductoTerminados)
                        .Where(cp =>
                            cp.IdProducto == idInsumo &&
                            cp.FechaVencimiento < ahora &&
                            cp.StockProductoTerminados.Any(sp => sp.IdSede == idSede && sp.StockDisponible > 0))
                        .OrderBy(cp => cp.FechaVencimiento)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var cp in productosVencidos)
                    {
                        var stockSede = cp.StockProductoTerminados
                            .Where(sp => sp.IdSede == idSede)
                            .Sum(sp => sp.StockDisponible);

                        if (stockSede <= 0) continue;

                        int diasVencido = 0;
                        if (cp.FechaVencimiento.HasValue)
                        {
                            diasVencido = Math.Max(0, (int)(ahora.Date - cp.FechaVencimiento.Value.Date).TotalDays);
                        }

                        string reg = "PT" + Alfanumerico.ConvertToBase36(cp.Id);

                        resultado.Add(new BajaInsumoRes
                        {
                            IdCompraInsumo = cp.Id,
                            Registro = reg,
                            IdInsumo = cp.IdProducto,
                            Codigo = cp.IdProducto.ToString(),
                            Descripcion = cp.Producto?.Descripcion ?? "",
                            Lote = cp.Lote ?? "-",
                            CantidadBaja = stockSede,
                            Um = cp.Um ?? "UND",
                            FechaFabricacion = cp.FechaFabricacion,
                            FechaVencimiento = cp.FechaVencimiento,
                            DiasVencido = diasVencido,
                            Estado = "VENCIDO",
                            Proveedor = cp.Compra?.Proveedor?.Datos ?? "",
                            SedeOrigen = cp.Compra?.Sede?.Nombre ?? "",
                            DocumentoOrigen = cp.Compra?.NumeroComprobante ?? (cp.Compra != null && cp.Compra.Id != 0 ? $"OC-{cp.Compra.Id}" : ""),
                            Observacion = cp.Observacion
                        });
                    }

                    return resultado;
                }

                if (fam == "PI")
                {
                    var pisVencidos = await _context.ProductosIntermedios
                        .Include(pi => pi.Insumo)
                        .Include(pi => pi.StockInsumo)
                        .Include(pi => pi.Sede)
                        .Where(pi =>
                            (pi.IdInsumo == idInsumo || pi.Id == idInsumo) &&
                            pi.IdSede == idSede &&
                            pi.FechaVencimiento < ahora &&
                            pi.StockInsumo != null &&
                            pi.StockInsumo.StockDisponible > 0)
                        .OrderBy(pi => pi.FechaVencimiento)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var pi in pisVencidos)
                    {
                        var stockSede = pi.StockInsumo?.StockDisponible ?? 0m;
                        if (stockSede <= 0) continue;

                        int diasVencido = 0;
                        if (pi.FechaVencimiento.HasValue)
                        {
                            diasVencido = Math.Max(0, (int)(ahora.Date - pi.FechaVencimiento.Value.Date).TotalDays);
                        }

                        string reg = "PI" + Alfanumerico.ConvertToBase36(pi.Id);

                        resultado.Add(new BajaInsumoRes
                        {
                            IdCompraInsumo = pi.Id,
                            Registro = reg,
                            IdInsumo = pi.IdInsumo,
                            Codigo = $"PI-QbD-{pi.IdInsumo}",
                            Descripcion = pi.Insumo?.Descripcion ?? "",
                            Lote = pi.Lote ?? "-",
                            CantidadBaja = stockSede,
                            Um = pi.Um ?? "G",
                            FechaFabricacion = pi.FechaEmision ?? pi.FechaCreacion,
                            FechaVencimiento = pi.FechaVencimiento,
                            DiasVencido = diasVencido,
                            Estado = "VENCIDO",
                            Proveedor = "PRODUCCIÓN INTERNA",
                            SedeOrigen = pi.Sede?.Nombre ?? "",
                            DocumentoOrigen = $"PI-{pi.Id}",
                            Observacion = pi.Procedimiento
                        });
                    }

                    return resultado;
                }

                if (fam == "ECO")
                {
                    return resultado;
                }

                // Default: MP (Materia Prima)
                var comprasVencidas = await _context.CompraInsumos
                    .Include(ci => ci.Insumo)
                    .Include(ci => ci.Compra)
                        .ThenInclude(c => c!.Proveedor)
                    .Include(ci => ci.Compra)
                        .ThenInclude(c => c!.Sede)
                    .Include(ci => ci.StockInsumos)
                    .Where(ci =>
                        ci.IdInsumo == idInsumo &&
                        ci.FechaVencimiento < ahora &&
                        ci.StockInsumos.Any(si => si.IdSede == idSede && si.StockDisponible > 0))
                    .OrderBy(ci => ci.FechaVencimiento)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var ci in comprasVencidas)
                {
                    var stockSede = ci.StockInsumos
                        .Where(si => si.IdSede == idSede)
                        .Sum(si => si.StockDisponible);

                    if (stockSede <= 0) continue;

                    int diasVencido = 0;
                    if (ci.FechaVencimiento.HasValue)
                    {
                        diasVencido = (int)(ahora.Date - ci.FechaVencimiento.Value.Date).TotalDays;
                        if (diasVencido < 0) diasVencido = 0;
                    }

                    string reg = "MP" + Alfanumerico.ConvertToBase36(ci.Id);

                    decimal cantidadBajaReal = stockSede;
                    bool esKgBaja = ci.Um != null && (
                        string.Equals(ci.Um.Trim(), "KG", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ci.Um.Trim(), "KILOGRAMO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ci.Um.Trim(), "KILOGRAMOS", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ci.Um.Trim(), "KGS", StringComparison.OrdinalIgnoreCase)
                    );
                    decimal entradasCI = (ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0)
                        ? (esKgBaja && ci.CantidadRecibida.Value <= ci.CantidadSolicitada * 2 && ci.CantidadRecibida.Value < 500 ? ci.CantidadRecibida.Value * 1000m : ci.CantidadRecibida.Value)
                        : (esKgBaja ? ci.CantidadSolicitada * 1000m : ci.CantidadSolicitada);

                    if (entradasCI > 0 && cantidadBajaReal > entradasCI * 1.5m && cantidadBajaReal >= 1000m)
                    {
                        if (cantidadBajaReal / 1000m <= entradasCI * 1.1m)
                        {
                            cantidadBajaReal /= 1000m;
                        }
                    }

                    resultado.Add(new BajaInsumoRes
                    {
                        IdCompraInsumo = ci.Id,
                        Registro = reg,
                        IdInsumo = ci.IdInsumo,
                        Codigo = $"MP-QbD-{ci.IdInsumo}",
                        Descripcion = ci.Insumo?.Descripcion ?? "",
                        Lote = ci.Lote ?? "-",
                        CantidadBaja = cantidadBajaReal,
                        Um = ci.Insumo?.UnidadMedida ?? (ci.StockInsumos.FirstOrDefault(si => si.IdSede == idSede)?.UnidadMedida ?? "G"),
                        FechaFabricacion = ci.FechaFabricacion,
                        FechaVencimiento = ci.FechaVencimiento,
                        DiasVencido = diasVencido,
                        Estado = "VENCIDO",
                        Proveedor = ci.Compra?.Proveedor?.Datos ?? "",
                        SedeOrigen = ci.Compra?.Sede?.Nombre ?? "",
                        DocumentoOrigen = ci.Compra?.NumeroComprobante ?? ci.Compra?.CodFacQBD ?? (ci.Compra != null && ci.Compra.Id != 0 ? $"OC-{ci.Compra.Id}" : ""),
                        Observacion = ci.Observacion
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener bajas de insumo {IdInsumo} en sede {IdSede} (Familia: {Familia})", idInsumo, idSede, familia);
            }

            return resultado;
        }

        public async Task<object> AssignLocation(AssignLocationReq request)
        {
            string fam = (request.Familia ?? "MP").ToUpper().Trim();

            if (fam == "ME")
            {
                var empaqueSede = await _context.EmpaqueSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEmpaque == request.IdInsumo);

                if (empaqueSede == null)
                {
                    empaqueSede = new EmpaqueSede
                    {
                        IdSite = request.IdSede,
                        IdEmpaque = request.IdInsumo,
                        Location = request.Ubicacion
                    };
                    await _context.EmpaqueSupplies.AddAsync(empaqueSede);
                }
                else
                {
                    empaqueSede.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return empaqueSede;
            }
            else if (fam == "PT")
            {
                var productoSede = await _context.ProductSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdProducto == request.IdInsumo);

                if (productoSede == null)
                {
                    productoSede = new ProductoSede
                    {
                        IdSite = request.IdSede,
                        IdProducto = request.IdInsumo,
                        Location = request.Ubicacion
                    };
                    await _context.ProductSupplies.AddAsync(productoSede);
                }
                else
                {
                    productoSede.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return productoSede;
            }
            else if (fam == "ECO")
            {
                var economatoSede = await _context.EconomatoSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEconomato == request.IdInsumo);

                if (economatoSede == null)
                {
                    economatoSede = new EconomatoSede
                    {
                        IdSite = request.IdSede,
                        IdEconomato = request.IdInsumo,
                        Location = request.Ubicacion
                    };
                    await _context.EconomatoSupplies.AddAsync(economatoSede);
                }
                else
                {
                    economatoSede.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return economatoSede;
            }
            else
            {
                // Default a MP / PI (SiteSupply)
                SiteSupply? siteSupply = await _repository.GetSedeSupplyAsync(request.IdInsumo, request.IdSede);

                if (siteSupply == null)
                {
                    siteSupply = new SiteSupply
                    {
                        IdSite = request.IdSede,
                        IdSupply = request.IdInsumo,
                        Location = request.Ubicacion
                    };

                    siteSupply = await _repository.CreateLocationBySiteAsync(siteSupply);
                }
                else
                {
                    siteSupply.Location = request.Ubicacion;
                }

                await _unitWork.SaveChangesAsync();
                return siteSupply;
            }
        }

        public async Task<object> AssignLimite(AssignLimiteReq request)
        {
            string fam = (request.Familia ?? "MP").ToUpper().Trim();

            if (fam == "ME")
            {
                var empaqueSede = await _context.EmpaqueSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEmpaque == request.IdInsumo);

                if (empaqueSede == null)
                {
                    empaqueSede = new EmpaqueSede
                    {
                        IdSite = request.IdSede,
                        IdEmpaque = request.IdInsumo,
                        Limite = request.Limite
                    };
                    await _context.EmpaqueSupplies.AddAsync(empaqueSede);
                }
                else
                {
                    empaqueSede.Limite = request.Limite;
                    empaqueSede.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return empaqueSede;
            }
            else if (fam == "PT")
            {
                var productoSede = await _context.ProductSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdProducto == request.IdInsumo);

                if (productoSede == null)
                {
                    productoSede = new ProductoSede
                    {
                        IdSite = request.IdSede,
                        IdProducto = request.IdInsumo,
                        Limite = request.Limite
                    };
                    await _context.ProductSupplies.AddAsync(productoSede);
                }
                else
                {
                    productoSede.Limite = request.Limite;
                    productoSede.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return productoSede;
            }
            else if (fam == "ECO")
            {
                var economatoSede = await _context.EconomatoSupplies
                    .FirstOrDefaultAsync(w => w.IdSite == request.IdSede && w.IdEconomato == request.IdInsumo);

                if (economatoSede == null)
                {
                    economatoSede = new EconomatoSede
                    {
                        IdSite = request.IdSede,
                        IdEconomato = request.IdInsumo,
                        Limite = request.Limite
                    };
                    await _context.EconomatoSupplies.AddAsync(economatoSede);
                }
                else
                {
                    economatoSede.Limite = request.Limite;
                    economatoSede.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return economatoSede;
            }
            else
            {
                // Default a MP / PI (SiteSupply)
                SiteSupply? siteSupply = await _repository.GetSedeSupplyAsync(request.IdInsumo, request.IdSede);

                if (siteSupply == null)
                {
                    siteSupply = new SiteSupply
                    {
                        IdSite = request.IdSede,
                        IdSupply = request.IdInsumo,
                        Limite = request.Limite
                    };

                    siteSupply = await _repository.CreateLocationBySiteAsync(siteSupply);
                }
                else
                {
                    siteSupply.Limite = request.Limite;
                    siteSupply.CantidadEnviar = request.CantidadEnviar;
                }

                await _unitWork.SaveChangesAsync();
                return siteSupply;
            }
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
                            listaLotes = await ObtenerDetallePI(item.IdInsumo, idSede);
                            if (listaLotes == null || !listaLotes.Any(l => (l.StockDisponible ?? l.Saldo ?? 0) > 0))
                            {
                                var fallbackMp = await ObtenerDetalleInsumo(item.IdInsumo, idSede);
                                if (fallbackMp != null && fallbackMp.Any(l => (l.StockDisponible ?? l.Saldo ?? 0) > 0))
                                {
                                    listaLotes = fallbackMp;
                                    tipoDetectado = "MP";
                                }
                            }
                        }
                        else
                        {
                            listaLotes = await ObtenerDetalleInsumo(item.IdInsumo, idSede);
                            if (listaLotes == null || !listaLotes.Any(l => (l.StockDisponible ?? l.Saldo ?? 0) > 0))
                            {
                                var fallbackPi = await ObtenerDetallePI(item.IdInsumo, idSede);
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
        private static string? ExtraerNumeroFactura(string? serie, string? numero, string? codFac)
        {
            serie = serie?.Trim();
            numero = numero?.Trim();
            codFac = codFac?.Trim();

            if (!string.IsNullOrEmpty(serie) && !string.IsNullOrEmpty(numero))
                return $"{serie}-{numero}";
            if (!string.IsNullOrEmpty(serie))
                return serie;
            if (!string.IsNullOrEmpty(numero))
                return numero;
            if (!string.IsNullOrEmpty(codFac))
                return codFac;
            return null;
        }
    }
}

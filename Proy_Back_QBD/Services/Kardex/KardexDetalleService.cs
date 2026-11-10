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
using proy_back_Qbd.Services.Kardex.Ledger;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Kardex
{
    /// <summary>Detalle por lote (entradas, salidas y saldo) de cada familia.</summary>
    public class KardexDetalleService : IKardexDetalleService
    {
        private readonly ApiContext _context;
        private readonly IKardexMovimientoService _movimientos;
        private readonly ILogger<KardexDetalleService> _logger;

        public KardexDetalleService(ApiContext context, IKardexMovimientoService movimientos, ILogger<KardexDetalleService> logger)
        {
            _context = context;
            _movimientos = movimientos;
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

            // Salidas, ajustes, bajas y saldo de cada lote salen del libro mayor (fuente única de stock)
            var mayor = await new KardexLedgerMpService(_context).CalcularInsumoAsync(idSede, idInsumo);
            LedgerParte? ParteDe(int idCompra, int? idNsi) => mayor.Partes.FirstOrDefault(p =>
                p.IdCompraInsumo == idCompra && (idNsi.HasValue ? p.IdNotaSalidaInsumo == idNsi : p.IdNotaSalidaInsumo == null));
            var partesMostradas = new HashSet<LedgerParte>();

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

                var stockSede = compraInsumo.StockInsumos
                    .Where(si => si.IdSede == idSede)
                    .ToList();

                var ajustesDeCompra = ajustesPorCompra.ContainsKey(compraInsumo.Id) ? ajustesPorCompra[compraInsumo.Id] : new List<AjusteInsumo>();

                string umDefecto = stockSede.FirstOrDefault()?.UnidadMedida ?? (compraInsumo.Insumo != null ? compraInsumo.Insumo.UnidadMedida : (compraInsumo.Um ?? "G"));
                string? numeroFacturaCompra = ExtraerNumeroFactura(compraInsumo.Compra?.SerieComprobante, compraInsumo.Compra?.NumeroComprobante, compraInsumo.Compra?.CodFacQBD);


                // 1. ENTRADA POR COMPRA (si corresponde a esta sede)
                if (esCompraEstaSede && entradasCompra > 0)
                {
                    var stockPrincipal = stockSede.FirstOrDefault(w => w.IdNotaSalidaInsumo == null || w.IdNotaSalidaInsumo == 0);
                    var parteCompra = ParteDe(compraInsumo.Id, null);
                    if (parteCompra != null) partesMostradas.Add(parteCompra);
                    decimal salidasCompra = parteCompra?.Salidas ?? 0m;
                    decimal totalAjuste = parteCompra?.Ajustes ?? 0m;
                    bool tieneAjuste = ajustesDeCompra.Any(a => a.StockInsumo == null || a.StockInsumo.IdNotaSalidaInsumo == null || a.StockInsumo.IdNotaSalidaInsumo == 0);
                    decimal saldoCompra = parteCompra?.Saldo ?? (entradasCompra - salidasCompra);

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
                        Baja = parteCompra?.Baja ?? 0m,
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

                    var parteNS = ParteDe(compraInsumo.Id, nsi.Id);
                    if (parteNS != null) partesMostradas.Add(parteNS);
                    decimal salidasNS = parteNS?.Salidas ?? 0m;

                    var stockNS = stockSede.FirstOrDefault(w => w.IdNotaSalidaInsumo == nsi.Id);
                    var ajustesNS = stockNS?.AjusteInsumos?.ToList();
                    bool tieneAjusteNS = ajustesNS != null && ajustesNS.Any();
                    decimal totalAjusteNS = parteNS?.Ajustes ?? 0m;

                    decimal saldoNS = parteNS?.Saldo ?? (cantRecibida - salidasNS);

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
                        Baja = parteNS?.Baja ?? 0m,
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

            // Partes del libro mayor sin fila propia (p. ej. ajuste sobre un lote sin ingreso en esta sede)
            foreach (var parte in mayor.Partes.Where(p => !partesMostradas.Contains(p)))
            {
                var ci = compraInsumos.FirstOrDefault(c => c.Id == parte.IdCompraInsumo);
                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "MP" + Alfanumerico.ConvertToBase36(parte.IdCompraInsumo),
                    Lote = ci?.Lote ?? "",
                    CantidadIngresada = parte.Entradas,
                    Salidas = parte.Salidas,
                    Ajuste = parte.Ajustes != 0 ? parte.Ajustes : (decimal?)null,
                    TieneAjuste = parte.Ajustes != 0,
                    Baja = parte.Baja,
                    Saldo = parte.Saldo,
                    StockDisponible = parte.Saldo,
                    Um = "G",
                    FechaVencimiento = ci?.FechaVencimiento,
                    TipoOrigen = "Ajuste",
                    IdCompra = parte.IdCompraInsumo
                });
            }

            // Salidas registradas sin ningún lote del insumo en esta sede: se muestran para que el total cuadre
            if (mayor.SalidasSinLote > 0)
            {
                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "-",
                    Lote = "SIN LOTE",
                    CantidadIngresada = 0m,
                    Salidas = mayor.SalidasSinLote,
                    Baja = 0m,
                    Saldo = -mayor.SalidasSinLote,
                    StockDisponible = -mayor.SalidasSinLote,
                    Um = "G",
                    TipoOrigen = "Sin lote",
                    Observacion = "Salidas sin lote de ingreso en esta sede"
                });
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

            // Salidas, ajustes, bajas y saldo de cada lote: libro mayor (fuente única de stock de ME)
            var mayor = await new KardexLedgerMeService(_context).CalcularEmpaqueAsync(idSede, empaqueId);
            LedgerParte? ParteDe(int idCompra, int? idNse) => mayor.Partes.FirstOrDefault(p =>
                p.IdCompraInsumo == idCompra && (idNse.HasValue ? p.IdNotaSalidaInsumo == idNse : p.IdNotaSalidaInsumo == null));
            var mostradas = new HashSet<LedgerParte>();

            var resultado = new List<DetalleEmpaqueRes>();

            foreach (var s in compras)
            {
                var registro = "ME" + Alfanumerico.ConvertToBase36(s.Id);

                // 1. ENTRADA POR ORDEN DE COMPRA (si esta sede fue quien compró a proveedor)
                bool esCompraEstaSede = s.Compra != null && s.Compra.IdSede == idSede && (idSede == 15 || s.Compra.FechaLab != null);
                decimal cantCompra = esCompraEstaSede ? (s.CantidadRecibida.HasValue && s.CantidadRecibida.Value > 0 ? s.CantidadRecibida.Value : s.CantidadSolicitada) : 0m;

                if (esCompraEstaSede && cantCompra > 0)
                {
                    var parte = ParteDe(s.Id, null);
                    if (parte != null) mostradas.Add(parte);

                    resultado.Add(new DetalleEmpaqueRes
                    {
                        Registro = registro,
                        Lote = s.Lote ?? "",
                        CantidadIngresada = cantCompra,
                        Salidas = parte?.Salidas ?? 0m,
                        Ajuste = parte != null && parte.Ajustes != 0 ? parte.Ajustes : (decimal?)null,
                        TieneAjuste = parte != null && parte.Ajustes != 0,
                        Baja = parte?.Baja ?? 0m,
                        Saldo = parte?.Saldo ?? cantCompra,
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
                    decimal cantRecibida = KardexLedgerMeService.CantidadNotaSalida(nse.Cantidad, nse.CantidadRecibida);
                    var parte = ParteDe(s.Id, nse.Id);
                    if (parte != null) mostradas.Add(parte);

                    resultado.Add(new DetalleEmpaqueRes
                    {
                        Registro = registro,
                        Lote = s.Lote ?? "",
                        CantidadIngresada = cantRecibida,
                        Salidas = parte?.Salidas ?? 0m,
                        Ajuste = parte != null && parte.Ajustes != 0 ? parte.Ajustes : (decimal?)null,
                        TieneAjuste = parte != null && parte.Ajustes != 0,
                        Baja = parte?.Baja ?? 0m,
                        Saldo = parte?.Saldo ?? cantRecibida,
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

            // Partes del libro mayor sin fila propia (ajuste sobre un lote sin ingreso en esta sede)
            foreach (var parte in mayor.Partes.Where(p => !mostradas.Contains(p)))
            {
                var ce = compras.FirstOrDefault(c => c.Id == parte.IdCompraInsumo);
                resultado.Add(new DetalleEmpaqueRes
                {
                    Registro = "ME" + Alfanumerico.ConvertToBase36(parte.IdCompraInsumo),
                    Lote = ce?.Lote ?? "",
                    CantidadIngresada = parte.Entradas,
                    Salidas = parte.Salidas,
                    Ajuste = parte.Ajustes != 0 ? parte.Ajustes : (decimal?)null,
                    TieneAjuste = parte.Ajustes != 0,
                    Baja = parte.Baja,
                    Saldo = parte.Saldo,
                    FechaVencimiento = ce?.FechaVencimiento,
                    TipoOrigen = "Ajuste",
                    IdCompra = parte.IdCompraInsumo
                });
            }

            if (mayor.SalidasSinLote > 0)
            {
                resultado.Add(new DetalleEmpaqueRes
                {
                    Registro = "-",
                    Lote = "SIN LOTE",
                    CantidadIngresada = 0m,
                    Salidas = mayor.SalidasSinLote,
                    Baja = 0m,
                    Saldo = -mayor.SalidasSinLote,
                    TipoOrigen = "Sin lote",
                    Observacion = "Salidas sin lote de ingreso en esta sede"
                });
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

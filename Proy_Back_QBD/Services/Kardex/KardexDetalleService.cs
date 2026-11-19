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
                        // Ajuste = lo que el libro mayor aplicó a este lote (igual que ME). Antes solo se mostraba si
                        // el ajuste estaba ligado a esta fila de stock, y en sedes que reciben por nota salía 0
                        // aunque el stock sí lo incluía.
                        Ajuste = totalAjuste != 0 ? totalAjuste : (tieneAjuste ? 0m : (decimal?)null),
                        TieneAjuste = tieneAjuste || totalAjuste != 0,
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
                        // Ajuste = lo que el libro mayor aplicó a este lote (los conteos se registran por registro,
                        // no por nota, así que no siempre están ligados a esta fila de stock)
                        Ajuste = totalAjusteNS != 0 ? totalAjusteNS : (tieneAjusteNS ? 0m : (decimal?)null),
                        TieneAjuste = tieneAjusteNS || totalAjusteNS != 0,
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
            // Saldo de cada lote: libro mayor de PI (misma regla que la lista de stock PI y que MP)
            var calc = await new KardexLedgerPiService(_context).CalcularAsync(idSede, new[] { idInsumo });
            var mayor = calc.PorInsumo.TryGetValue(idInsumo, out var m) ? m : new LedgerResultado();
            var resultado = new List<DetalleInsumoRes>();
            if (mayor.Partes.Count == 0 && mayor.SalidasSinLote <= 0) return resultado;

            // Observación del último ajuste de cada lote
            var idsAjuste = mayor.Conteos.Select(c => c.Id).ToList();
            var obsAjuste = await _context.AjusteInsumos.AsNoTracking()
                .Where(a => idsAjuste.Contains(a.Id))
                .Select(a => new { a.Id, a.Observacion })
                .ToDictionaryAsync(a => a.Id, a => a.Observacion);
            string? ObservacionDe(LedgerParte p) => mayor.Conteos
                .Where(c => c.IdCompraInsumo == p.IdCompraInsumo && c.IdNotaSalidaInsumo == p.IdNotaSalidaInsumo)
                .OrderByDescending(c => c.Fecha)
                .Select(c => obsAjuste.TryGetValue(c.Id, out var o) ? o : null)
                .FirstOrDefault(o => !string.IsNullOrEmpty(o));

            // Notas de salida recibidas (para CUO y sede de origen)
            var idsNs = mayor.Partes.Where(p => p.IdNotaSalidaInsumo.HasValue).Select(p => p.IdNotaSalidaInsumo!.Value).Distinct().ToList();
            var notas = await _context.NotaSalidaInsumos.AsNoTracking()
                .Where(n => idsNs.Contains(n.Id))
                .Select(n => new
                {
                    n.Id, n.Lote, n.Observacion,
                    IdNota = n.NotaSalida != null ? (int?)n.NotaSalida.Id : null,
                    SedeOrigen = n.NotaSalida != null && n.NotaSalida.SedeOrigen != null ? n.NotaSalida.SedeOrigen.Nombre : null
                })
                .ToDictionaryAsync(n => n.Id);

            // Compras del insumo (lotes comprados)
            var idsCompra = mayor.Partes.Where(p => p.IdCompraInsumo < 0).Select(p => -p.IdCompraInsumo).Distinct().ToList();
            var compras = await _context.CompraInsumos.AsNoTracking()
                .Where(c => idsCompra.Contains(c.Id))
                .Select(c => new { c.Id, c.Lote, c.FechaFabricacion, c.FechaVencimiento, c.Observacion, IdCompra = c.Compra != null ? (int?)c.Compra.Id : null })
                .ToDictionaryAsync(c => c.Id);

            foreach (var p in mayor.Partes.OrderByDescending(x => x.FechaIngreso ?? DateTime.MinValue))
            {
                bool esCompra = p.IdCompraInsumo < 0;
                calc.Lotes.TryGetValue(p.IdCompraInsumo, out var pi);
                var compra = esCompra && compras.TryGetValue(-p.IdCompraInsumo, out var cc) ? cc : null;
                var ns = p.IdNotaSalidaInsumo.HasValue && notas.TryGetValue(p.IdNotaSalidaInsumo.Value, out var nn) ? nn : null;
                bool tieneAjuste = mayor.Conteos.Any(c => c.IdCompraInsumo == p.IdCompraInsumo && c.IdNotaSalidaInsumo == p.IdNotaSalidaInsumo);

                var fila = new DetalleInsumoRes
                {
                    Registro = KardexLedgerPiService.Registro(p.IdCompraInsumo),
                    Lote = ns?.Lote ?? compra?.Lote ?? pi?.Lote ?? "",
                    CantidadIngresada = p.Entradas,
                    Salidas = p.Salidas,
                    Ajuste = tieneAjuste ? p.Ajustes : (decimal?)null,
                    TieneAjuste = tieneAjuste,
                    Baja = p.Baja,
                    Saldo = p.Saldo,
                    StockDisponible = p.Saldo,
                    FechaCompra = p.FechaIngreso,
                    FechaFabricacion = compra?.FechaFabricacion ?? pi?.FechaEmision ?? pi?.FechaCreacion,
                    FechaVencimiento = p.FechaVencimiento ?? compra?.FechaVencimiento ?? pi?.FechaVencimiento,
                    Observacion = ObservacionDe(p) ?? ns?.Observacion ?? compra?.Observacion ?? "",
                    IdCompra = esCompra ? -p.IdCompraInsumo : p.IdCompraInsumo
                };
                if (ns != null)
                {
                    fila.TipoOrigen = "Nota de Salida";
                    fila.SedeOrigen = ns.SedeOrigen ?? "CENTRAL";
                    fila.CUO = ns.IdNota.HasValue ? $"NT{ns.IdNota}" : null;
                    fila.DocumentoOrigen = fila.CUO;
                }
                else if (esCompra)
                {
                    fila.TipoOrigen = "Orden de Compra";
                    fila.DocumentoOrigen = compra?.IdCompra != null ? $"OC{compra.IdCompra}" : null;
                }
                else if (p.Entradas == 0 && tieneAjuste)
                {
                    fila.TipoOrigen = "Ajuste";
                }
                else
                {
                    // Lote elaborado en la sede. Sin TipoOrigen el front lo mostraba como COMPRA.
                    fila.TipoOrigen = "Elaboración PI";
                    fila.DocumentoOrigen = pi?.Lote;
                }
                resultado.Add(fila);
            }

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
                    TipoOrigen = "Sin lote",
                    Observacion = "Salidas sin lote de ingreso en esta sede"
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
            // Saldo de cada lote: libro mayor de PT (misma regla que la lista de stock PT)
            var mayor = await new KardexLedgerPtService(_context).CalcularProductoAsync(idSede, idProducto);
            var idsCompra = mayor.Partes.Select(p => p.IdCompraInsumo).Distinct().ToList();
            var compras = await _context.CompraProductos.AsNoTracking()
                .Where(c => idsCompra.Contains(c.Id))
                .Select(c => new
                {
                    c.Id, c.Lote, c.FechaFabricacion, c.FechaVencimiento, c.Observacion,
                    IdCompra = c.Compra != null ? (int?)c.Compra.Id : null,
                    Serie = c.Compra != null ? c.Compra.SerieComprobante : null,
                    Numero = c.Compra != null ? c.Compra.NumeroComprobante : null,
                    CodFac = c.Compra != null ? c.Compra.CodFacQBD : null,
                    Proveedor = c.Compra != null && c.Compra.Proveedor != null ? c.Compra.Proveedor.Datos : null,
                    Sede = c.Compra != null && c.Compra.Sede != null ? c.Compra.Sede.Nombre : null
                })
                .ToDictionaryAsync(c => c.Id);
            var idsNs = mayor.Partes.Where(p => p.IdNotaSalidaInsumo.HasValue).Select(p => p.IdNotaSalidaInsumo!.Value).ToList();
            var notas = await _context.NotaSalidaProductos.AsNoTracking()
                .Where(n => idsNs.Contains(n.Id))
                .Select(n => new
                {
                    n.Id, n.Observacion, n.IdNotaSalida,
                    ObsNota = n.NotaSalida != null ? n.NotaSalida.Observacion : null,
                    SedeOrigen = n.NotaSalida != null && n.NotaSalida.SedeOrigen != null ? n.NotaSalida.SedeOrigen.Nombre : null
                })
                .ToDictionaryAsync(n => n.Id);

            var resultado = new List<DetalleInsumoRes>();
            foreach (var p in mayor.Partes.OrderBy(x => x.FechaIngreso ?? DateTime.MinValue))
            {
                compras.TryGetValue(p.IdCompraInsumo, out var c);
                var ns = p.IdNotaSalidaInsumo.HasValue && notas.TryGetValue(p.IdNotaSalidaInsumo.Value, out var nn) ? nn : null;
                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "PT" + Alfanumerico.ConvertToBase36(p.IdCompraInsumo),
                    Lote = c?.Lote ?? "",
                    CantidadIngresada = p.Entradas,
                    Salidas = p.Salidas,
                    Ajuste = p.Ajustes != 0 ? p.Ajustes : (decimal?)null,
                    TieneAjuste = p.Ajustes != 0,
                    Baja = p.Baja,
                    Saldo = p.Saldo,
                    StockDisponible = p.Saldo,
                    FechaCompra = p.FechaIngreso,
                    FechaFabricacion = c?.FechaFabricacion,
                    FechaVencimiento = p.FechaVencimiento ?? c?.FechaVencimiento,
                    Observacion = ns != null ? (!string.IsNullOrEmpty(ns.Observacion) ? ns.Observacion : (ns.ObsNota ?? c?.Observacion ?? "")) : c?.Observacion,
                    TipoOrigen = ns != null ? "Nota de Salida" : (p.Entradas > 0 ? "Orden de Compra" : "Ajuste"),
                    SedeOrigen = ns != null ? (ns.SedeOrigen ?? "SEDE ORIGEN") : (c?.Proveedor ?? c?.Sede ?? "PROVEEDOR"),
                    DocumentoOrigen = ns != null ? $"NT{ns.IdNotaSalida}" : (c?.Numero ?? c?.CodFac ?? (c?.IdCompra != null ? $"OC-{c.IdCompra}" : "")),
                    NumeroFactura = ExtraerNumeroFactura(c?.Serie, c?.Numero, c?.CodFac),
                    IdCompra = p.IdCompraInsumo
                });
            }
            AgregarSinLote(resultado, mayor);
            return resultado;
        }

        public async Task<List<DetalleInsumoRes>> ObtenerDetalleEconomato(int idEconomato, int idSede)
        {
            // Saldo de cada lote: libro mayor de ECO (misma regla que la lista de stock ECO)
            var mayor = await new KardexLedgerEcoService(_context).CalcularEconomatoAsync(idSede, idEconomato);
            var idsCompra = mayor.Partes.Select(p => p.IdCompraInsumo).Distinct().ToList();
            var compras = await _context.CompraEconomatos.AsNoTracking()
                .Where(c => idsCompra.Contains(c.Id))
                .Select(c => new
                {
                    c.Id, c.Observacion, c.Um,
                    IdCompra = c.Compra != null ? (int?)c.Compra.Id : null,
                    Serie = c.Compra != null ? c.Compra.SerieComprobante : null,
                    Numero = c.Compra != null ? c.Compra.NumeroComprobante : null,
                    CodFac = c.Compra != null ? c.Compra.CodFacQBD : null,
                    Proveedor = c.Compra != null && c.Compra.Proveedor != null ? c.Compra.Proveedor.Datos : null,
                    Sede = c.Compra != null && c.Compra.Sede != null ? c.Compra.Sede.Nombre : null
                })
                .ToDictionaryAsync(c => c.Id);
            var idsNs = mayor.Partes.Where(p => p.IdNotaSalidaInsumo.HasValue).Select(p => p.IdNotaSalidaInsumo!.Value).ToList();
            var notas = await _context.NotaSalidaEconomatos.AsNoTracking()
                .Where(n => idsNs.Contains(n.Id))
                .Select(n => new
                {
                    n.Id, n.IdNotaSalida,
                    ObsNota = n.NotaSalida != null ? n.NotaSalida.Observacion : null,
                    SedeOrigen = n.NotaSalida != null && n.NotaSalida.SedeOrigen != null ? n.NotaSalida.SedeOrigen.Nombre : null
                })
                .ToDictionaryAsync(n => n.Id);

            var resultado = new List<DetalleInsumoRes>();
            foreach (var p in mayor.Partes.OrderBy(x => x.FechaIngreso ?? DateTime.MinValue))
            {
                compras.TryGetValue(p.IdCompraInsumo, out var c);
                var ns = p.IdNotaSalidaInsumo.HasValue && notas.TryGetValue(p.IdNotaSalidaInsumo.Value, out var nn) ? nn : null;
                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "ECO" + Alfanumerico.ConvertToBase36(p.IdCompraInsumo),
                    Lote = "",
                    CantidadIngresada = p.Entradas,
                    Salidas = p.Salidas,
                    Ajuste = p.Ajustes != 0 ? p.Ajustes : (decimal?)null,
                    TieneAjuste = p.Ajustes != 0,
                    Baja = p.Baja,
                    Saldo = p.Saldo,
                    StockDisponible = p.Saldo,
                    Um = c?.Um,
                    FechaCompra = p.FechaIngreso,
                    FechaVencimiento = p.FechaVencimiento,
                    Observacion = ns?.ObsNota ?? c?.Observacion ?? "",
                    TipoOrigen = ns != null ? "Nota de Salida" : (p.Entradas > 0 ? "Orden de Compra" : "Ajuste"),
                    SedeOrigen = ns != null ? (ns.SedeOrigen ?? "SEDE ORIGEN") : (c?.Proveedor ?? c?.Sede ?? "PROVEEDOR"),
                    DocumentoOrigen = ns != null ? $"NT{ns.IdNotaSalida}" : (c?.Numero ?? c?.CodFac ?? (c?.IdCompra != null ? $"OC-{c.IdCompra}" : "")),
                    NumeroFactura = ExtraerNumeroFactura(c?.Serie, c?.Numero, c?.CodFac),
                    IdCompra = p.IdCompraInsumo
                });
            }
            AgregarSinLote(resultado, mayor);
            return resultado;
        }

        /// <summary>Salidas sin ningún lote en la sede: se muestran para que el total cuadre con la lista.</summary>
        private static void AgregarSinLote(List<DetalleInsumoRes> resultado, LedgerResultado mayor)
        {
            if (mayor.SalidasSinLote <= 0) return;
            resultado.Add(new DetalleInsumoRes
            {
                Registro = "-",
                Lote = "SIN LOTE",
                CantidadIngresada = 0m,
                Salidas = mayor.SalidasSinLote,
                Baja = 0m,
                Saldo = -mayor.SalidasSinLote,
                StockDisponible = -mayor.SalidasSinLote,
                TipoOrigen = "Sin lote",
                Observacion = "Salidas sin lote de ingreso en esta sede"
            });
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

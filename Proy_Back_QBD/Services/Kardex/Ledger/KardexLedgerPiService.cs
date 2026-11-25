using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Util;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services.Kardex.Ledger
{
    /// <summary>
    /// Libro mayor (<see cref="KardexLedger"/>) de productos intermedios (insumos con Clasificacion "PI") en una sede.
    /// Misma regla que MP: el saldo de cada lote sale solo de sus movimientos y en orden de fecha.
    ///  - Entradas: elaboraciones en la sede, notas de salida recibidas (por lote) y compras del insumo.
    ///  - Salidas: notas de salida enviadas, consumo en elaboración de otros PI y fórmulas magistrales.
    ///  - Ajustes: conteos físicos (en su fecha el saldo del lote pasa a ser lo contado).
    ///  - Lo que queda en un lote vencido es BAJA.
    /// Clave de lote (<see cref="LedgerParte.IdCompraInsumo"/>): id del producto intermedio (&gt; 0) o
    /// -id de la compra_insumo (&lt; 0) para lotes comprados.
    /// </summary>
    public class KardexLedgerPiService
    {
        private readonly ApiContext _context;

        public KardexLedgerPiService(ApiContext context)
        {
            _context = context;
        }

        public static bool EsClaveCompra(int clave) => clave < 0;

        /// <summary>Registro que se muestra para una clave de lote ("PI…" o "MP…").</summary>
        public static string Registro(int clave) =>
            clave < 0 ? "MP" + Alfanumerico.ConvertToBase36(-clave) : "PI" + Alfanumerico.ConvertToBase36(clave);

        /// <summary>Cantidad que ingresa una elaboración (misma regla que la lista de stock PI).</summary>
        public static decimal EntradaElaboracion(string? tipoUso, string? um, int? loteEstandar, decimal? loteEstTotal) =>
            (tipoUso == "PI-FMG" || um == "UND")
                ? (loteEstandar ?? 0)
                : ((loteEstTotal.HasValue && loteEstTotal.Value > 0) ? loteEstTotal.Value : (loteEstandar ?? 0));

        private static bool Recibida(string? estado, DateTime? fechaRecepcion, decimal? cantidadRecibida) =>
            estado == "RECIBIDO" || estado == "RECEPCIONADO" || fechaRecepcion != null || (cantidadRecibida.HasValue && cantidadRecibida.Value > 0);

        private static string Clave(string? lote) => (lote ?? "").Trim().ToUpperInvariant();

        /// <summary>"PIxx:10;MPyy:5" → claves de lote PI (+id) o compra (-id).</summary>
        private static List<(int Clave, decimal Cantidad)> ParsearReparto(string? valor)
        {
            var lista = new List<(int, decimal)>();
            if (string.IsNullOrWhiteSpace(valor)) return lista;
            foreach (var parte in valor.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var kv = parte.Split(':', 2, StringSplitOptions.TrimEntries);
                if (kv.Length != 2 || string.IsNullOrWhiteSpace(kv[0])) continue;
                var reg = kv[0].Trim().ToUpperInvariant();
                int signo;
                if (reg.StartsWith("PI")) signo = 1;
                else if (reg.StartsWith("MP")) signo = -1;
                else continue;
                var id = Alfanumerico.ConvertFromBase36(reg.Substring(2));
                if (id <= 0) continue;
                if (decimal.TryParse(kv[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var cant) && cant > 0)
                    lista.Add((signo * id, cant));
            }
            return lista;
        }

        /// <summary>Datos de un lote PI (elaboración) para mostrar.</summary>
        public class LotePi
        {
            public int Id { get; init; }
            public int IdInsumo { get; init; }
            public string? Lote { get; init; }
            public DateTime FechaCreacion { get; init; }
            public DateTime? FechaEmision { get; init; }
            public DateTime? FechaVencimiento { get; init; }
        }

        public class ResultadoPi
        {
            public Dictionary<int, LedgerResultado> PorInsumo { get; init; } = new();
            /// <summary>Elaboraciones (de cualquier sede) referenciadas por los lotes, por id.</summary>
            public Dictionary<int, LotePi> Lotes { get; init; } = new();
        }

        public async Task<ResultadoPi> CalcularAsync(int idSede, ICollection<int>? insumoIds, DateTime? ahora = null)
        {
            var momento = ahora ?? DateTime.UtcNow;
            List<int> ids = insumoIds?.ToList() ?? await _context.Insumos.AsNoTracking()
                .Where(i => i.Clasificacion == "PI").Select(i => i.Id).ToListAsync();

            // Todos los lotes elaborados de estos insumos (en cualquier sede): sirven para ubicar las notas de salida por lote
            var lotes = await _context.ProductosIntermedios.AsNoTracking()
                .Where(p => ids.Contains(p.IdInsumo))
                .Select(p => new LotePi
                {
                    Id = p.Id, IdInsumo = p.IdInsumo, Lote = p.Lote, FechaCreacion = p.FechaCreacion,
                    FechaEmision = p.FechaEmision, FechaVencimiento = p.FechaVencimiento
                })
                .ToListAsync();
            var lotePorId = lotes.ToDictionary(l => l.Id);
            var lotePorNombre = lotes.Where(l => !string.IsNullOrWhiteSpace(l.Lote))
                .GroupBy(l => Clave(l.Lote))
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());
            // PI de una línea de nota de salida: el PI exacto si la línea lo guarda; si no (líneas antiguas), por lote
            // (si guarda un PI que no es de estos insumos, la línea no es de ellos aunque el lote coincida)
            LotePi? LoteDe(int? idPi, string? lote) =>
                idPi.HasValue ? (lotePorId.TryGetValue(idPi.Value, out var porId) ? porId : null)
                : (lotePorNombre.TryGetValue(Clave(lote), out var porNombre) ? porNombre : null);

            var partesPorInsumo = new Dictionary<int, List<LedgerParte>>();
            List<LedgerParte> PartesDe(int idInsumo)
            {
                if (!partesPorInsumo.TryGetValue(idInsumo, out var l)) { l = new List<LedgerParte>(); partesPorInsumo[idInsumo] = l; }
                return l;
            }

            // ---------- ENTRADAS: elaboraciones en esta sede ----------
            var elaboraciones = await _context.ProductosIntermedios.AsNoTracking()
                .Where(p => ids.Contains(p.IdInsumo) && p.IdSede == idSede)
                .Select(p => new { p.Id, p.IdInsumo, p.TipoUso, p.Um, p.LoteEstandar, p.LoteEstTotal, p.FechaCreacion, p.FechaVencimiento })
                .ToListAsync();
            foreach (var p in elaboraciones)
            {
                PartesDe(p.IdInsumo).Add(new LedgerParte
                {
                    IdCompraInsumo = p.Id,
                    FechaIngreso = p.FechaCreacion,
                    FechaVencimiento = p.FechaVencimiento,
                    Entradas = EntradaElaboracion(p.TipoUso, p.Um, p.LoteEstandar, p.LoteEstTotal)
                });
            }

            // ---------- ENTRADAS: notas de salida de PI recibidas en esta sede (por lote) ----------
            var nsRecibidas = await _context.NotaSalidaInsumos.AsNoTracking()
                .Where(n => !n.IdCompraInsumo.HasValue && n.NotaSalida != null && n.NotaSalida.IdSedeDestino == idSede &&
                            (n.NotaSalida.Estado == "RECIBIDO" || n.NotaSalida.Estado == "RECEPCIONADO" || n.NotaSalida.FechaRecepcion != null || (n.CantidadRecibida.HasValue && n.CantidadRecibida.Value > 0)))
                .Select(n => new { n.Id, n.IdProductoIntermedio, n.Lote, n.Cantidad, n.CantidadRecibida, n.Um, Fecha = n.NotaSalida!.FechaRecepcion ?? n.NotaSalida.FechaSalida })
                .ToListAsync();
            var loteDeNs = new Dictionary<int, LotePi>();
            foreach (var n in nsRecibidas)
            {
                var pi = LoteDe(n.IdProductoIntermedio, n.Lote);
                if (pi == null) continue;
                loteDeNs[n.Id] = pi;
                PartesDe(pi.IdInsumo).Add(new LedgerParte
                {
                    IdCompraInsumo = pi.Id,
                    IdNotaSalidaInsumo = n.Id,
                    FechaIngreso = n.Fecha,
                    FechaVencimiento = pi.FechaVencimiento,
                    Entradas = KardexLedgerMpService.CantidadNotaSalida(n.Cantidad, n.CantidadRecibida, n.Um)
                });
            }

            // ---------- ENTRADAS: compras del insumo PI (y sus notas de salida recibidas) ----------
            var compras = await _context.CompraInsumos.AsNoTracking()
                .Where(ci => ids.Contains(ci.IdInsumo) &&
                    ((ci.Compra != null && ci.Compra.IdSede == idSede && (idSede == 15 || ci.Compra.FechaLab != null)) ||
                     ci.NotaSalidaInsumos.Any(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))))
                .Select(ci => new
                {
                    ci.Id, ci.IdInsumo, ci.Um, ci.CantidadRecibida, ci.CantidadSolicitada, ci.FechaVencimiento, ci.FechaCreacion,
                    EsCompraSede = ci.Compra != null && ci.Compra.IdSede == idSede && (idSede == 15 || ci.Compra.FechaLab != null),
                    FechaCompra = ci.Compra != null ? (ci.Compra.FechaLab ?? ci.Compra.FechaFactura ?? (DateTime?)ci.Compra.FechaCreacion) : null,
                    Recibidas = ci.NotaSalidaInsumos
                        .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede &&
                            (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                        .Select(nsi => new { nsi.Id, nsi.Cantidad, nsi.CantidadRecibida, nsi.Um, Fecha = nsi.NotaSalida!.FechaRecepcion ?? nsi.NotaSalida.FechaSalida })
                        .ToList()
                })
                .ToListAsync();
            foreach (var c in compras)
            {
                var lista = PartesDe(c.IdInsumo);
                if (c.EsCompraSede)
                {
                    lista.Add(new LedgerParte
                    {
                        IdCompraInsumo = -c.Id,
                        FechaIngreso = c.FechaCompra ?? c.FechaCreacion,
                        FechaVencimiento = c.FechaVencimiento,
                        Entradas = KardexLedgerMpService.EntradaCompra(c.CantidadRecibida, c.CantidadSolicitada, c.Um)
                    });
                }
                foreach (var r in c.Recibidas)
                {
                    lista.Add(new LedgerParte
                    {
                        IdCompraInsumo = -c.Id,
                        IdNotaSalidaInsumo = r.Id,
                        FechaIngreso = r.Fecha,
                        FechaVencimiento = c.FechaVencimiento,
                        Entradas = KardexLedgerMpService.CantidadNotaSalida(r.Cantidad, r.CantidadRecibida, r.Um)
                    });
                }
            }

            // ---------- AJUSTES (conteos físicos) ----------
            var ajustes = await _context.AjusteInsumos.AsNoTracking()
                .Where(a => a.StockInsumo != null && a.StockInsumo.IdSede == idSede &&
                    ((a.StockInsumo.ProductoIntermedio != null && ids.Contains(a.StockInsumo.ProductoIntermedio.IdInsumo)) ||
                     (a.StockInsumo.CompraInsumo != null && ids.Contains(a.StockInsumo.CompraInsumo.IdInsumo)) ||
                     (a.StockInsumo.IdNotaSalidaInsumo.HasValue && !a.StockInsumo.IdCompraInsumo.HasValue)))
                .Select(a => new
                {
                    a.Id,
                    a.FechaCreacion,
                    a.StockNuevo,
                    a.Ajuste,
                    a.StockInsumo!.IdProductoIntermedio,
                    IdInsumoPi = a.StockInsumo.ProductoIntermedio != null ? (int?)a.StockInsumo.ProductoIntermedio.IdInsumo : null,
                    a.StockInsumo.IdCompraInsumo,
                    IdInsumoCompra = a.StockInsumo.CompraInsumo != null ? (int?)a.StockInsumo.CompraInsumo.IdInsumo : null,
                    a.StockInsumo.IdNotaSalidaInsumo,
                    LoteNs = a.StockInsumo.NotaSalidaInsumo != null ? a.StockInsumo.NotaSalidaInsumo.Lote : null,
                    IdPiNs = a.StockInsumo.NotaSalidaInsumo != null ? a.StockInsumo.NotaSalidaInsumo.IdProductoIntermedio : null
                })
                .ToListAsync();

            var conteosPorInsumo = new Dictionary<int, List<LedgerConteo>>();
            foreach (var a in ajustes)
            {
                int clave; int idInsumo;
                if (a.IdCompraInsumo.HasValue && a.IdInsumoCompra.HasValue)
                {
                    clave = -a.IdCompraInsumo.Value; idInsumo = a.IdInsumoCompra.Value;
                }
                else if (a.IdNotaSalidaInsumo.HasValue)
                {
                    var pi = (a.IdProductoIntermedio.HasValue && lotePorId.TryGetValue(a.IdProductoIntermedio.Value, out var porId)) ? porId
                        : (loteDeNs.TryGetValue(a.IdNotaSalidaInsumo.Value, out var porNs) ? porNs
                        : LoteDe(a.IdPiNs, a.LoteNs));
                    if (pi == null) continue;
                    clave = pi.Id; idInsumo = pi.IdInsumo;
                }
                else if (a.IdProductoIntermedio.HasValue && a.IdInsumoPi.HasValue)
                {
                    clave = a.IdProductoIntermedio.Value; idInsumo = a.IdInsumoPi.Value;
                }
                else continue;
                if (!ids.Contains(idInsumo)) continue;

                var lista = PartesDe(idInsumo);
                if (!lista.Any(p => p.IdCompraInsumo == clave))
                {
                    // Ajuste sobre un lote sin ingreso registrado en la sede: se conserva como parte propia
                    var ref_ = clave > 0 && lotePorId.TryGetValue(clave, out var lp) ? lp : null;
                    lista.Add(new LedgerParte { IdCompraInsumo = clave, IdNotaSalidaInsumo = a.IdNotaSalidaInsumo, FechaVencimiento = ref_?.FechaVencimiento });
                }
                if (!conteosPorInsumo.TryGetValue(idInsumo, out var lc)) { lc = new List<LedgerConteo>(); conteosPorInsumo[idInsumo] = lc; }
                lc.Add(new LedgerConteo
                {
                    Id = a.Id,
                    IdCompraInsumo = clave,
                    IdNotaSalidaInsumo = a.IdNotaSalidaInsumo,
                    Fecha = a.FechaCreacion.UtcDateTime,
                    StockContado = a.StockNuevo,
                    AjusteRegistrado = a.Ajuste
                });
            }

            var salidasPorInsumo = new Dictionary<int, List<LedgerSalida>>();
            void AgregarSalida(int idInsumo, int? clave, decimal cantidad, DateTime? fecha)
            {
                if (cantidad <= 0) return;
                if (!salidasPorInsumo.TryGetValue(idInsumo, out var l)) { l = new List<LedgerSalida>(); salidasPorInsumo[idInsumo] = l; }
                l.Add(new LedgerSalida { IdCompraInsumo = clave, Cantidad = cantidad, Fecha = fecha });
            }

            // ---------- SALIDAS: notas de salida enviadas desde esta sede ----------
            var nsEnviadas = await _context.NotaSalidaInsumos.AsNoTracking()
                .Where(n => n.NotaSalida != null && n.NotaSalida.IdSedeOrigen == idSede &&
                    ((n.IdCompraInsumo.HasValue && n.CompraInsumos != null && ids.Contains(n.CompraInsumos.IdInsumo)) || !n.IdCompraInsumo.HasValue))
                .Select(n => new
                {
                    n.IdCompraInsumo,
                    IdInsumoCompra = n.CompraInsumos != null ? (int?)n.CompraInsumos.IdInsumo : null,
                    n.IdProductoIntermedio, n.Lote, n.Cantidad, n.CantidadRecibida, n.Um,
                    n.NotaSalida!.FechaCreacion
                })
                .ToListAsync();
            foreach (var n in nsEnviadas)
            {
                var cant = KardexLedgerMpService.CantidadNotaSalida(n.Cantidad, n.CantidadRecibida, n.Um);
                if (n.IdCompraInsumo.HasValue && n.IdInsumoCompra.HasValue)
                {
                    AgregarSalida(n.IdInsumoCompra.Value, -n.IdCompraInsumo.Value, cant, n.FechaCreacion);
                }
                else if (LoteDe(n.IdProductoIntermedio, n.Lote) is LotePi pi)
                {
                    AgregarSalida(pi.IdInsumo, pi.Id, cant, n.FechaCreacion);
                }
            }

            // ---------- SALIDAS: consumo en elaboración de otros PI ----------
            var consumosPi = await _context.InsumoProductoIntermedios.AsNoTracking()
                .Where(x => x.ProductoIntermedio != null && x.ProductoIntermedio.IdSede == idSede && ids.Contains(x.IdInsumo))
                .Select(x => new
                {
                    x.IdInsumo, x.Practica, x.CantidadLote,
                    x.ProductoIntermedio!.FechaCreacion,
                    Lotes = x.StockInsumoProductoIntermedios.Select(r => new
                    {
                        r.Cantidad,
                        IdPi = r.StockInsumo != null ? r.StockInsumo.IdProductoIntermedio : null,
                        IdCompra = r.StockInsumo != null ? r.StockInsumo.IdCompraInsumo : null,
                        IdNs = r.StockInsumo != null ? r.StockInsumo.IdNotaSalidaInsumo : null,
                        LoteNs = r.StockInsumo != null && r.StockInsumo.NotaSalidaInsumo != null ? r.StockInsumo.NotaSalidaInsumo.Lote : null,
                        IdPiNs = r.StockInsumo != null && r.StockInsumo.NotaSalidaInsumo != null ? r.StockInsumo.NotaSalidaInsumo.IdProductoIntermedio : null
                    }).ToList()
                })
                .ToListAsync();
            foreach (var c in consumosPi)
            {
                decimal total = (c.Practica.HasValue && c.Practica.Value > 0) ? c.Practica.Value : c.CantidadLote;
                decimal explicito = 0m;
                foreach (var r in c.Lotes.Where(r => r.Cantidad > 0))
                {
                    int? clave = r.IdCompra.HasValue ? -r.IdCompra.Value
                        : r.IdPi.HasValue ? r.IdPi.Value
                        : (r.IdNs.HasValue && loteDeNs.TryGetValue(r.IdNs.Value, out var pn)) ? pn.Id
                        : LoteDe(r.IdPiNs, r.LoteNs)?.Id;
                    explicito += r.Cantidad;
                    AgregarSalida(c.IdInsumo, clave, r.Cantidad, c.FechaCreacion);
                }
                AgregarSalida(c.IdInsumo, null, total - explicito, c.FechaCreacion);
            }

            // ---------- SALIDAS: fórmulas magistrales ----------
            var formulas = await _context.FormulasCC.AsNoTracking()
                .Where(x => x.SedeId == idSede && ids.Contains(x.InsumoId))
                .Select(x => new
                {
                    x.InsumoId, x.CantidadL, x.RegistrosLote, x.FechaCreacion,
                    FechaFormula = x.Formula != null ? (DateTime?)x.Formula.FechaCreacion : null,
                    FechaLab = x.Formula != null && x.Formula.Laboratorio != null ? (DateTime?)x.Formula.Laboratorio.FechaCreacion : null
                })
                .ToListAsync();
            foreach (var f in formulas)
            {
                var fecha = f.FechaLab ?? f.FechaFormula ?? f.FechaCreacion;
                var reparto = ParsearReparto(f.RegistrosLote);
                // Misma regla que MP: la línea de fórmula sin registro (lote) elegido no afecta el stock.
                if (!KardexLedgerMpService.EsSalidaFormulaConRegistro(reparto.Count)) continue;
                decimal explicito = 0m;
                foreach (var (clave, cant) in reparto)
                {
                    explicito += cant;
                    AgregarSalida(f.InsumoId, clave, cant, fecha);
                }
                AgregarSalida(f.InsumoId, null, f.CantidadL - explicito, fecha);
            }

            // ---------- CÁLCULO ----------
            var resultado = new Dictionary<int, LedgerResultado>();
            foreach (var idInsumo in partesPorInsumo.Keys.Union(salidasPorInsumo.Keys).Union(conteosPorInsumo.Keys))
            {
                partesPorInsumo.TryGetValue(idInsumo, out var partes);
                salidasPorInsumo.TryGetValue(idInsumo, out var salidas);
                conteosPorInsumo.TryGetValue(idInsumo, out var conteos);
                resultado[idInsumo] = KardexLedger.Calcular(
                    partes ?? new List<LedgerParte>(),
                    salidas ?? new List<LedgerSalida>(),
                    conteos ?? new List<LedgerConteo>(),
                    momento);
            }
            return new ResultadoPi { PorInsumo = resultado, Lotes = lotePorId };
        }
    }
}

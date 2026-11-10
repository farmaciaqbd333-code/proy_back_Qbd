using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Util;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services.Kardex.Ledger
{
    /// <summary>
    /// Libro mayor (<see cref="KardexLedger"/>) de economato (ECO) en una sede, en unidades.
    /// En las partes y salidas, IdCompraInsumo = id de compra_economato e IdNotaSalidaInsumo = id de nota_salida_economato.
    ///   Entradas: compra propia de la sede + notas de salida recibidas (NotaSalidaEconomatos donde IdSedeDestino == idSede y recibidas).
    ///   Salidas: notas de salida despachadas desde la sede (NotaSalidaEconomatos donde IdSedeOrigen == idSede).
    ///   Ajustes: conteos físicos vía AjusteEconomatos / StockEconomatos.
    /// No se usa StockEconomatos.StockDisponible como fuente de stock.
    /// </summary>
    public class KardexLedgerEcoService
    {
        private readonly ApiContext _context;

        public KardexLedgerEcoService(ApiContext context)
        {
            _context = context;
        }

        /// <summary>Cantidad de una línea de nota de salida de economato: lo recibido si ya se recibió; si no, lo despachado.</summary>
        public static decimal CantidadNotaSalida(decimal cantidad, decimal cantidadRecibida)
            => cantidadRecibida > 0 ? cantidadRecibida : cantidad;

        /// <summary>
        /// Calcula el libro mayor de cada economato en la sede. Si <paramref name="economatoIds"/> es null, de todos los economatos
        /// con movimientos. Las salidas traen en <see cref="LedgerSalida.Info"/> un <see cref="SalidaInsumoRes"/> modelo.
        /// </summary>
        public async Task<Dictionary<int, LedgerResultado>> CalcularAsync(int idSede, ICollection<int>? economatoIds, DateTime? ahora = null)
        {
            var momento = ahora ?? DateTime.UtcNow;
            bool todos = economatoIds == null;
            var ids = economatoIds?.ToList() ?? new List<int>();

            // ---------- ENTRADAS (partes de lote) ----------
            var compras = await _context.CompraEconomatos
                .AsNoTracking()
                .Where(ce => (todos || ids.Contains(ce.IdEconomato)) &&
                    ((ce.Compra != null && ce.Compra.IdSede == idSede) ||
                     (ce.NotaSalidaEconomatos != null && ce.NotaSalidaEconomatos.Any(n => n.NotaSalida != null && n.NotaSalida.IdSedeDestino == idSede &&
                        (n.NotaSalida.Estado == "RECIBIDO" || n.NotaSalida.Estado == "RECEPCIONADO" || n.NotaSalida.FechaRecepcion != null || n.CantidadRecibida > 0)))))
                .Select(ce => new
                {
                    ce.Id,
                    ce.IdEconomato,
                    ce.CantidadRecibida,
                    ce.CantidadSolicitada,
                    ce.FechaCreacion,
                    EsCompraSede = ce.Compra != null && ce.Compra.IdSede == idSede,
                    FechaCompra = ce.Compra != null ? (ce.Compra.FechaLab ?? ce.Compra.FechaFactura ?? (DateTime?)ce.Compra.FechaCreacion) : null,
                    Recibidas = ce.NotaSalidaEconomatos == null ? new List<object>().Select(x => new { Id = 0, Cantidad = 0m, CantidadRecibida = 0m, Fecha = (DateTime?)null }) :
                        ce.NotaSalidaEconomatos
                            .Where(n => n.NotaSalida != null && n.NotaSalida.IdSedeDestino == idSede &&
                                (n.NotaSalida.Estado == "RECIBIDO" || n.NotaSalida.Estado == "RECEPCIONADO" || n.NotaSalida.FechaRecepcion != null || n.CantidadRecibida > 0))
                            .Select(n => new { n.Id, n.Cantidad, n.CantidadRecibida, Fecha = (DateTime?)(n.NotaSalida!.FechaRecepcion ?? n.NotaSalida.FechaSalida) })
                })
                .ToListAsync();

            var partesPorEconomato = new Dictionary<int, List<LedgerParte>>();
            List<LedgerParte> PartesDe(int idEconomato)
            {
                if (!partesPorEconomato.TryGetValue(idEconomato, out var l)) { l = new List<LedgerParte>(); partesPorEconomato[idEconomato] = l; }
                return l;
            }

            foreach (var c in compras)
            {
                var lista = PartesDe(c.IdEconomato);
                if (c.EsCompraSede)
                {
                    lista.Add(new LedgerParte
                    {
                        IdCompraInsumo = c.Id,
                        FechaIngreso = c.FechaCompra ?? c.FechaCreacion,
                        Entradas = (c.CantidadRecibida.HasValue && c.CantidadRecibida.Value > 0) ? c.CantidadRecibida.Value : c.CantidadSolicitada
                    });
                }
                foreach (var r in c.Recibidas)
                {
                    lista.Add(new LedgerParte
                    {
                        IdCompraInsumo = c.Id,
                        IdNotaSalidaInsumo = r.Id,
                        FechaIngreso = r.Fecha,
                        Entradas = CantidadNotaSalida(r.Cantidad, r.CantidadRecibida)
                    });
                }
            }

            // ---------- AJUSTES = conteos físicos por registro ----------
            var ajustes = await _context.AjusteEconomatos
                .AsNoTracking()
                .Where(a => a.StockEconomato != null && a.StockEconomato.IdSede == idSede && a.StockEconomato.CompraEconomato != null &&
                            (todos || ids.Contains(a.StockEconomato.CompraEconomato.IdEconomato)))
                .Select(a => new
                {
                    a.Id,
                    IdCompra = a.StockEconomato!.IdCompraEconomato,
                    a.StockEconomato.IdNotaSalidaEconomato,
                    a.StockEconomato.CompraEconomato!.IdEconomato,
                    a.FechaCreacion,
                    a.StockNuevo,
                    a.Ajuste
                })
                .ToListAsync();

            var conteosPorEconomato = new Dictionary<int, List<LedgerConteo>>();
            foreach (var a in ajustes)
            {
                var lista = PartesDe(a.IdEconomato);
                if (!lista.Any(p => p.IdCompraInsumo == a.IdCompra))
                    lista.Add(new LedgerParte { IdCompraInsumo = a.IdCompra, IdNotaSalidaInsumo = a.IdNotaSalidaEconomato });
                if (!conteosPorEconomato.TryGetValue(a.IdEconomato, out var lc)) { lc = new List<LedgerConteo>(); conteosPorEconomato[a.IdEconomato] = lc; }
                lc.Add(new LedgerConteo
                {
                    Id = a.Id,
                    IdCompraInsumo = a.IdCompra,
                    IdNotaSalidaInsumo = a.IdNotaSalidaEconomato,
                    Fecha = a.FechaCreacion.UtcDateTime,
                    StockContado = a.StockNuevo,
                    AjusteRegistrado = a.Ajuste
                });
            }

            var salidasPorEconomato = new Dictionary<int, List<LedgerSalida>>();
            void Agregar(int idEconomato, LedgerSalida s)
            {
                if (s.Cantidad <= 0) return;
                if (!salidasPorEconomato.TryGetValue(idEconomato, out var l)) { l = new List<LedgerSalida>(); salidasPorEconomato[idEconomato] = l; }
                l.Add(s);
            }

            // ---------- SALIDAS POR NOTA DE SALIDA (despachadas desde la sede) ----------
            var notas = await _context.NotaSalidaEconomatos
                .AsNoTracking()
                .Where(x => x.CompraEconomato != null && x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede &&
                            (todos || ids.Contains(x.CompraEconomato.IdEconomato)))
                .Select(x => new
                {
                    IdCompra = x.CompraEconomato!.Id,
                    x.CompraEconomato.IdEconomato,
                    x.Cantidad,
                    x.CantidadRecibida,
                    IdNota = x.NotaSalida!.Id,
                    x.NotaSalida.FechaCreacion,
                    SedeDestino = x.NotaSalida.SedeDestino != null ? x.NotaSalida.SedeDestino.Nombre : null,
                    Creador = x.NotaSalida.Creador != null ? x.NotaSalida.Creador.Codigo : null
                })
                .ToListAsync();

            foreach (var n in notas)
            {
                var cant = CantidadNotaSalida(n.Cantidad, n.CantidadRecibida);
                Agregar(n.IdEconomato, new LedgerSalida
                {
                    IdCompraInsumo = n.IdCompra,
                    Cantidad = cant,
                    Fecha = n.FechaCreacion,
                    Info = new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = "NT" + n.IdNota,
                        DescripcionDestino = n.SedeDestino != null ? $"Envío a {n.SedeDestino}" : "Nota de Salida",
                        Cantidad = cant,
                        Um = "UND",
                        Fecha = n.FechaCreacion,
                        Usuario = n.Creador ?? "ADMIN"
                    }
                });
            }

            // ---------- CÁLCULO ----------
            var resultado = new Dictionary<int, LedgerResultado>();
            foreach (var idEconomato in partesPorEconomato.Keys.Union(salidasPorEconomato.Keys).Union(conteosPorEconomato.Keys))
            {
                partesPorEconomato.TryGetValue(idEconomato, out var partes);
                salidasPorEconomato.TryGetValue(idEconomato, out var salidas);
                conteosPorEconomato.TryGetValue(idEconomato, out var conteos);
                resultado[idEconomato] = KardexLedger.Calcular(
                    partes ?? new List<LedgerParte>(),
                    salidas ?? new List<LedgerSalida>(),
                    conteos ?? new List<LedgerConteo>(),
                    momento);
            }
            return resultado;
        }

        /// <summary>Libro mayor de un solo economato (vacío si no tiene movimientos en la sede).</summary>
        public async Task<LedgerResultado> CalcularEconomatoAsync(int idSede, int idEconomato, DateTime? ahora = null)
        {
            var r = await CalcularAsync(idSede, new[] { idEconomato }, ahora);
            return r.TryGetValue(idEconomato, out var res) ? res : new LedgerResultado();
        }

        /// <summary>
        /// Convierte las salidas del libro mayor en filas para el kardex: una fila por cada lote del que salió.
        /// </summary>
        public static List<SalidaInsumoRes> FilasSalida(LedgerResultado ledger, Func<int, string?> loteDeCompra)
        {
            var filas = new List<SalidaInsumoRes>();
            foreach (var s in ledger.Salidas)
            {
                if (s.Info is not SalidaInsumoRes m) continue;
                foreach (var (parte, cant) in s.Asignaciones)
                {
                    filas.Add(new SalidaInsumoRes
                    {
                        TipoSalida = m.TipoSalida,
                        RegistroDestino = m.RegistroDestino,
                        DescripcionDestino = m.DescripcionDestino,
                        LoteInsumo = parte != null ? (loteDeCompra(parte.IdCompraInsumo) ?? "") : "",
                        RegistroLoteInsumo = parte != null ? "ECO" + Alfanumerico.ConvertToBase36(parte.IdCompraInsumo) : "",
                        Cantidad = cant,
                        Um = "UND",
                        Fecha = m.Fecha,
                        Usuario = m.Usuario
                    });
                }
            }
            return filas.OrderByDescending(x => x.Fecha).ToList();
        }
    }
}

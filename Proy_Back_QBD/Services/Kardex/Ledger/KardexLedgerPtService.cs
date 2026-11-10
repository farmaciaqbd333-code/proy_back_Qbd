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
    /// Libro mayor (<see cref="KardexLedger"/>) de producto terminado (PT) en una sede, en unidades.
    /// En las partes y salidas, IdCompraInsumo = id de compra_producto e IdNotaSalidaInsumo = id de nota_salida_producto.
    ///   Entradas: compra propia de la sede + notas de salida recibidas (NotaSalidaProductos donde IdSedeDestino == idSede y recibidas).
    ///   Salidas: notas de salida despachadas desde la sede (NotaSalidaProductos donde IdSedeOrigen == idSede).
    ///   Ajustes: conteos físicos vía AjusteProductos / StockProductoTerminados.
    /// No se usa StockProductoTerminados.StockDisponible como fuente de stock.
    /// </summary>
    public class KardexLedgerPtService
    {
        private readonly ApiContext _context;

        public KardexLedgerPtService(ApiContext context)
        {
            _context = context;
        }

        /// <summary>Cantidad de una línea de nota de salida de producto terminado: lo recibido si ya se recibió; si no, lo despachado.</summary>
        public static decimal CantidadNotaSalida(decimal cantidad, decimal cantidadRecibida)
            => cantidadRecibida > 0 ? cantidadRecibida : cantidad;

        /// <summary>
        /// Calcula el libro mayor de cada producto terminado en la sede. Si <paramref name="productoIds"/> es null, de todos los productos
        /// con movimientos. Las salidas traen en <see cref="LedgerSalida.Info"/> un <see cref="SalidaInsumoRes"/> modelo.
        /// </summary>
        public async Task<Dictionary<int, LedgerResultado>> CalcularAsync(int idSede, ICollection<int>? productoIds, DateTime? ahora = null)
        {
            var momento = ahora ?? DateTime.UtcNow;
            bool todos = productoIds == null;
            var ids = productoIds?.ToList() ?? new List<int>();

            // ---------- ENTRADAS (partes de lote) ----------
            var compras = await _context.CompraProductos
                .AsNoTracking()
                .Where(cp => (todos || ids.Contains(cp.IdProducto)) &&
                    ((cp.Compra != null && cp.Compra.IdSede == idSede) ||
                     (cp.NotaSalidaProductos != null && cp.NotaSalidaProductos.Any(n => n.NotaSalida != null && n.NotaSalida.IdSedeDestino == idSede &&
                        (n.NotaSalida.Estado == "RECIBIDO" || n.NotaSalida.Estado == "RECEPCIONADO" || n.NotaSalida.FechaRecepcion != null || n.CantidadRecibida > 0)))))
                .Select(cp => new
                {
                    cp.Id,
                    cp.IdProducto,
                    cp.CantidadRecibida,
                    cp.CantidadSolicitada,
                    cp.FechaVencimiento,
                    cp.FechaCreacion,
                    EsCompraSede = cp.Compra != null && cp.Compra.IdSede == idSede,
                    FechaCompra = cp.Compra != null ? (cp.Compra.FechaLab ?? cp.Compra.FechaFactura ?? (DateTime?)cp.Compra.FechaCreacion) : null,
                    Recibidas = cp.NotaSalidaProductos == null ? new List<object>().Select(x => new { Id = 0, Cantidad = 0m, CantidadRecibida = 0m, Fecha = (DateTime?)null }) :
                        cp.NotaSalidaProductos
                            .Where(n => n.NotaSalida != null && n.NotaSalida.IdSedeDestino == idSede &&
                                (n.NotaSalida.Estado == "RECIBIDO" || n.NotaSalida.Estado == "RECEPCIONADO" || n.NotaSalida.FechaRecepcion != null || n.CantidadRecibida > 0))
                            .Select(n => new { n.Id, n.Cantidad, n.CantidadRecibida, Fecha = (DateTime?)(n.NotaSalida!.FechaRecepcion ?? n.NotaSalida.FechaSalida) })
                })
                .ToListAsync();

            var partesPorProducto = new Dictionary<int, List<LedgerParte>>();
            List<LedgerParte> PartesDe(int idProducto)
            {
                if (!partesPorProducto.TryGetValue(idProducto, out var l)) { l = new List<LedgerParte>(); partesPorProducto[idProducto] = l; }
                return l;
            }

            foreach (var c in compras)
            {
                var lista = PartesDe(c.IdProducto);
                if (c.EsCompraSede)
                {
                    lista.Add(new LedgerParte
                    {
                        IdCompraInsumo = c.Id,
                        FechaIngreso = c.FechaCompra ?? c.FechaCreacion,
                        FechaVencimiento = c.FechaVencimiento,
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
                        FechaVencimiento = c.FechaVencimiento,
                        Entradas = CantidadNotaSalida(r.Cantidad, r.CantidadRecibida)
                    });
                }
            }

            // ---------- AJUSTES = conteos físicos por registro ----------
            var ajustes = await _context.AjusteProductoTerminados
                .AsNoTracking()
                .Where(a => a.StockProducto != null && a.StockProducto.IdSede == idSede && a.StockProducto.CompraProducto != null &&
                            (todos || ids.Contains(a.StockProducto.CompraProducto.IdProducto)))
                .Select(a => new
                {
                    a.Id,
                    IdCompra = a.StockProducto!.IdCompraProducto,
                    a.StockProducto.IdNotaSalidaProducto,
                    a.StockProducto.CompraProducto!.IdProducto,
                    a.FechaCreacion,
                    a.StockNuevo,
                    a.Ajuste
                })
                .ToListAsync();

            var conteosPorProducto = new Dictionary<int, List<LedgerConteo>>();
            foreach (var a in ajustes)
            {
                var lista = PartesDe(a.IdProducto);
                if (!lista.Any(p => p.IdCompraInsumo == a.IdCompra))
                    lista.Add(new LedgerParte { IdCompraInsumo = a.IdCompra, IdNotaSalidaInsumo = a.IdNotaSalidaProducto });
                if (!conteosPorProducto.TryGetValue(a.IdProducto, out var lc)) { lc = new List<LedgerConteo>(); conteosPorProducto[a.IdProducto] = lc; }
                lc.Add(new LedgerConteo
                {
                    Id = a.Id,
                    IdCompraInsumo = a.IdCompra,
                    IdNotaSalidaInsumo = a.IdNotaSalidaProducto,
                    Fecha = a.FechaCreacion.HasValue ? a.FechaCreacion.Value.UtcDateTime : (DateTime?)null,
                    StockContado = a.StockNuevo,
                    AjusteRegistrado = a.Ajuste
                });
            }

            var salidasPorProducto = new Dictionary<int, List<LedgerSalida>>();
            void Agregar(int idProducto, LedgerSalida s)
            {
                if (s.Cantidad <= 0) return;
                if (!salidasPorProducto.TryGetValue(idProducto, out var l)) { l = new List<LedgerSalida>(); salidasPorProducto[idProducto] = l; }
                l.Add(s);
            }

            // ---------- SALIDAS POR NOTA DE SALIDA (despachadas desde la sede) ----------
            var notas = await _context.NotaSalidaProductos
                .AsNoTracking()
                .Where(x => x.CompraProducto != null && x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede &&
                            (todos || ids.Contains(x.CompraProducto.IdProducto)))
                .Select(x => new
                {
                    IdCompra = x.CompraProducto!.Id,
                    x.CompraProducto.IdProducto,
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
                Agregar(n.IdProducto, new LedgerSalida
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
            foreach (var idProducto in partesPorProducto.Keys.Union(salidasPorProducto.Keys).Union(conteosPorProducto.Keys))
            {
                partesPorProducto.TryGetValue(idProducto, out var partes);
                salidasPorProducto.TryGetValue(idProducto, out var salidas);
                conteosPorProducto.TryGetValue(idProducto, out var conteos);
                resultado[idProducto] = KardexLedger.Calcular(
                    partes ?? new List<LedgerParte>(),
                    salidas ?? new List<LedgerSalida>(),
                    conteos ?? new List<LedgerConteo>(),
                    momento);
            }
            return resultado;
        }

        /// <summary>Libro mayor de un solo producto terminado (vacío si no tiene movimientos en la sede).</summary>
        public async Task<LedgerResultado> CalcularProductoAsync(int idSede, int idProducto, DateTime? ahora = null)
        {
            var r = await CalcularAsync(idSede, new[] { idProducto }, ahora);
            return r.TryGetValue(idProducto, out var res) ? res : new LedgerResultado();
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
                        RegistroLoteInsumo = parte != null ? "PT" + Alfanumerico.ConvertToBase36(parte.IdCompraInsumo) : "",
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

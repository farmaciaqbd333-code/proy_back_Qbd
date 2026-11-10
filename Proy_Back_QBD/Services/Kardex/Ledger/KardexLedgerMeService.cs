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
    /// Libro mayor (<see cref="KardexLedger"/>) de material de empaque (ME) en una sede, en unidades.
    /// En las partes y salidas, IdCompraInsumo = id de compra_empaque e IdNotaSalidaInsumo = id de nota_salida_empaque.
    ///   Entradas: compra + notas de salida recibidas.
    ///   Salidas: notas de salida (con registro), elaboración de PI (con registro si se guardó el lote; si no, 1 FIFO)
    ///            y fórmulas de laboratorio con envase (1 por fórmula, FIFO).
    ///   Ajustes: conteos físicos. Baja: lo que queda en lotes vencidos.
    /// No se usa StockEmpaque.StockDisponible.
    /// </summary>
    public class KardexLedgerMeService
    {
        private readonly ApiContext _context;

        public KardexLedgerMeService(ApiContext context)
        {
            _context = context;
        }

        /// <summary>Cantidad de una línea de nota de salida de empaque: lo recibido si ya se recibió; si no, lo despachado.</summary>
        public static decimal CantidadNotaSalida(decimal cantidad, decimal cantidadRecibida)
            => cantidadRecibida > 0 ? cantidadRecibida : cantidad;

        public async Task<Dictionary<int, LedgerResultado>> CalcularAsync(int idSede, ICollection<int>? empaqueIds, DateTime? ahora = null)
        {
            var momento = ahora ?? DateTime.UtcNow;
            bool todos = empaqueIds == null;
            var ids = empaqueIds?.ToList() ?? new List<int>();

            var compras = await _context.CompraEmpaques
                .AsNoTracking()
                .Where(ce => (todos || ids.Contains(ce.IdEmpaque)) &&
                    ((ce.Compra != null && ce.Compra.IdSede == idSede && (idSede == 15 || ce.Compra.FechaLab != null)) ||
                     ce.NotaSalidaEmpaques.Any(n => n.NotaSalida != null && n.NotaSalida.IdSedeDestino == idSede &&
                        (n.NotaSalida.Estado == "RECIBIDO" || n.NotaSalida.Estado == "RECEPCIONADO" || n.NotaSalida.FechaRecepcion != null || n.CantidadRecibida > 0))))
                .Select(ce => new
                {
                    ce.Id,
                    ce.IdEmpaque,
                    ce.CantidadRecibida,
                    ce.CantidadSolicitada,
                    ce.FechaVencimiento,
                    ce.FechaCreacion,
                    EsCompraSede = ce.Compra != null && ce.Compra.IdSede == idSede && (idSede == 15 || ce.Compra.FechaLab != null),
                    FechaCompra = ce.Compra != null ? (ce.Compra.FechaLab ?? ce.Compra.FechaFactura ?? (DateTime?)ce.Compra.FechaCreacion) : null,
                    Recibidas = ce.NotaSalidaEmpaques
                        .Where(n => n.NotaSalida != null && n.NotaSalida.IdSedeDestino == idSede &&
                            (n.NotaSalida.Estado == "RECIBIDO" || n.NotaSalida.Estado == "RECEPCIONADO" || n.NotaSalida.FechaRecepcion != null || n.CantidadRecibida > 0))
                        .Select(n => new { n.Id, n.Cantidad, n.CantidadRecibida, Fecha = n.NotaSalida!.FechaRecepcion ?? n.NotaSalida.FechaSalida })
                        .ToList()
                })
                .ToListAsync();

            var partesPor = new Dictionary<int, List<LedgerParte>>();
            List<LedgerParte> PartesDe(int id)
            {
                if (!partesPor.TryGetValue(id, out var l)) { l = new List<LedgerParte>(); partesPor[id] = l; }
                return l;
            }

            foreach (var c in compras)
            {
                var lista = PartesDe(c.IdEmpaque);
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

            // Ajustes = conteos físicos
            var ajustes = await _context.AjusteEmpaques
                .AsNoTracking()
                .Where(a => a.StockEmpaque != null && a.StockEmpaque.IdSede == idSede && a.StockEmpaque.CompraEmpaque != null &&
                            (todos || ids.Contains(a.StockEmpaque.CompraEmpaque.IdEmpaque)))
                .Select(a => new
                {
                    a.Id,
                    IdCompra = a.StockEmpaque!.IdCompraEmpaque,
                    a.StockEmpaque.IdNotaSalidaEmpaque,
                    a.StockEmpaque.CompraEmpaque!.IdEmpaque,
                    a.FechaCreacion,
                    a.StockNuevo,
                    a.Ajuste
                })
                .ToListAsync();

            var conteosPor = new Dictionary<int, List<LedgerConteo>>();
            foreach (var a in ajustes)
            {
                var lista = PartesDe(a.IdEmpaque);
                if (!lista.Any(p => p.IdCompraInsumo == a.IdCompra))
                    lista.Add(new LedgerParte { IdCompraInsumo = a.IdCompra, IdNotaSalidaInsumo = a.IdNotaSalidaEmpaque });
                if (!conteosPor.TryGetValue(a.IdEmpaque, out var lc)) { lc = new List<LedgerConteo>(); conteosPor[a.IdEmpaque] = lc; }
                lc.Add(new LedgerConteo
                {
                    Id = a.Id,
                    IdCompraInsumo = a.IdCompra,
                    IdNotaSalidaInsumo = a.IdNotaSalidaEmpaque,
                    Fecha = a.FechaCreacion.UtcDateTime,
                    StockContado = a.StockNuevo,
                    AjusteRegistrado = a.Ajuste
                });
            }

            var salidasPor = new Dictionary<int, List<LedgerSalida>>();
            void Agregar(int idEmpaque, LedgerSalida s)
            {
                if (s.Cantidad <= 0) return;
                if (!salidasPor.TryGetValue(idEmpaque, out var l)) { l = new List<LedgerSalida>(); salidasPor[idEmpaque] = l; }
                l.Add(s);
            }

            // Notas de salida despachadas desde la sede
            var notas = await _context.NotaSalidaEmpaques
                .AsNoTracking()
                .Where(x => x.CompraEmpaques != null && x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede &&
                            (todos || ids.Contains(x.CompraEmpaques.IdEmpaque)))
                .Select(x => new
                {
                    IdCompra = x.CompraEmpaques!.Id,
                    x.CompraEmpaques.IdEmpaque,
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
                Agregar(n.IdEmpaque, new LedgerSalida
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

            // Elaboración de productos intermedios
            var pis = await _context.EmpaqueProductoIntermedios
                .AsNoTracking()
                .Where(x => x.ProductoIntermedio != null && x.ProductoIntermedio.IdSede == idSede && (todos || ids.Contains(x.IdEmpaque)))
                .Select(x => new
                {
                    x.IdEmpaque,
                    IdPI = x.ProductoIntermedio!.Id,
                    x.ProductoIntermedio.FechaCreacion,
                    Desc = x.ProductoIntermedio.Insumo != null ? x.ProductoIntermedio.Insumo.Descripcion : (x.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                    Elaborador = x.ProductoIntermedio.Elaborador != null ? x.ProductoIntermedio.Elaborador.Codigo : null,
                    Lotes = x.StockEmpaqueProductoIntermedios.Select(r => new { r.Cantidad, IdCompra = r.StockEmpaque != null ? (int?)r.StockEmpaque.IdCompraEmpaque : null }).ToList()
                })
                .ToListAsync();
            foreach (var p in pis)
            {
                SalidaInsumoRes Info(decimal cant) => new SalidaInsumoRes
                {
                    TipoSalida = "ELABORACIÓN PI",
                    RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(p.IdPI),
                    DescripcionDestino = p.Desc ?? "Producto Intermedio",
                    Cantidad = cant,
                    Um = "UND",
                    Fecha = p.FechaCreacion,
                    Usuario = p.Elaborador ?? "ADMIN"
                };
                var lotes = p.Lotes.Where(r => r.Cantidad > 0).ToList();
                if (lotes.Count == 0)
                {
                    Agregar(p.IdEmpaque, new LedgerSalida { Cantidad = 1m, Fecha = p.FechaCreacion, Info = Info(1m) });
                    continue;
                }
                foreach (var r in lotes)
                    Agregar(p.IdEmpaque, new LedgerSalida { IdCompraInsumo = r.IdCompra, Cantidad = r.Cantidad, Fecha = p.FechaCreacion, Info = Info(r.Cantidad) });
            }

            // Fórmulas de laboratorio con envase: 1 unidad por fórmula
            var labs = await _context.Laboratorios
                .AsNoTracking()
                .Where(l => l.SedeId == idSede && l.EmpaqueId != null && (todos || ids.Contains(l.EmpaqueId!.Value)))
                .Select(l => new { IdEmpaque = l.EmpaqueId!.Value, l.Id, l.FechaCreacion, Usuario = l.Creador != null ? l.Creador.Codigo : null })
                .ToListAsync();
            foreach (var l in labs)
            {
                Agregar(l.IdEmpaque, new LedgerSalida
                {
                    Cantidad = 1m,
                    Fecha = l.FechaCreacion,
                    Info = new SalidaInsumoRes
                    {
                        TipoSalida = "FÓRMULA MAGISTRAL",
                        RegistroDestino = "FM-" + l.Id,
                        DescripcionDestino = "Envase de fórmula",
                        Cantidad = 1m,
                        Um = "UND",
                        Fecha = l.FechaCreacion,
                        Usuario = l.Usuario ?? "ADMIN"
                    }
                });
            }

            var resultado = new Dictionary<int, LedgerResultado>();
            foreach (var id in partesPor.Keys.Union(salidasPor.Keys).Union(conteosPor.Keys))
            {
                partesPor.TryGetValue(id, out var partes);
                salidasPor.TryGetValue(id, out var salidas);
                conteosPor.TryGetValue(id, out var conteos);
                resultado[id] = KardexLedger.Calcular(partes ?? new(), salidas ?? new(), conteos ?? new(), momento);
            }
            return resultado;
        }

        public async Task<LedgerResultado> CalcularEmpaqueAsync(int idSede, int idEmpaque, DateTime? ahora = null)
        {
            var r = await CalcularAsync(idSede, new[] { idEmpaque }, ahora);
            return r.TryGetValue(idEmpaque, out var res) ? res : new LedgerResultado();
        }

        /// <summary>Salidas del libro mayor como filas para el kardex (una por lote del que salió).</summary>
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
                        RegistroLoteInsumo = parte != null ? "ME" + Alfanumerico.ConvertToBase36(parte.IdCompraInsumo) : "",
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

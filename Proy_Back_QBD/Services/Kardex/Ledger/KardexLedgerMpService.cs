using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Util;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services.Kardex.Ledger
{
    /// <summary>
    /// Arma el libro mayor (<see cref="KardexLedger"/>) de materia prima en una sede a partir de los movimientos:
    /// entradas por compra y por nota de salida recibida; salidas por nota de salida, elaboración de PI y fórmula
    /// magistral; ajustes (diferencia registrada). Es la fuente única de stock para lista, kardex, salidas y bajas.
    /// </summary>
    public class KardexLedgerMpService
    {
        private readonly ApiContext _context;

        public KardexLedgerMpService(ApiContext context)
        {
            _context = context;
        }

        private static bool EsKg(string? um)
        {
            var u = (um ?? "").Trim().ToUpperInvariant();
            return u == "KG" || u == "KILOGRAMO" || u == "KILOGRAMOS" || u == "KGS";
        }

        /// <summary>Cantidad ingresada por compra, en gramos (misma regla que se usaba en lista y detalle).</summary>
        public static decimal EntradaCompra(decimal? cantidadRecibida, decimal cantidadSolicitada, string? um)
        {
            bool esKg = EsKg(um);
            if (cantidadRecibida.HasValue && cantidadRecibida.Value > 0)
            {
                if (esKg && cantidadRecibida.Value <= cantidadSolicitada * 2 && cantidadRecibida.Value < 500)
                    return cantidadRecibida.Value * 1000m;
                return cantidadRecibida.Value;
            }
            return cantidadSolicitada * (esKg ? 1000m : 1m);
        }

        /// <summary>
        /// Cantidad de una línea de nota de salida, en gramos. Es la misma para la sede origen (salida) y la destino
        /// (entrada): si ya se recibió, lo recibido (al confirmar, lo no recibido se devuelve al origen); si no, lo despachado.
        /// </summary>
        public static decimal CantidadNotaSalida(decimal cantidad, decimal? cantidadRecibida, string? um)
        {
            var factor = (um == "KG" || um == "KILOGRAMOS" || um == "Kg") ? 1000m : 1m;
            return factor * (cantidadRecibida.HasValue && cantidadRecibida.Value > 0 ? cantidadRecibida.Value : cantidad);
        }

        private static List<(int IdCompra, decimal Cantidad)> ParsearReparto(string? valor)
        {
            var lista = new List<(int, decimal)>();
            if (string.IsNullOrWhiteSpace(valor)) return lista;
            foreach (var parte in valor.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var kv = parte.Split(':', 2, StringSplitOptions.TrimEntries);
                if (kv.Length != 2 || string.IsNullOrWhiteSpace(kv[0])) continue;
                var reg = kv[0].Trim().ToUpperInvariant();
                if (!reg.StartsWith("MP")) continue;
                var id = Alfanumerico.ConvertFromBase36(reg.Substring(2));
                if (id <= 0) continue;
                if (decimal.TryParse(kv[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var cant) && cant > 0)
                    lista.Add((id, cant));
            }
            return lista;
        }

        /// <summary>
        /// Calcula el libro mayor de cada insumo en la sede. Si <paramref name="insumoIds"/> es null, de todos los insumos
        /// con movimientos. Las salidas traen en <see cref="LedgerSalida.Info"/> un <see cref="SalidaInsumoRes"/> modelo.
        /// </summary>
        public async Task<Dictionary<int, LedgerResultado>> CalcularAsync(int idSede, ICollection<int>? insumoIds, DateTime? ahora = null)
        {
            var momento = ahora ?? DateTime.UtcNow;
            bool todos = insumoIds == null;
            var ids = insumoIds?.ToList() ?? new List<int>();

            // ---------- ENTRADAS (partes de lote) ----------
            var compras = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci => (todos || ids.Contains(ci.IdInsumo)) &&
                    ((ci.Compra != null && ci.Compra.IdSede == idSede && (idSede == 15 || ci.Compra.FechaLab != null)) ||
                     ci.NotaSalidaInsumos.Any(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))))
                .Select(ci => new
                {
                    ci.Id,
                    ci.IdInsumo,
                    ci.Um,
                    ci.CantidadRecibida,
                    ci.CantidadSolicitada,
                    ci.FechaVencimiento,
                    ci.FechaCreacion,
                    EsCompraSede = ci.Compra != null && ci.Compra.IdSede == idSede && (idSede == 15 || ci.Compra.FechaLab != null),
                    FechaCompra = ci.Compra != null ? (ci.Compra.FechaLab ?? ci.Compra.FechaFactura ?? (DateTime?)ci.Compra.FechaCreacion) : null,
                    Recibidas = ci.NotaSalidaInsumos
                        .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede &&
                            (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                        .Select(nsi => new
                        {
                            nsi.Id,
                            nsi.Cantidad,
                            nsi.CantidadRecibida,
                            nsi.Um,
                            Fecha = nsi.NotaSalida!.FechaRecepcion ?? nsi.NotaSalida.FechaSalida
                        }).ToList()
                })
                .ToListAsync();

            var partesPorInsumo = new Dictionary<int, List<LedgerParte>>();
            List<LedgerParte> PartesDe(int idInsumo)
            {
                if (!partesPorInsumo.TryGetValue(idInsumo, out var l)) { l = new List<LedgerParte>(); partesPorInsumo[idInsumo] = l; }
                return l;
            }
            var insumoDeCompra = new Dictionary<int, int>();

            foreach (var c in compras)
            {
                insumoDeCompra[c.Id] = c.IdInsumo;
                var lista = PartesDe(c.IdInsumo);
                if (c.EsCompraSede)
                {
                    lista.Add(new LedgerParte
                    {
                        IdCompraInsumo = c.Id,
                        FechaIngreso = c.FechaCompra ?? c.FechaCreacion,
                        FechaVencimiento = c.FechaVencimiento,
                        Entradas = EntradaCompra(c.CantidadRecibida, c.CantidadSolicitada, c.Um)
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
                        Entradas = CantidadNotaSalida(r.Cantidad, r.CantidadRecibida, r.Um)
                    });
                }
            }

            // ---------- AJUSTES (diferencia registrada, por fila de stock de la sede) ----------
            var ajustes = await _context.AjusteInsumos
                .AsNoTracking()
                .Where(a => a.StockInsumo != null && a.StockInsumo.IdSede == idSede &&
                            a.StockInsumo.IdCompraInsumo.HasValue && a.StockInsumo.CompraInsumo != null &&
                            (todos || ids.Contains(a.StockInsumo.CompraInsumo.IdInsumo)))
                .GroupBy(a => new { IdCompra = a.StockInsumo!.IdCompraInsumo!.Value, a.StockInsumo.IdNotaSalidaInsumo, a.StockInsumo.CompraInsumo!.IdInsumo })
                .Select(g => new { g.Key.IdCompra, g.Key.IdNotaSalidaInsumo, g.Key.IdInsumo, Total = g.Sum(a => a.Ajuste) })
                .ToListAsync();

            foreach (var a in ajustes)
            {
                var lista = PartesDe(a.IdInsumo);
                var delRegistro = lista.Where(p => p.IdCompraInsumo == a.IdCompra).ToList();
                var destino =
                    (a.IdNotaSalidaInsumo.HasValue && a.IdNotaSalidaInsumo.Value > 0
                        ? delRegistro.FirstOrDefault(p => p.IdNotaSalidaInsumo == a.IdNotaSalidaInsumo)
                        : delRegistro.FirstOrDefault(p => p.IdNotaSalidaInsumo == null))
                    ?? delRegistro.FirstOrDefault();
                if (destino == null)
                {
                    // Ajuste sobre un lote sin ingreso registrado en la sede: se conserva como parte propia
                    destino = new LedgerParte { IdCompraInsumo = a.IdCompra, IdNotaSalidaInsumo = a.IdNotaSalidaInsumo };
                    lista.Add(destino);
                    insumoDeCompra[a.IdCompra] = a.IdInsumo;
                }
                destino.Ajustes += a.Total;
            }

            var salidasPorInsumo = new Dictionary<int, List<LedgerSalida>>();
            void AgregarSalida(int idInsumo, LedgerSalida s)
            {
                if (s.Cantidad <= 0) return;
                if (!salidasPorInsumo.TryGetValue(idInsumo, out var l)) { l = new List<LedgerSalida>(); salidasPorInsumo[idInsumo] = l; }
                l.Add(s);
            }
            SalidaInsumoRes Copia(SalidaInsumoRes m) => new SalidaInsumoRes
            {
                TipoSalida = m.TipoSalida, RegistroDestino = m.RegistroDestino, DescripcionDestino = m.DescripcionDestino,
                LoteInsumo = m.LoteInsumo, RegistroLoteInsumo = m.RegistroLoteInsumo, Cantidad = m.Cantidad, Um = m.Um,
                Fecha = m.Fecha, Usuario = m.Usuario
            };

            // ---------- SALIDAS POR NOTA DE SALIDA (con registro) ----------
            var notas = await _context.NotaSalidaInsumos
                .AsNoTracking()
                .Where(x => x.CompraInsumos != null && x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede &&
                            (todos || ids.Contains(x.CompraInsumos.IdInsumo)))
                .Select(x => new
                {
                    IdCompra = x.CompraInsumos!.Id,
                    x.CompraInsumos.IdInsumo,
                    x.Cantidad,
                    x.CantidadRecibida,
                    x.Um,
                    IdNota = x.NotaSalida!.Id,
                    x.NotaSalida.FechaCreacion,
                    SedeDestino = x.NotaSalida.SedeDestino != null ? x.NotaSalida.SedeDestino.Nombre : null,
                    Creador = x.NotaSalida.Creador != null ? x.NotaSalida.Creador.Codigo : null
                })
                .ToListAsync();

            foreach (var n in notas)
            {
                var cant = CantidadNotaSalida(n.Cantidad, n.CantidadRecibida, n.Um);
                AgregarSalida(n.IdInsumo, new LedgerSalida
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
                        Um = "G",
                        Fecha = n.FechaCreacion,
                        Usuario = n.Creador ?? "ADMIN"
                    }
                });
            }

            // ---------- SALIDAS POR ELABORACIÓN DE PRODUCTO INTERMEDIO ----------
            var pis = await _context.InsumoProductoIntermedios
                .AsNoTracking()
                .Where(x => x.ProductoIntermedio != null && x.ProductoIntermedio.IdSede == idSede && (todos || ids.Contains(x.IdInsumo)))
                .Select(x => new
                {
                    x.IdInsumo,
                    x.Practica,
                    x.CantidadLote,
                    IdPI = x.ProductoIntermedio!.Id,
                    x.ProductoIntermedio.FechaCreacion,
                    Desc = x.ProductoIntermedio.Insumo != null ? x.ProductoIntermedio.Insumo.Descripcion : (x.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                    Elaborador = x.ProductoIntermedio.Elaborador != null ? x.ProductoIntermedio.Elaborador.Codigo : null,
                    Lotes = x.StockInsumoProductoIntermedios
                        .Select(r => new { r.Cantidad, IdCompra = r.StockInsumo != null ? r.StockInsumo.IdCompraInsumo : null })
                        .ToList()
                })
                .ToListAsync();

            foreach (var p in pis)
            {
                var modelo = new SalidaInsumoRes
                {
                    TipoSalida = "ELABORACIÓN PI",
                    RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(p.IdPI),
                    DescripcionDestino = p.Desc ?? "Producto Intermedio",
                    Um = "G",
                    Fecha = p.FechaCreacion,
                    Usuario = p.Elaborador ?? "ADMIN"
                };
                decimal total = (p.Practica.HasValue && p.Practica.Value > 0) ? p.Practica.Value : p.CantidadLote;
                decimal explicito = 0m;
                foreach (var r in p.Lotes.Where(r => r.Cantidad > 0))
                {
                    explicito += r.Cantidad;
                    var info = Copia(modelo); info.Cantidad = r.Cantidad;
                    AgregarSalida(p.IdInsumo, new LedgerSalida { IdCompraInsumo = r.IdCompra, Cantidad = r.Cantidad, Fecha = p.FechaCreacion, Info = info });
                }
                var resto = total - explicito;
                if (resto > 0)
                {
                    var info = Copia(modelo); info.Cantidad = resto;
                    AgregarSalida(p.IdInsumo, new LedgerSalida { Cantidad = resto, Fecha = p.FechaCreacion, Info = info });
                }
            }

            // ---------- SALIDAS POR FÓRMULA MAGISTRAL ----------
            var formulas = await _context.FormulasCC
                .AsNoTracking()
                .Where(x => x.SedeId == idSede && (todos || ids.Contains(x.InsumoId)))
                .Select(x => new
                {
                    x.InsumoId,
                    x.CantidadL,
                    x.RegistrosLote,
                    x.FechaCreacion,
                    IdFormula = x.Formula != null ? (int?)x.Formula.Id : null,
                    Desc = x.Formula != null ? x.Formula.FormulaMagistral : null,
                    FechaFormula = x.Formula != null ? (DateTime?)x.Formula.FechaCreacion : null,
                    FechaLab = x.Formula != null && x.Formula.Laboratorio != null ? (DateTime?)x.Formula.Laboratorio.FechaCreacion : null,
                    UsuarioLab = x.Formula != null && x.Formula.Laboratorio != null && x.Formula.Laboratorio.Creador != null ? x.Formula.Laboratorio.Creador.Codigo : null,
                    UsuarioFormula = x.Formula != null && x.Formula.Creador != null ? x.Formula.Creador.Codigo : null
                })
                .ToListAsync();

            foreach (var f in formulas)
            {
                var fecha = f.FechaLab ?? f.FechaFormula ?? f.FechaCreacion;
                var modelo = new SalidaInsumoRes
                {
                    TipoSalida = "FÓRMULA MAGISTRAL",
                    RegistroDestino = f.IdFormula.HasValue ? "FM-" + f.IdFormula : "FM",
                    DescripcionDestino = f.Desc ?? (f.IdFormula.HasValue ? "Fórmula #" + f.IdFormula : "Fórmula"),
                    Um = "G",
                    Fecha = fecha,
                    Usuario = f.UsuarioLab ?? f.UsuarioFormula ?? "ADMIN"
                };
                decimal explicito = 0m;
                foreach (var (idCompra, cant) in ParsearReparto(f.RegistrosLote))
                {
                    explicito += cant;
                    var info = Copia(modelo); info.Cantidad = cant;
                    AgregarSalida(f.InsumoId, new LedgerSalida { IdCompraInsumo = idCompra, Cantidad = cant, Fecha = fecha, Info = info });
                }
                var resto = f.CantidadL - explicito;
                if (resto > 0)
                {
                    var info = Copia(modelo); info.Cantidad = resto;
                    AgregarSalida(f.InsumoId, new LedgerSalida { Cantidad = resto, Fecha = fecha, Info = info });
                }
            }

            // ---------- CÁLCULO ----------
            var resultado = new Dictionary<int, LedgerResultado>();
            foreach (var idInsumo in partesPorInsumo.Keys.Union(salidasPorInsumo.Keys))
            {
                partesPorInsumo.TryGetValue(idInsumo, out var partes);
                salidasPorInsumo.TryGetValue(idInsumo, out var salidas);
                resultado[idInsumo] = KardexLedger.Calcular(partes ?? new List<LedgerParte>(), salidas ?? new List<LedgerSalida>(), momento);
            }
            return resultado;
        }

        /// <summary>Libro mayor de un solo insumo (vacío si no tiene movimientos en la sede).</summary>
        public async Task<LedgerResultado> CalcularInsumoAsync(int idSede, int idInsumo, DateTime? ahora = null)
        {
            var r = await CalcularAsync(idSede, new[] { idInsumo }, ahora);
            return r.TryGetValue(idInsumo, out var res) ? res : new LedgerResultado();
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
                        RegistroLoteInsumo = parte != null ? "MP" + Alfanumerico.ConvertToBase36(parte.IdCompraInsumo) : "",
                        Cantidad = cant,
                        Um = "G",
                        Fecha = m.Fecha,
                        Usuario = m.Usuario
                    });
                }
            }
            return filas.OrderByDescending(x => x.Fecha).ToList();
        }
    }
}

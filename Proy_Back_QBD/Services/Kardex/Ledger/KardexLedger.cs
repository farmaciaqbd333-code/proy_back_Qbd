using System;
using System.Collections.Generic;
using System.Linq;

namespace proy_back_Qbd.Services.Kardex.Ledger
{
    /// <summary>
    /// Una "parte" de un lote en una sede: la entrada por compra o cada nota de salida recibida.
    /// Varias partes pueden compartir el mismo registro (misma compra_insumo).
    /// </summary>
    public class LedgerParte
    {
        public int IdCompraInsumo { get; init; }
        /// <summary>Null = entrada por compra; con valor = nota de salida recibida.</summary>
        public int? IdNotaSalidaInsumo { get; init; }
        public DateTime? FechaIngreso { get; init; }
        public DateTime? FechaVencimiento { get; init; }
        public decimal Entradas { get; init; }

        // Resultado
        /// <summary>Suma de las diferencias reales de los conteos físicos (ajustes) sobre esta parte.</summary>
        public decimal Ajustes { get; internal set; }
        public decimal Salidas { get; internal set; }
        public decimal Baja { get; internal set; }
        public decimal Saldo => Entradas - Salidas + Ajustes - Baja;

        internal decimal Disponible => Entradas - Salidas + Ajustes;
        internal bool IngresadaEn(DateTime? t) => t == null || FechaIngreso == null || FechaIngreso.Value <= t.Value;
        /// <summary>Lo que había en la parte en el momento t (si aún no había ingresado, sin su entrada).</summary>
        internal decimal DisponibleEn(DateTime? t) => (IngresadaEn(t) ? Entradas : 0m) - Salidas + Ajustes;
    }

    /// <summary>Una salida del insumo. Si trae IdCompraInsumo se descuenta de ese registro; si no, FIFO.</summary>
    public class LedgerSalida
    {
        public int? IdCompraInsumo { get; init; }
        public decimal Cantidad { get; init; }
        public DateTime? Fecha { get; init; }
        /// <summary>Dato libre del llamador (p. ej. la fila a mostrar).</summary>
        public object? Info { get; init; }

        /// <summary>Cómo quedó repartida: (parte, cantidad). Parte null = sin lote en esta sede.</summary>
        public List<(LedgerParte? Parte, decimal Cantidad)> Asignaciones { get; } = new();
    }

    /// <summary>
    /// Un ajuste de inventario = conteo físico de un registro en una fecha. Lo que manda es lo contado
    /// (StockContado): la diferencia real es contado - saldo del sistema en esa fecha. La diferencia que
    /// se guardó al registrar (AjusteRegistrado) se calculó contra lo que mostraba la pantalla entonces.
    /// </summary>
    public class LedgerConteo
    {
        public int Id { get; init; }
        public int IdCompraInsumo { get; init; }
        /// <summary>Fila de stock ajustada: null = la de compra; con valor = la de esa nota de salida recibida.</summary>
        public int? IdNotaSalidaInsumo { get; init; }
        public DateTime? Fecha { get; init; }
        public decimal StockContado { get; init; }
        public decimal AjusteRegistrado { get; init; }

        // Resultado
        /// <summary>Saldo del registro según movimientos justo antes del conteo.</summary>
        public decimal StockSistema { get; internal set; }
        /// <summary>Diferencia real aplicada (StockContado - StockSistema).</summary>
        public decimal Diferencia { get; internal set; }
    }

    public class LedgerResultado
    {
        public List<LedgerParte> Partes { get; init; } = new();
        public List<LedgerSalida> Salidas { get; init; } = new();
        public List<LedgerConteo> Conteos { get; init; } = new();
        /// <summary>Salidas que no se pudieron asignar porque el insumo no tiene ningún lote en la sede.</summary>
        public decimal SalidasSinLote { get; init; }

        public decimal TotalEntradas => Partes.Sum(p => p.Entradas);
        public decimal TotalSalidas => Partes.Sum(p => p.Salidas) + SalidasSinLote;
        public decimal TotalAjustes => Partes.Sum(p => p.Ajustes);
        public decimal TotalBaja => Partes.Sum(p => p.Baja);
        /// <summary>Stock = entradas - salidas + ajustes - bajas. Puede ser negativo (se consumió más de lo ingresado).</summary>
        public decimal Stock => TotalEntradas - TotalSalidas + TotalAjustes - TotalBaja;
    }

    /// <summary>
    /// Libro mayor de stock por lote, calculado solo a partir de movimientos y en orden de fecha.
    /// Es la única regla de stock para la lista principal, el kardex por lote, las salidas, las bajas y los ajustes:
    ///  1. Las salidas con registro conocido se descuentan de ese registro.
    ///  2. Las demás se reparten FIFO (por fecha de ingreso), partiéndolas entre lotes si hace falta,
    ///     prefiriendo lotes ya ingresados y no vencidos en la fecha de la salida.
    ///  3. Lo que no cabe en ningún lote queda en el último lote (saldo negativo), nunca se pierde.
    ///  4. Un ajuste es un conteo físico: en su fecha, el saldo del registro pasa a ser lo contado.
    ///  5. Lo que queda en un lote vencido es BAJA (y su saldo pasa a 0). Un saldo negativo no genera baja.
    /// No se usa la columna StockInsumo.StockDisponible.
    /// </summary>
    public static class KardexLedger
    {
        public static LedgerResultado Calcular(IEnumerable<LedgerParte> partesIn, IEnumerable<LedgerSalida> salidasIn, DateTime ahora)
            => Calcular(partesIn, salidasIn, Array.Empty<LedgerConteo>(), ahora);

        public static LedgerResultado Calcular(IEnumerable<LedgerParte> partesIn, IEnumerable<LedgerSalida> salidasIn, IEnumerable<LedgerConteo> conteosIn, DateTime ahora)
        {
            var partes = partesIn
                .OrderBy(p => p.FechaIngreso ?? DateTime.MinValue)
                .ThenBy(p => p.IdCompraInsumo)
                .ThenBy(p => p.IdNotaSalidaInsumo ?? 0)
                .ToList();
            var salidas = salidasIn.ToList();
            var conteos = conteosIn.ToList();
            var porRegistro = partes.GroupBy(p => p.IdCompraInsumo).ToDictionary(g => g.Key, g => g.ToList());

            decimal sinLote = 0m;

            // Línea de tiempo: salidas y conteos por fecha; a igual fecha, primero las salidas.
            var eventos = salidas.Where(s => s.Cantidad > 0).Select(s => (Fecha: s.Fecha, Orden: 0, Salida: (LedgerSalida?)s, Conteo: (LedgerConteo?)null))
                .Concat(conteos.Select(c => (Fecha: c.Fecha, Orden: 1, Salida: (LedgerSalida?)null, Conteo: (LedgerConteo?)c)))
                .OrderBy(e => e.Fecha ?? DateTime.MinValue)
                .ThenBy(e => e.Orden)
                .ToList();

            foreach (var e in eventos)
            {
                if (e.Salida != null)
                {
                    var s = e.Salida;
                    var t = s.Fecha;
                    if (s.IdCompraInsumo.HasValue && porRegistro.TryGetValue(s.IdCompraInsumo.Value, out var partesReg))
                    {
                        // Salida con registro explícito
                        Repartir(s, partesReg, t, partesReg, s.Cantidad);
                        continue;
                    }
                    if (partes.Count == 0)
                    {
                        s.Asignaciones.Add((null, s.Cantidad));
                        sinLote += s.Cantidad;
                        continue;
                    }
                    // FIFO: primero lotes ya ingresados y no vencidos en la fecha de la salida
                    var preferidas = partes.Where(p =>
                        p.IngresadaEn(t) &&
                        (t == null || p.FechaVencimiento == null || p.FechaVencimiento.Value.Date >= t.Value.Date)).ToList();
                    var resto = Repartir(s, preferidas, t, null, s.Cantidad);
                    if (resto > 0) resto = Repartir(s, partes, null, null, resto);
                    if (resto > 0) Asignar(s, partes[^1], resto);
                }
                else if (e.Conteo != null)
                {
                    var c = e.Conteo;
                    if (!porRegistro.TryGetValue(c.IdCompraInsumo, out var partesReg) || partesReg.Count == 0)
                    {
                        continue; // el cargador crea una parte para cada registro con ajuste; aquí no debería pasar
                    }
                    var sistema = partesReg.Sum(p => p.DisponibleEn(c.Fecha));
                    var diferencia = c.StockContado - sistema;
                    var destino = (c.IdNotaSalidaInsumo.HasValue && c.IdNotaSalidaInsumo.Value > 0
                            ? partesReg.FirstOrDefault(p => p.IdNotaSalidaInsumo == c.IdNotaSalidaInsumo)
                            : partesReg.FirstOrDefault(p => p.IdNotaSalidaInsumo == null))
                        ?? partesReg[0];
                    destino.Ajustes += diferencia;
                    c.StockSistema = sistema;
                    c.Diferencia = diferencia;
                }
            }

            // Bajas por vencimiento
            foreach (var p in partes)
            {
                if (p.FechaVencimiento.HasValue && p.FechaVencimiento.Value < ahora && p.Disponible > 0)
                {
                    p.Baja = p.Disponible;
                }
            }

            return new LedgerResultado { Partes = partes, Salidas = salidas, Conteos = conteos, SalidasSinLote = sinLote };
        }

        private static void Asignar(LedgerSalida s, LedgerParte p, decimal cantidad)
        {
            p.Salidas += cantidad;
            var i = s.Asignaciones.FindIndex(x => ReferenceEquals(x.Parte, p));
            if (i >= 0) s.Asignaciones[i] = (p, s.Asignaciones[i].Cantidad + cantidad);
            else s.Asignaciones.Add((p, cantidad));
        }

        /// <summary>
        /// Descuenta 'cantidad' de las partes candidatas según lo disponible en el momento t (null = sin mirar fecha).
        /// Si sobra y hay 'destinoSobrante', el sobrante va a su última parte ya ingresada (queda negativa).
        /// Devuelve lo que no se pudo asignar.
        /// </summary>
        private static decimal Repartir(LedgerSalida s, List<LedgerParte> candidatas, DateTime? t, List<LedgerParte>? destinoSobrante, decimal cantidad)
        {
            foreach (var p in candidatas)
            {
                if (cantidad <= 0) break;
                var disp = t.HasValue ? p.DisponibleEn(t) : p.Disponible;
                if (disp <= 0) continue;
                var tomar = Math.Min(disp, cantidad);
                Asignar(s, p, tomar);
                cantidad -= tomar;
            }
            if (cantidad > 0 && destinoSobrante != null && destinoSobrante.Count > 0)
            {
                var ultima = destinoSobrante.LastOrDefault(p => p.IngresadaEn(t)) ?? destinoSobrante[^1];
                Asignar(s, ultima, cantidad);
                cantidad = 0;
            }
            return cantidad;
        }
    }
}

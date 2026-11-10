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
        public decimal Ajustes { get; set; }

        // Resultado
        public decimal Salidas { get; internal set; }
        public decimal Baja { get; internal set; }
        public decimal Saldo => Entradas - Salidas + Ajustes - Baja;
        internal decimal Disponible => Entradas - Salidas + Ajustes;
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

    public class LedgerResultado
    {
        public List<LedgerParte> Partes { get; init; } = new();
        public List<LedgerSalida> Salidas { get; init; } = new();
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
    /// Libro mayor de stock por lote, calculado solo a partir de movimientos.
    /// Es la única regla de stock para la lista principal, el kardex por lote, las salidas y las bajas:
    ///  1. Las salidas con registro conocido se descuentan de ese registro.
    ///  2. Las demás se reparten FIFO (por fecha de ingreso), partiéndolas entre lotes si hace falta,
    ///     prefiriendo lotes ya ingresados y no vencidos en la fecha de la salida.
    ///  3. Lo que no cabe en ningún lote queda en el último lote (saldo negativo), nunca se pierde.
    ///  4. Lo que queda en un lote vencido es BAJA (y su saldo pasa a 0). Un saldo negativo no genera baja.
    /// No se usa la columna StockInsumo.StockDisponible.
    /// </summary>
    public static class KardexLedger
    {
        public static LedgerResultado Calcular(IEnumerable<LedgerParte> partesIn, IEnumerable<LedgerSalida> salidasIn, DateTime ahora)
        {
            var partes = partesIn
                .OrderBy(p => p.FechaIngreso ?? DateTime.MinValue)
                .ThenBy(p => p.IdCompraInsumo)
                .ThenBy(p => p.IdNotaSalidaInsumo ?? 0)
                .ToList();
            var salidas = salidasIn.ToList();
            var porRegistro = partes.GroupBy(p => p.IdCompraInsumo).ToDictionary(g => g.Key, g => g.ToList());

            decimal sinLote = 0m;
            var fifo = new List<LedgerSalida>();

            // 1. Salidas con registro explícito
            foreach (var s in salidas.Where(x => x.Cantidad > 0))
            {
                if (s.IdCompraInsumo.HasValue && porRegistro.TryGetValue(s.IdCompraInsumo.Value, out var partesReg))
                {
                    Repartir(s, partesReg, partesReg, s.Cantidad);
                }
                else
                {
                    fifo.Add(s);
                }
            }

            // 2. Salidas sin registro: FIFO en orden de fecha
            foreach (var s in fifo.OrderBy(x => x.Fecha ?? DateTime.MinValue))
            {
                if (partes.Count == 0)
                {
                    s.Asignaciones.Add((null, s.Cantidad));
                    sinLote += s.Cantidad;
                    continue;
                }
                var fecha = s.Fecha;
                var preferidas = partes.Where(p =>
                    (fecha == null || p.FechaIngreso == null || p.FechaIngreso.Value.Date <= fecha.Value.Date) &&
                    (fecha == null || p.FechaVencimiento == null || p.FechaVencimiento.Value.Date >= fecha.Value.Date)).ToList();
                var resto = Repartir(s, preferidas, null, s.Cantidad);
                if (resto > 0) Repartir(s, partes, partes, resto);
            }

            // 3. Bajas por vencimiento
            foreach (var p in partes)
            {
                if (p.FechaVencimiento.HasValue && p.FechaVencimiento.Value < ahora && p.Disponible > 0)
                {
                    p.Baja = p.Disponible;
                }
            }

            return new LedgerResultado { Partes = partes, Salidas = salidas, SalidasSinLote = sinLote };
        }

        private static void Asignar(LedgerSalida s, LedgerParte p, decimal cantidad)
        {
            p.Salidas += cantidad;
            var i = s.Asignaciones.FindIndex(x => ReferenceEquals(x.Parte, p));
            if (i >= 0) s.Asignaciones[i] = (p, s.Asignaciones[i].Cantidad + cantidad);
            else s.Asignaciones.Add((p, cantidad));
        }

        /// <summary>
        /// Descuenta 'cantidad' de las partes candidatas según su disponible. Si sobra y hay 'destinoSobrante',
        /// el sobrante va a su última parte (queda negativa). Devuelve lo que no se pudo asignar.
        /// </summary>
        private static decimal Repartir(LedgerSalida s, List<LedgerParte> candidatas, List<LedgerParte>? destinoSobrante, decimal cantidad)
        {
            foreach (var p in candidatas)
            {
                if (cantidad <= 0) break;
                var disp = p.Disponible;
                if (disp <= 0) continue;
                var tomar = Math.Min(disp, cantidad);
                Asignar(s, p, tomar);
                cantidad -= tomar;
            }
            if (cantidad > 0 && destinoSobrante != null && destinoSobrante.Count > 0)
            {
                var ultima = destinoSobrante[^1];
                Asignar(s, ultima, cantidad);
                cantidad = 0;
            }
            return cantidad;
        }
    }
}

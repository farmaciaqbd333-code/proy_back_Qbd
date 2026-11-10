using System;
using System.Collections.Generic;
using System.Linq;
using proy_back_Qbd.Services.Kardex.Ledger;
using Xunit;

namespace Proy_Back_QBD.Tests;

public class KardexLedgerTests
{
    private static readonly DateTime Hoy = new(2026, 11, 10);

    private static LedgerParte Lote(int id, string ingreso, string vence, decimal entradas, decimal ajustes = 0m) => new()
    {
        IdCompraInsumo = id,
        FechaIngreso = DateTime.Parse(ingreso),
        FechaVencimiento = DateTime.Parse(vence),
        Entradas = entradas,
        Ajustes = ajustes
    };

    private static LedgerSalida Salida(decimal cant, string fecha, int? idCompra = null) => new()
    {
        Cantidad = cant,
        Fecha = DateTime.Parse(fecha),
        IdCompraInsumo = idCompra
    };

    [Fact]
    public void Totales_cuadran_con_la_suma_de_lotes()
    {
        var lotes = new[]
        {
            Lote(1, "2024-01-10", "2025-06-01", 2000m),   // vencido
            Lote(2, "2025-01-15", "2027-03-20", 2850m),
            Lote(3, "2025-01-15", "2028-04-22", 5000m),
        };
        var salidas = new[]
        {
            Salida(3745m, "2025-05-01", idCompra: 3),
            Salida(1792.5m, "2025-07-01"),                 // FIFO sin registro
        };

        var r = KardexLedger.Calcular(lotes, salidas, Hoy);

        Assert.Equal(r.Partes.Sum(p => p.Saldo), r.Stock);
        Assert.Equal(r.TotalEntradas - r.TotalSalidas + r.TotalAjustes - r.TotalBaja, r.Stock);
        Assert.Equal(9850m, r.TotalEntradas);
        Assert.Equal(5537.5m, r.TotalSalidas);
    }

    [Fact]
    public void Fifo_no_usa_lotes_vencidos_en_la_fecha_de_la_salida_y_la_baja_es_lo_que_queda()
    {
        var vencido = Lote(1, "2024-01-10", "2025-06-01", 2000m);
        var vigente = Lote(2, "2025-01-15", "2027-03-20", 2850m);

        var r = KardexLedger.Calcular(new[] { vencido, vigente }, new[] { Salida(1792.5m, "2025-07-01") }, Hoy);

        Assert.Equal(0m, vencido.Salidas);
        Assert.Equal(2000m, vencido.Baja);
        Assert.Equal(0m, vencido.Saldo);
        Assert.Equal(1792.5m, vigente.Salidas);
        Assert.Equal(1057.5m, vigente.Saldo);
        Assert.Equal(1057.5m, r.Stock);
    }

    [Fact]
    public void La_baja_nunca_supera_lo_que_queda_en_el_lote()
    {
        // Caso ZINC OXIDO: antes la baja venía de StockDisponible (2,000) y daba stock negativo
        var vencido = Lote(1, "2024-01-10", "2025-06-01", 2000m);
        var otro = Lote(2, "2024-02-10", "2027-06-01", 250m);

        var r = KardexLedger.Calcular(new[] { vencido, otro }, new[] { Salida(669m, "2025-01-01") }, Hoy);

        Assert.Equal(669m, vencido.Salidas);
        Assert.Equal(1331m, vencido.Baja);
        Assert.Equal(250m, r.Stock);
        Assert.True(r.Stock >= 0);
    }

    [Fact]
    public void Una_salida_se_parte_entre_lotes_y_el_exceso_queda_negativo_en_el_ultimo()
    {
        var a = Lote(1, "2025-01-01", "2028-01-01", 100m);
        var b = Lote(2, "2025-02-01", "2028-01-01", 100m);

        var r = KardexLedger.Calcular(new[] { a, b }, new[] { Salida(250m, "2025-03-01") }, Hoy);

        Assert.Equal(100m, a.Salidas);
        Assert.Equal(150m, b.Salidas);
        Assert.Equal(-50m, b.Saldo);
        Assert.Equal(-50m, r.Stock);
        Assert.Equal(250m, r.TotalSalidas);
        Assert.Equal(2, r.Salidas[0].Asignaciones.Count);
    }

    [Fact]
    public void Ajustes_suman_y_se_reflejan_en_el_saldo_del_lote()
    {
        var a = Lote(1, "2025-01-01", "2028-01-01", 1000m, ajustes: -50m);
        var r = KardexLedger.Calcular(new[] { a }, new[] { Salida(200m, "2025-03-01", idCompra: 1) }, Hoy);
        Assert.Equal(750m, a.Saldo);
        Assert.Equal(-50m, r.TotalAjustes);
        Assert.Equal(750m, r.Stock);
    }

    [Fact]
    public void Salidas_sin_ningun_lote_se_cuentan_igual()
    {
        var r = KardexLedger.Calcular(new List<LedgerParte>(), new[] { Salida(30m, "2025-03-01") }, Hoy);
        Assert.Equal(30m, r.SalidasSinLote);
        Assert.Equal(-30m, r.Stock);
    }
}

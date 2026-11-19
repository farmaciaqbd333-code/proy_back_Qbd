using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.ProductoIntermedio;
using proy_back_Qbd.Services.Kardex;
using proy_back_Qbd.Services.Kardex.Ledger;
using proy_back_Qbd.Services.Kardex.Stock;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Xunit;

namespace Proy_Back_QBD.Tests;

public class KardexLedgerPiTests
{
    private static ApiContext Memoria() => new(new DbContextOptionsBuilder<ApiContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>
    /// Caso real (B- SYNDET AVENA GEL, Central): lote elaborado de 1,000 g, conteo físico de 61,000 g y luego
    /// 42,000 g consumidos en fórmulas. Antes la lista daba 19,000 y el detalle 60,000 (el último conteo).
    /// </summary>
    [Fact]
    public async Task ListaYDetalle_DanElMismoStock_ConsumoPosteriorAlConteo()
    {
        using var ctx = Memoria();
        ctx.Insumos.Add(new Insumo { Id = 110, Descripcion = "B- SYNDET AVENA GEL", UnidadMedida = "G", FactorCorreccion = "1", Clasificacion = "PI", Tipo = "PI-F%" });
        ctx.ProductosIntermedios.Add(new ProductoIntermedio
        {
            Id = 740, Lote = "PI-25022601", IdInsumo = 110, LoteEstandar = 1000, LoteEstTotal = 1000, Um = "G", IdSede = 15, IdCreador = 1,
            FechaCreacion = new DateTime(2025, 2, 26), FechaVencimiento = DateTime.UtcNow.AddYears(1)
        });
        ctx.StockInsumos.Add(new StockInsumo { Id = 1, IdProductoIntermedio = 740, IdSede = 15, Tipo = "PI", StockDisponible = 60000 });
        ctx.AjusteInsumos.Add(new AjusteInsumo
        {
            Id = 1, IdStockInsumo = 1, StockAnterior = 1000, StockNuevo = 61000, Ajuste = 60000, IdCreador = 1,
            FechaCreacion = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero), Observacion = "conteo"
        });
        // Consumo de 42,000 g al elaborar otro PI (insumo 397) después del conteo
        ctx.ProductosIntermedios.Add(new ProductoIntermedio
        {
            Id = 741, Lote = "PI-OTRO", IdInsumo = 397, LoteEstandar = 50000, Um = "G", IdSede = 15, IdCreador = 1,
            FechaCreacion = new DateTime(2025, 4, 1)
        });
        ctx.InsumoProductoIntermedios.Add(new InsumoProductoIntermedio
        {
            Id = 1, IdInsumo = 110, IdProductoIntermedio = 741, Porcentaje = 84, CantidadLote = 42000, Variable = "A", UnidadMedida = "G"
        });
        await ctx.SaveChangesAsync();

        var lista = await new ProductoIntermedioStockStrategy(ctx, NullLogger<ProductoIntermedioStockStrategy>.Instance).ObtenerStock(15);
        var fila = Assert.Single(lista);
        Assert.Equal(19000m, fila.Stock);
        Assert.Equal(1000m, fila.Entradas);
        Assert.Equal(42000m, fila.Salidas);
        Assert.Equal(60000m, fila.Ajustes);

        var detalle = await new KardexDetalleService(ctx, null!, NullLogger<KardexDetalleService>.Instance).ObtenerDetallePI(110, 15);
        var lote = Assert.Single(detalle);
        Assert.Equal(19000m, lote.Saldo);
        Assert.Equal(42000m, lote.Salidas);
        Assert.Equal("PI-25022601", lote.Lote);
        Assert.Equal(740, lote.IdCompra);
        Assert.Equal("Elaboración PI", lote.TipoOrigen); // antes salía sin origen y el front lo mostraba como COMPRA
        Assert.Equal(fila.Stock, detalle.Sum(d => d.Saldo));
    }

    /// <summary>Sin base real: no abre la conexión y cada consulta devuelve 0 filas. Si EF no puede traducir, falla antes.</summary>
    private sealed class SinBase : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
    {
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult> ConnectionOpeningAsync(
            System.Data.Common.DbConnection c, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEventData d,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult r, CancellationToken t = default)
            => ValueTask.FromResult(Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult.Suppress());
        public override Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult ConnectionOpening(
            System.Data.Common.DbConnection c, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEventData d,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult r)
            => Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult.Suppress();
    }

    private sealed class SinFilas : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public List<string> Sql { get; } = new();
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand cmd, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData d,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> r, CancellationToken t = default)
        {
            Sql.Add(cmd.CommandText);
            return ValueTask.FromResult(Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>
                .SuppressWithResult(new System.Data.DataTable().CreateDataReader()));
        }
    }

    [Fact]
    public async Task LedgerPi_ListaYDetalle_SeTraducenASqlDePostgres()
    {
        var filas = new SinFilas();
        using var ctx = new ApiContext(new DbContextOptionsBuilder<ApiContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x")
            .AddInterceptors(new SinBase(), filas).Options);

        await new KardexLedgerPiService(ctx).CalcularAsync(15, new[] { 110 });
        await new ProductoIntermedioStockStrategy(ctx, NullLogger<ProductoIntermedioStockStrategy>.Instance).ObtenerStock(15);
        await new KardexDetalleService(ctx, null!, NullLogger<KardexDetalleService>.Instance).ObtenerDetallePI(110, 15);

        Assert.True(filas.Sql.Count >= 7);
    }

    [Fact]
    public async Task PtYEco_ListaYDetalle_SeTraducenASqlDePostgres()
    {
        var filas = new SinFilas();
        using var ctx = new ApiContext(new DbContextOptionsBuilder<ApiContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x")
            .AddInterceptors(new SinBase(), filas).Options);

        await new ProductoTerminadoStockStrategy(ctx, NullLogger<ProductoTerminadoStockStrategy>.Instance).ObtenerStock(15);
        await new EconomatoStockStrategy(ctx, NullLogger<EconomatoStockStrategy>.Instance).ObtenerStock(15);
        var detalle = new KardexDetalleService(ctx, null!, NullLogger<KardexDetalleService>.Instance);
        await detalle.ObtenerDetallePT(1, 15);
        await detalle.ObtenerDetalleEconomato(1, 15);

        Assert.NotEmpty(filas.Sql);
    }
}

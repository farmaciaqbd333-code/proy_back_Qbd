using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using proy_back_Qbd.Models.ProductoIntermedio;
using proy_back_Qbd.Services;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Xunit;

namespace Proy_Back_QBD.Tests;

public class ProductoIntermedioListaTests
{
    private static ProductoIntermedioService Servicio(ApiContext contexto) =>
        new(contexto, NullLogger<ProductoIntermedioService>.Instance);

    [Fact]
    public async Task Lista_FiltraPorSede_YAceptaInsumoSinModificador()
    {
        using var contexto = new ApiContext(new DbContextOptionsBuilder<ApiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        contexto.Insumos.Add(new Insumo
        {
            Id = 397,
            Descripcion = "B- GLUCOMILD",
            UnidadMedida = "G",
            FactorCorreccion = "1",
            FormaFarmaceutica = "SOL",
            Tipo = "PI-F%",
            ModificadorId = null
        });
        contexto.ProductosIntermedios.AddRange(
            new ProductoIntermedio { Id = 1, Lote = "PI1", IdInsumo = 397, LoteEstandar = 100, LoteEstTotal = 100, Um = "KG", IdSede = 15, IdCreador = 1, CondicionAlmacenamiento = "15°C - 25°C" },
            new ProductoIntermedio { Id = 2, Lote = "PI2", IdInsumo = 397, LoteEstandar = 100, Um = "G", IdSede = 1, IdCreador = 1 });
        contexto.InsumoProductoIntermedios.Add(new InsumoProductoIntermedio
        {
            Id = 1,
            IdInsumo = 397,
            IdProductoIntermedio = 1,
            Porcentaje = 10,
            CantidadLote = 10,
            Practica = 12,
            Variable = "A",
            UnidadMedida = "G"
        });
        await contexto.SaveChangesAsync();

        var fila = Assert.Single(await Servicio(contexto).ListaProductoIntermedio(15));

        Assert.Equal(1, fila.Id);
        Assert.Equal("PI1", fila.Lote);
        Assert.Equal("MP-QbD-0397", fila.Codigo);
        Assert.Equal("B- GLUCOMILD", fila.Descripcion);
        Assert.Equal("SOL", fila.Tipo);
        Assert.Equal("PI-F%", fila.TipoUso);
        Assert.Equal("G", fila.Um);
        Assert.Equal(100m, fila.Cantidad);
        Assert.True(fila.Descargado);
    }

    [Fact]
    public async Task Lista_SeTraduceASqlDePostgres()
    {
        // Sin servidor en ese puerto: si llega a intentar conectarse, la consulta ya se tradujo a SQL
        using var contexto = new ApiContext(new DbContextOptionsBuilder<ApiContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1").Options);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Servicio(contexto).ListaProductoIntermedio(15));
        Assert.IsType<NpgsqlException>(error.InnerException);
    }
}

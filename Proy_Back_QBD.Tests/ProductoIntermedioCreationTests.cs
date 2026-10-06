using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Proy_back_QBD.Request;
using proy_back_Qbd.Models.Kardex;
using Xunit;
using Xunit.Abstractions;

namespace Proy_Back_QBD.Tests;

public class ProductoIntermedioCreationTests
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;
    private const string ApiKey = "4554654654754";
    private const string BaseAddress = "http://localhost:5051/api/";

    public ProductoIntermedioCreationTests(ITestOutputHelper output)
    {
        _output = output;
        _client = new HttpClient { BaseAddress = new Uri(BaseAddress) };
        _client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
    }

    [Fact]
    public async Task DetalleInsumo_UsesStockDisponibleFromStockInsumo_InsteadOfCantidadSolicitada()
    {
        // Arrange: Insumo 1 (SEPIGEL 305) en sede 15
        int insumoId = 1;
        int sedeId = 15;

        // Act
        var response = await _client.GetAsync($"Kardex/detalle-insumo/{insumoId}?idSede={sedeId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var detalles = await response.Content.ReadFromJsonAsync<List<DetalleInsumoRes>>(jsonOptions);

        Assert.NotNull(detalles);
        Assert.NotEmpty(detalles);

        var primerDetalle = detalles[0];
        _output.WriteLine($"Registro: {primerDetalle.Registro}, Lote: {primerDetalle.Lote}");
        _output.WriteLine($"Saldo: {primerDetalle.Saldo} {primerDetalle.Um}, StockDisponible: {primerDetalle.StockDisponible} {primerDetalle.Um}");
        _output.WriteLine($"CantidadSolicitada (Compra): {primerDetalle.CantidadSolicitada}");

        // El stock disponible debe estar en gramos (>= 100 G) y coincidir con stock_insumo, NO ser 5 (que era KG de compra_insumo)
        Assert.NotNull(primerDetalle.StockDisponible);
        Assert.True(primerDetalle.StockDisponible.Value >= 100m,
            $"El stockDisponible ({primerDetalle.StockDisponible}) debe ser >= 100 G, no la cantidad solicitada en KG (5).");

        // La unidad de medida para el stock debe ser 'G'
        Assert.Equal("G", primerDetalle.Um);

        // Saldo y StockDisponible deben reflejar el stock disponible de stock_insumo
        Assert.Equal(primerDetalle.StockDisponible, primerDetalle.Saldo);
    }

    [Fact]
    public async Task CrearProductoIntermedio_Rejects_WhenQuantityExceedsAvailableStock()
    {
        // Arrange: Obtener el registro y lote activos con stock
        int insumoId = 1;
        int sedeId = 15;

        var detalleResp = await _client.GetFromJsonAsync<List<DetalleInsumoRes>>(
            $"Kardex/detalle-insumo/{insumoId}?idSede={sedeId}",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(detalleResp);
        Assert.NotEmpty(detalleResp);
        var loteInfo = detalleResp[0];

        // Payload con requerimiento desmesurado que supera el stock físico existente
        var req = new CrearProductoIntermedioReq
        {
            Lote = "TEST-EXCEED-" + Guid.NewGuid().ToString()[..8],
            IdInsumo = insumoId,
            LoteEstandar = 1000,
            LoteEstTotal = 1000m,
            TipoUso = "PI-F%",
            Um = "G",
            FechaEmision = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow.AddMonths(6),
            IdElaborado = 1,
            IdSede = sedeId,
            IdCreador = 1,
            Insumos = new List<InsumoProductoIntermedioReq>
            {
                new()
                {
                    IdInsumo = insumoId,
                    CodigoInsumo = "MP-QbD-1",
                    Tipo = "MP",
                    Porcentaje = 100,
                    Variable = "A",
                    CantidadUnidad = 999999m,
                    FactorCorrecion = 1,
                    Dilucion = 1,
                    UnidadMedida = "G",
                    CantidadLote = 999999m,
                    Practica = 999999m,
                    Csp = false,
                    Registro = loteInfo.Registro,
                    Lote = loteInfo.Lote
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("ProductoIntermedio", req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Respuesta de rechazo: {errorBody}");

        Assert.Contains("falta de stock", errorBody, StringComparison.OrdinalIgnoreCase);
        // Debe mostrar el stock disponible en gramos (ej. 49xx G), no 5
        Assert.Contains("G", errorBody);
    }

    [Fact]
    public async Task CrearProductoIntermedio_SuccessfullyConsumesStock_AndDecrementsStockDisponible()
    {
        // Arrange
        int insumoId = 1;
        int sedeId = 15;
        decimal cantidadAConsumir = 5.0m; // 5 gramos

        var detalleAntes = await _client.GetFromJsonAsync<List<DetalleInsumoRes>>(
            $"Kardex/detalle-insumo/{insumoId}?idSede={sedeId}",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(detalleAntes);
        Assert.NotEmpty(detalleAntes);
        var loteInfo = detalleAntes[0];
        decimal stockAntes = loteInfo.StockDisponible ?? 0m;

        Assert.True(stockAntes >= cantidadAConsumir, $"No hay suficiente stock ({stockAntes} G) para la prueba.");

        var req = new CrearProductoIntermedioReq
        {
            Lote = "PI-XUNIT-" + DateTime.UtcNow.ToString("yyMMddHHmmss"),
            IdInsumo = insumoId,
            LoteEstandar = 100,
            LoteEstTotal = 100m,
            TipoUso = "PI-F%",
            Um = "G",
            FechaEmision = DateTime.UtcNow,
            FechaVencimiento = DateTime.UtcNow.AddMonths(6),
            IdElaborado = 1,
            IdSede = sedeId,
            IdCreador = 1,
            Aspecto = "GEL TRASLUCIDO",
            Color = "INCOLORO",
            Olor = "CARACTERISTICO",
            Ph = 5.5m,
            CondicionAlmacenamiento = "15-25 C",
            Insumos = new List<InsumoProductoIntermedioReq>
            {
                new()
                {
                    IdInsumo = insumoId,
                    CodigoInsumo = "MP-QbD-1",
                    Tipo = "MP",
                    Porcentaje = 5,
                    Variable = "A",
                    CantidadUnidad = cantidadAConsumir,
                    FactorCorrecion = 1,
                    Dilucion = 1,
                    UnidadMedida = "G",
                    CantidadLote = cantidadAConsumir,
                    Practica = cantidadAConsumir,
                    Csp = false,
                    Registro = loteInfo.Registro,
                    Lote = loteInfo.Lote
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("ProductoIntermedio", req);
        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Respuesta de creación: {body}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert: Consultar el stock luego de la creación
        var detalleDespues = await _client.GetFromJsonAsync<List<DetalleInsumoRes>>(
            $"Kardex/detalle-insumo/{insumoId}?idSede={sedeId}",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(detalleDespues);
        Assert.NotEmpty(detalleDespues);

        decimal stockDespues = detalleDespues[0].StockDisponible ?? 0m;
        decimal diferencia = stockAntes - stockDespues;

        _output.WriteLine($"Stock previo: {stockAntes} G | Consumo: {cantidadAConsumir} G | Stock posterior: {stockDespues} G (Diferencia: {diferencia} G)");

        Assert.Equal(cantidadAConsumir, diferencia);
    }
}

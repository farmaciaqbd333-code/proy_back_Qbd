using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Controllers;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Security;
using Proy_back_QBD.Services;
using Xunit;

namespace Proy_Back_QBD.Tests;

public class CalculoPagoTests
{
    private static ApiContext CrearContexto() =>
        new(new DbContextOptionsBuilder<ApiContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<ApiContext> CrearContextoConUsuarios()
    {
        ApiContext contexto = CrearContexto();
        contexto.Usuarios.Add(new Usuario { Id = 1, Contrasena = "x", Codigo = "A", Persona = new Persona { Id = 1, NombreCompleto = "ADMIN PRUEBA" } });
        contexto.Usuarios.Add(new Usuario { Id = 2, Contrasena = "y", Codigo = "B", Persona = new Persona { Id = 2, NombreCompleto = "COLABORADOR" } });
        await contexto.SaveChangesAsync();
        return contexto;
    }

    [Fact]
    public async Task Guardar_CreaYLuegoReemplazaElMismoMes()
    {
        await using ApiContext contexto = await CrearContextoConUsuarios();
        var servicio = new AsistenciaService(contexto, null!);

        await servicio.GuardarCalculoPago(2, 2026, 9, new CalculoPagoReq { Datos = "{\"sueldoMensual\":1500}", PagoFinal = 1360.50m }, 1);
        CalculoPagoRes? res = await servicio.GuardarCalculoPago(2, 2026, 9, new CalculoPagoReq { Datos = "{\"sueldoMensual\":1600}", PagoFinal = 1400m }, 1);

        Assert.Equal(1, await contexto.CalculosPago.CountAsync());
        Assert.Equal("{\"sueldoMensual\":1600}", res!.Datos);
        Assert.Equal(1400m, res.PagoFinal);
        Assert.Equal("ADMIN PRUEBA", res.ModificadoPor);
    }

    [Fact]
    public async Task Obtener_SeparaPorColaboradorYMes()
    {
        await using ApiContext contexto = await CrearContextoConUsuarios();
        var servicio = new AsistenciaService(contexto, null!);
        await servicio.GuardarCalculoPago(2, 2026, 9, new CalculoPagoReq { Datos = "{}" }, 1);

        Assert.NotNull(await servicio.ObtenerCalculoPago(2, 2026, 9));
        Assert.Null(await servicio.ObtenerCalculoPago(2, 2026, 10));
        Assert.Null(await servicio.ObtenerCalculoPago(1, 2026, 9));
    }

    [Fact]
    public async Task Guardar_DevuelveNullSiElColaboradorNoExiste()
    {
        await using ApiContext contexto = await CrearContextoConUsuarios();
        var servicio = new AsistenciaService(contexto, null!);

        Assert.Null(await servicio.GuardarCalculoPago(99, 2026, 9, new CalculoPagoReq { Datos = "{}" }, 1));
        Assert.Equal(0, await contexto.CalculosPago.CountAsync());
    }

    [Theory]
    [InlineData(nameof(AsistenciaController.ObtenerCalculoPago))]
    [InlineData(nameof(AsistenciaController.GuardarCalculoPago))]
    public void Endpoints_SonSoloParaAdmin(string metodo)
    {
        var atributo = typeof(AsistenciaController).GetMethod(metodo)!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.Equal(Roles.SoloAdmin, atributo!.Roles);
    }

    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\"a\":1}", true)]
    [InlineData("[]", false)]
    [InlineData("no es json", false)]
    [InlineData("", false)]
    public void DatosCalculoValidos_SoloAceptaObjetosJson(string datos, bool esperado)
    {
        Assert.Equal(esperado, AsistenciaController.DatosCalculoValidos(datos));
    }

    [Fact]
    public void DatosCalculoValidos_RechazaDatosDemasiadoGrandes()
    {
        Assert.False(AsistenciaController.DatosCalculoValidos("{\"a\":\"" + new string('x', 70000) + "\"}"));
    }

    [Theory]
    [InlineData(2026, 9, true)]
    [InlineData(2026, 0, false)]
    [InlineData(2026, 13, false)]
    [InlineData(1999, 1, false)]
    public void PeriodoValido_RevisaAnioYMes(int anio, int mes, bool esperado)
    {
        Assert.Equal(esperado, AsistenciaController.PeriodoValido(anio, mes));
    }
}

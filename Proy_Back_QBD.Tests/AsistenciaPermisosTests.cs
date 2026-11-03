using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Controllers;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Security;
using Proy_back_QBD.Services;
using Xunit;

namespace Proy_Back_QBD.Tests;

public class AsistenciaPermisosTests
{
    private sealed class ServicioFalso : IAsistenciaService
    {
        public AsistenciaCreateReq? Creada;
        public int? SedeConsultada;
        public int? IdConsultado;

        public Task<Asistencia?> Crear(AsistenciaCreateReq asistencia)
        {
            Creada = asistencia;
            return Task.FromResult<Asistencia?>(new Asistencia());
        }

        public Task<AsistenciaByIdRes?> ObtenerPorId(int id, int año, int mes, int idSede)
        {
            IdConsultado = id;
            SedeConsultada = idSede;
            return Task.FromResult<AsistenciaByIdRes?>(new AsistenciaByIdRes());
        }

        public Task<CalculoPagoRes?> ObtenerCalculoPago(int usuarioId, int anio, int mes) =>
            Task.FromResult<CalculoPagoRes?>(null);

        public Task<CalculoPagoRes?> GuardarCalculoPago(int usuarioId, int anio, int mes, CalculoPagoReq request, int modificadorId) =>
            Task.FromResult<CalculoPagoRes?>(null);
    }

    private static AsistenciaController Controlador(ServicioFalso servicio, int usuario, int rol, int sede = 7)
    {
        var identidad = new ClaimsIdentity(new[]
        {
            new Claim("sub", usuario.ToString()),
            new Claim(JwtTokens.ClaimRol, rol.ToString()),
            new Claim(JwtTokens.ClaimSede, sede.ToString()),
        }, "prueba");
        return new AsistenciaController(servicio)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) },
            },
        };
    }

    [Fact]
    public async Task Trabajador_NoPuedeMarcarPorOtro()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.AsesorVentas)
            .CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 6, SedeId = 7 });

        Assert.IsType<ForbidResult>(resultado);
        Assert.Null(servicio.Creada);
    }

    [Fact]
    public async Task Trabajador_MarcaPorSiMismoEnSuSede()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.Formulador)
            .CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 5, SedeId = 99 });

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(7, servicio.Creada!.SedeId);
    }

    [Fact]
    public async Task Trabajador_NoPuedeConsultarAOtro()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.DirectorTecnico)
            .ObtenerAsistenciaByCodigo(6, new AsistenciaByIdReq { Año = 2026, Mes = 9 }, 7);

        Assert.IsType<ForbidResult>(resultado);
        Assert.Null(servicio.IdConsultado);
    }

    [Fact]
    public async Task Trabajador_ConsultaLoSuyoEnSuSede()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.AsesorVentas)
            .ObtenerAsistenciaByCodigo(5, new AsistenciaByIdReq { Año = 2026, Mes = 9 }, 99);

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(5, servicio.IdConsultado);
        Assert.Equal(7, servicio.SedeConsultada);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Supervisor)]
    [InlineData(Roles.S1)]
    public async Task Gestores_SiguenViendoYMarcandoAOtros(int rol)
    {
        var servicio = new ServicioFalso();
        var controlador = Controlador(servicio, 1, rol);

        var marca = await controlador.CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 6, SedeId = 99 });
        var consulta = await controlador.ObtenerAsistenciaByCodigo(6, new AsistenciaByIdReq { Año = 2026, Mes = 9 }, 99);

        Assert.IsType<OkObjectResult>(marca);
        Assert.Equal(99, servicio.Creada!.SedeId);
        Assert.IsType<OkObjectResult>(consulta);
        Assert.Equal(99, servicio.SedeConsultada);
    }
}

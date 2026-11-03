using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
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
        public Dictionary<int, int> SedesActivas = new() { [5] = 7, [6] = 7, [8] = 8 };

        public Task<int?> ObtenerSedeParaMarcar(int? usuarioId) =>
            Task.FromResult<int?>(usuarioId is int id && SedesActivas.TryGetValue(id, out var sede) ? sede : null);

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
    public async Task Trabajador_PuedeMarcarPorOtroDeSuMismaSede()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.AsesorVentas)
            .CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 6, SedeId = 99 });

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(6, servicio.Creada!.CreadorId);
        Assert.Equal(7, servicio.Creada.SedeId);
    }

    [Fact]
    public async Task Marca_RegistraQuienTieneLaSesion_AunqueElClienteEnvieOtroDato()
    {
        var servicio = new ServicioFalso();

        await Controlador(servicio, 5, Roles.AsesorVentas)
            .CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 6, RegistradoPorId = 6 });

        Assert.Equal(5, servicio.Creada!.RegistradoPorId);
    }

    [Fact]
    public async Task Trabajador_NoPuedeMarcarPorUsuarioInactivoOInexistente()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.AsesorVentas)
            .CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 99 });

        Assert.IsType<BadRequestObjectResult>(resultado);
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
    public void ListaParaMarcar_NoExponeContrasenasNiDatosPersonales()
    {
        var propiedades = typeof(UsuarioParaMarcarRes).GetProperties().Select(x => x.Name).OrderBy(x => x);

        Assert.Equal(new[] { "Codigo", "Id", "NombreCompleto" }, propiedades);
    }

    [Fact]
    public async Task ListaParaMarcar_SoloTraeActivosDeLaSedeYNoAdmins()
    {
        var opciones = new DbContextOptionsBuilder<ApiContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var contexto = new ApiContext(opciones);
        contexto.Usuarios.AddRange(
            new Usuario { Id = 1, Contrasena = "a", Codigo = "ADM", TipoId = Roles.Admin, Estado = true, SedeId = 7, Persona = new Persona { Id = 1, NombreCompleto = "ADMIN" } },
            new Usuario { Id = 2, Contrasena = "b", Codigo = "ACT", TipoId = Roles.AsesorVentas, Estado = true, SedeId = 7, Persona = new Persona { Id = 2, NombreCompleto = "ACTIVA" } },
            new Usuario { Id = 3, Contrasena = "c", Codigo = "INA", TipoId = Roles.AsesorVentas, Estado = false, SedeId = 7, Persona = new Persona { Id = 3, NombreCompleto = "INACTIVA" } },
            new Usuario { Id = 4, Contrasena = "d", Codigo = "OTR", TipoId = Roles.AsesorVentas, Estado = true, SedeId = 8, Persona = new Persona { Id = 4, NombreCompleto = "OTRA SEDE" } });
        await contexto.SaveChangesAsync();

        var lista = await new UserService(contexto, null!).ListarParaMarcar(7);

        var unico = Assert.Single(lista);
        Assert.Equal(2, unico.Id);
    }

    [Fact]
    public async Task Trabajador_ConsultaAOtroDeSuMismaSede()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.DirectorTecnico)
            .ObtenerAsistenciaByCodigo(6, new AsistenciaByIdReq { Año = 2026, Mes = 9 }, 99);

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(6, servicio.IdConsultado);
        Assert.Equal(7, servicio.SedeConsultada);
    }

    [Fact]
    public async Task Trabajador_NoPuedeMarcarNiConsultarOtraSede()
    {
        var servicio = new ServicioFalso();
        var controlador = Controlador(servicio, 5, Roles.AsesorVentas);

        var marca = await controlador.CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 8 });
        var consulta = await controlador.ObtenerAsistenciaByCodigo(8, new AsistenciaByIdReq { Año = 2026, Mes = 9 }, 8);

        Assert.IsType<ForbidResult>(marca);
        Assert.IsType<ForbidResult>(consulta);
        Assert.Null(servicio.Creada);
        Assert.Null(servicio.IdConsultado);
    }

    [Fact]
    public async Task Trabajador_SiempreAccedeALoSuyo_AunqueSuTokenTengaOtraSede()
    {
        var servicio = new ServicioFalso();
        var controlador = Controlador(servicio, 5, Roles.AsesorVentas, sede: 3);

        var marca = await controlador.CrearAsistencia(new AsistenciaCreateReq { Tipo = "entrada", CreadorId = 5 });
        var consulta = await controlador.ObtenerAsistenciaByCodigo(5, new AsistenciaByIdReq { Año = 2026, Mes = 9 }, 3);

        Assert.IsType<OkObjectResult>(marca);
        Assert.IsType<OkObjectResult>(consulta);
        Assert.Equal(7, servicio.SedeConsultada);
    }

    [Fact]
    public async Task Trabajador_NoConsultaUsuarioInactivoOInexistente()
    {
        var servicio = new ServicioFalso();

        var resultado = await Controlador(servicio, 5, Roles.AsesorVentas)
            .ObtenerAsistenciaByCodigo(99, new AsistenciaByIdReq { Año = 2026, Mes = 9 }, 7);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Null(servicio.IdConsultado);
    }

    [Fact]
    public void CalculoDePago_SigueSiendoSoloParaAdmin()
    {
        foreach (var metodo in new[] { nameof(AsistenciaController.ObtenerCalculoPago), nameof(AsistenciaController.GuardarCalculoPago) })
        {
            var atributo = typeof(AsistenciaController).GetMethod(metodo)!
                .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
                .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
                .Single();
            Assert.Equal(Roles.SoloAdmin, atributo.Roles);
        }
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

using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Security;
using Proy_back_QBD.Services;
using Xunit;

namespace Proy_Back_QBD.Tests;

public class EstadoTurnoTests
{
    private static readonly DateOnly Hoy = new(2026, 11, 23);
    private static readonly TimeOnly Entrada = new(9, 0);
    private static readonly TimeOnly Salida = new(13, 0);
    private static readonly DateOnly[] MarcoHoy = { Hoy };
    private static readonly DateOnly[] SinMarcas = Array.Empty<DateOnly>();

    private static DateTime A(int hora, int minuto, int diasDespues = 0) =>
        Hoy.AddDays(diasDespues).ToDateTime(new TimeOnly(hora, minuto));

    private static string Calcular(IReadOnlyCollection<DateOnly> dias, DateTime ahora, bool estado = true, int tipo = Roles.AsesorVentas) =>
        EstadoTurno.Calcular(estado, tipo, Entrada, Salida, dias, ahora);

    [Theory]
    [InlineData(8, 30)]   // justo 30 min antes de la entrada
    [InlineData(9, 0)]
    [InlineData(11, 0)]   // en medio del turno
    [InlineData(13, 30)]  // justo 30 min después de la salida
    public void Activo_DentroDelTurnoSiMarcoEntrada(int hora, int minuto)
    {
        Assert.Equal(EstadoTurno.Activo, Calcular(MarcoHoy, A(hora, minuto)));
    }

    [Theory]
    [InlineData(8, 29)]
    [InlineData(13, 31)]
    [InlineData(20, 0)]
    public void Inactivo_FueraDelTurnoAunqueHayaMarcado(int hora, int minuto)
    {
        Assert.Equal(EstadoTurno.Inactivo, Calcular(MarcoHoy, A(hora, minuto)));
    }

    [Fact]
    public void Inactivo_SiNoMarcoEntrada()
    {
        Assert.Equal(EstadoTurno.Inactivo, Calcular(SinMarcas, A(11, 0)));
    }

    [Fact]
    public void Inactivo_SiSoloMarcoEntradaOtroDia()
    {
        Assert.Equal(EstadoTurno.Inactivo, Calcular(new[] { Hoy.AddDays(-1) }, A(11, 0)));
    }

    [Fact]
    public void Baja_MandaSobreTodo()
    {
        Assert.Equal(EstadoTurno.Baja, Calcular(MarcoHoy, A(11, 0), estado: false));
        Assert.Equal(EstadoTurno.Baja, Calcular(MarcoHoy, A(11, 0), estado: false, tipo: Roles.Admin));
    }

    [Fact]
    public void Admin_SiempreActivo()
    {
        Assert.Equal(EstadoTurno.Activo, Calcular(SinMarcas, A(3, 0), tipo: Roles.Admin));
    }

    [Theory]
    [InlineData(Roles.Supervisor)]
    [InlineData(Roles.DirectorTecnico)]
    public void OtrosRoles_SiguenLaRegla(int tipo)
    {
        Assert.Equal(EstadoTurno.Inactivo, Calcular(SinMarcas, A(11, 0), tipo: tipo));
    }

    [Fact]
    public void Inactivo_SinHorario()
    {
        Assert.Equal(EstadoTurno.Inactivo, EstadoTurno.Calcular(true, Roles.AsesorVentas, null, null, MarcoHoy, A(11, 0)));
    }

    [Fact]
    public void TurnoQueCruzaMedianoche_UsaLaEntradaDelDiaAnterior()
    {
        var noche = new TimeOnly(22, 0);
        var manana = new TimeOnly(6, 0);
        DateOnly[] marcoAyer = { Hoy.AddDays(-1) };

        Assert.Equal(EstadoTurno.Activo, EstadoTurno.Calcular(true, Roles.AsesorVentas, noche, manana, marcoAyer, A(2, 0)));
        Assert.Equal(EstadoTurno.Activo, EstadoTurno.Calcular(true, Roles.AsesorVentas, noche, manana, marcoAyer, A(6, 30)));
        Assert.Equal(EstadoTurno.Inactivo, EstadoTurno.Calcular(true, Roles.AsesorVentas, noche, manana, marcoAyer, A(6, 31)));
    }

    [Fact]
    public void DiaLima_ConvierteDesdeUtc()
    {
        // 03:00 UTC del 24 = 22:00 del 23 en Lima
        Assert.Equal(Hoy, EstadoTurno.DiaLima(new DateTime(2026, 11, 24, 3, 0, 0)));
    }

    [Fact]
    public async Task Listar_DevuelveEstadoTurnoSegunLasEntradasMarcadas()
    {
        await using var contexto = new ApiContext(new DbContextOptionsBuilder<ApiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sede = new Sede { Id = 1, Nombre = "PUNO" };
        var tipo = new TipoUsuario { Id = Roles.AsesorVentas, Nombre = "ASESOR" };
        contexto.Add(sede);
        contexto.Add(tipo);
        Usuario Nuevo(int id, bool estado) => new()
        {
            Id = id, Codigo = "U" + id, Contrasena = "x", Estado = estado, SedeId = 1, Sede = sede, TipoId = tipo.Id, Tipo = tipo,
            HorarioEntrada = Entrada, HorarioSalida = Salida,
            Persona = new Persona { Id = id, NombreCompleto = "PERSONA " + id },
        };
        contexto.Usuarios.AddRange(Nuevo(1, true), Nuevo(2, true), Nuevo(3, false));
        // Usuario 1 marcó su entrada hoy a las 08:55 de Lima (13:55 UTC); los demás no
        contexto.Asistencias.Add(new Asistencia { Id = 1, CreadorId = 1, ModificadorId = 1, SedeId = 1, Tipo = "entrada", FechaCreacion = new DateTime(2026, 11, 23, 13, 55, 0) });
        contexto.Asistencias.Add(new Asistencia { Id = 2, CreadorId = 3, ModificadorId = 3, SedeId = 1, Tipo = "entrada", FechaCreacion = new DateTime(2026, 11, 23, 13, 55, 0) });
        await contexto.SaveChangesAsync();

        var servicio = new UserService(contexto, null!);
        List<UsuarioListaRes>? lista = await servicio.Listar(A(10, 0));

        Assert.Equal(EstadoTurno.Activo, lista!.Single(u => u.Id == 1).EstadoTurno);
        Assert.Equal(EstadoTurno.Inactivo, lista.Single(u => u.Id == 2).EstadoTurno);
        Assert.Equal(EstadoTurno.Baja, lista.Single(u => u.Id == 3).EstadoTurno);
        Assert.Null(lista.Single(u => u.Id == 1).MotivoTurno);
        Assert.Equal(EstadoTurno.MotivoSinEntrada, lista.Single(u => u.Id == 2).MotivoTurno);
        Assert.Null(lista.Single(u => u.Id == 3).MotivoTurno);
    }

    [Fact]
    public void Motivo_SinEntradaFueraDeHorarioYSinHorario()
    {
        Assert.Equal(EstadoTurno.MotivoSinEntrada, EstadoTurno.Motivo(true, Roles.AsesorVentas, Entrada, Salida, SinMarcas, A(10, 0)));
        Assert.Equal(EstadoTurno.MotivoFueraDeHorario, EstadoTurno.Motivo(true, Roles.AsesorVentas, Entrada, Salida, MarcoHoy, A(20, 0)));
        Assert.Equal(EstadoTurno.MotivoSinHorario, EstadoTurno.Motivo(true, Roles.AsesorVentas, null, null, MarcoHoy, A(10, 0)));
    }

    [Fact]
    public void Motivo_NuloSiEstaActivoODeBaja()
    {
        Assert.Null(EstadoTurno.Motivo(true, Roles.AsesorVentas, Entrada, Salida, MarcoHoy, A(10, 0)));
        Assert.Null(EstadoTurno.Motivo(false, Roles.AsesorVentas, Entrada, Salida, SinMarcas, A(10, 0)));
    }
}

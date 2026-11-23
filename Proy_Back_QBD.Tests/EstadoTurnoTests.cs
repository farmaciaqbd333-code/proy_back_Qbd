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

    [Theory]
    [InlineData(8, 30, true)]    // 30 min antes de la entrada
    [InlineData(11, 0, true)]
    [InlineData(13, 30, true)]   // 30 min después de la salida
    [InlineData(8, 29, false)]
    [InlineData(13, 31, false)]
    [InlineData(22, 0, false)]
    public void PuedeIngresar_SoloDentroDelHorarioConMargen(int hora, int minuto, bool esperado)
    {
        Assert.Equal(esperado, EstadoTurno.PuedeIngresar(Roles.Supervisor, Entrada, Salida, A(hora, minuto)));
    }

    [Fact]
    public void PuedeIngresar_AdminSiempreYSinHorarioNunca()
    {
        Assert.True(EstadoTurno.PuedeIngresar(Roles.Admin, null, null, A(3, 0)));
        Assert.False(EstadoTurno.PuedeIngresar(Roles.AsesorVentas, null, null, A(10, 0)));
    }

    [Fact]
    public void PuedeIngresar_TurnoQueCruzaMedianoche()
    {
        var entrada = new TimeOnly(22, 0);
        var salida = new TimeOnly(6, 0);
        Assert.True(EstadoTurno.PuedeIngresar(Roles.AsesorVentas, entrada, salida, A(2, 0)));    // madrugada, turno de ayer
        Assert.True(EstadoTurno.PuedeIngresar(Roles.AsesorVentas, entrada, salida, A(21, 30)));  // 30 min antes de entrar
        Assert.False(EstadoTurno.PuedeIngresar(Roles.AsesorVentas, entrada, salida, A(12, 0)));
    }

    [Fact]
    public void AsistenciaHoy_TrabajandoSalioYSinMarcar()
    {
        var entrada = A(8, 55);
        var trabajando = EstadoTurno.AsistenciaHoy(new[] { entrada }, Array.Empty<DateTime>(), A(11, 0));
        Assert.Equal(EstadoTurno.Trabajando, trabajando.Estado);
        Assert.Equal(entrada, trabajando.Entrada);

        var salio = EstadoTurno.AsistenciaHoy(new[] { entrada }, new[] { A(13, 2) }, A(14, 0));
        Assert.Equal(EstadoTurno.Salio, salio.Estado);
        Assert.Equal(A(13, 2), salio.Salida);

        Assert.Equal(EstadoTurno.SinMarcar, EstadoTurno.AsistenciaHoy(Array.Empty<DateTime>(), Array.Empty<DateTime>(), A(11, 0)).Estado);
    }

    [Fact]
    public void AsistenciaHoy_SalidaViejaNoCuentaYEntradaDeAyerVencida()
    {
        // La salida de ayer no cierra la entrada de hoy
        var hoy = EstadoTurno.AsistenciaHoy(new[] { A(9, 0, -1), A(9, 0) }, new[] { A(13, 0, -1) }, A(10, 0));
        Assert.Equal(EstadoTurno.Trabajando, hoy.Estado);
        // Entrada de hace más de 20 h: ya no está trabajando
        Assert.Equal(EstadoTurno.SinMarcar, EstadoTurno.AsistenciaHoy(new[] { A(9, 0, -1) }, Array.Empty<DateTime>(), A(10, 0)).Estado);
        // Turno de noche: entró 22:00 de ayer, a las 02:00 sigue trabajando
        Assert.Equal(EstadoTurno.Trabajando, EstadoTurno.AsistenciaHoy(new[] { A(22, 0, -1) }, Array.Empty<DateTime>(), A(2, 0)).Estado);
    }

    [Fact]
    public void Motivo_NuloSiEstaActivoODeBaja()
    {
        Assert.Null(EstadoTurno.Motivo(true, Roles.AsesorVentas, Entrada, Salida, MarcoHoy, A(10, 0)));
        Assert.Null(EstadoTurno.Motivo(false, Roles.AsesorVentas, Entrada, Salida, SinMarcas, A(10, 0)));
    }
}

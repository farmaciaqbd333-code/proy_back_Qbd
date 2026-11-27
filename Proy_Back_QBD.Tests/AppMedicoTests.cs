using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using proy_back_Qbd.Exceptions;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.AppMedico;
using Proy_back_QBD.Models;
using Proy_back_QBD.Models.AppMedico;
using Proy_back_QBD.Security;
using Proy_back_QBD.Services;
using Proy_back_QBD.Services.AppMedico;
using Xunit;

namespace Proy_Back_QBD.Tests;

/// <summary>App de médicos: login con CMP, catálogo con precio Lista A y pedidos.</summary>
public class AppMedicoTests
{
    private const string Secreto = "clave-de-pruebas-de-al-menos-32-caracteres!!";

    private static ApiContext Memoria() => new(new DbContextOptionsBuilder<ApiContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AppMedicoService Servicio(ApiContext ctx) =>
        new(ctx, Options.Create(new JwtOptions { Secret = Secreto }));

    /// <summary>
    /// Directorio: la Dra. Pérez (CMP 091379) está en Central (1) y Puno (2); el Dr. Rojas (CMP 24669) solo en Central.
    /// Lista de precios: 3 productos activos y 1 desactivado.
    /// </summary>
    private static async Task<ApiContext> Datos()
    {
        var ctx = Memoria();
        ctx.Sedes.AddRange(new Sede { Id = 1, Nombre = "CENTRAL" }, new Sede { Id = 2, Nombre = "PUNO" }, new Sede { Id = 3, Nombre = "JULIACA" });
        ctx.Especialidads.AddRange(new Especialidad { Id = 1, Nombre = "DERMATOLOGIA" }, new Especialidad { Id = 2, Nombre = "MEDICINA INTERNA" });
        ctx.Personas.AddRange(new Persona { Id = 1, NombreCompleto = "ANA PEREZ " }, new Persona { Id = 2, NombreCompleto = "LUIS ROJAS" });
        ctx.Medicos.AddRange(
            new Medico { Id = 10, Cmp = "091379", SedeId = 1, PersonaId = 1, EspecialidadId = 1, CreadorId = 1, ModificadorId = 1 },
            new Medico { Id = 11, Cmp = "91379", SedeId = 2, PersonaId = 1, EspecialidadId = 1, CreadorId = 1, ModificadorId = 1 },
            new Medico { Id = 20, Cmp = "24669", SedeId = 1, PersonaId = 2, EspecialidadId = 2, CreadorId = 1, ModificadorId = 1 },
            new Medico { Id = 30, Cmp = null, SedeId = 1, PersonaId = 2, EspecialidadId = 2, CreadorId = 1, ModificadorId = 1 });
        ctx.ListaPrecios.AddRange(
            new ListaPrecio { Id = 1, Producto = "ABLANDADOR DE COMEDONES SOL CSP 20 ML", Ifas = "RESORCINA, AC.SALICILICO", CodigoA = "A001", PrecioA = 27m, PrecioB = 30m, PrecioC = 54m, Especialidad1 = "DERM", Especialidad2 = "COSM", Enfermedades = "ACNE", Categoria = "FM" },
            new ListaPrecio { Id = 2, Producto = "ACICLOVIR JARABE C.S.P 120 ML", Ifas = "ACICLOVIR 200mg/5ml", CodigoA = "A002", PrecioA = 37m, PrecioB = 43m, PrecioC = 75m, Especialidad1 = "PED", Categoria = "FM" },
            new ListaPrecio { Id = 3, Producto = "SHAMPOO VETERINARIO", CodigoA = "V001", PrecioA = null, Especialidad1 = "DERM", Categoria = "FMVET" },
            new ListaPrecio { Id = 4, Producto = "PRODUCTO RETIRADO", CodigoA = "X", PrecioA = 10m, Activo = false });
        await ctx.SaveChangesAsync();
        return ctx;
    }

    private static SesionMedico SesionDe(MedicoLoginRes login)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);
        return new SesionMedico(int.Parse(jwt.Claims.First(c => c.Type == AppMedicoTokens.ClaimAcceso).Value),
                                int.Parse(jwt.Claims.First(c => c.Type == AppMedicoTokens.ClaimVersion).Value));
    }

    private static async Task<MedicoLoginRes> EntrarComoPerez(AppMedicoService s)
    {
        await s.AsignarAcceso(10, "secreto1", usuarioId: 1);
        return await s.Login(new MedicoLoginReq { Cmp = "091379", Contrasena = "secreto1" });
    }

    // ===================== Reglas =====================

    [Theory]
    [InlineData("091379", "91379")]
    [InlineData(" 24 669 ", "24669")]
    [InlineData("000", "000")]
    [InlineData(null, "")]
    public void NormalizarCmp_QuitaEspaciosYCerosALaIzquierda(string? cmp, string esperado) =>
        Assert.Equal(esperado, AppMedicoReglas.NormalizarCmp(cmp));

    [Theory]
    [InlineData("DERMATOLOGIA", "DERM")]
    [InlineData("DERMOESTÉTICA", "DERM,COSM")]
    [InlineData("NEUMOLOGIA PEDIATRICA", "PED")]
    [InlineData("GINECOLOGIA Y OBSTETRICIA", "GO")]
    [InlineData("GERIATRIA", "GER")]
    [InlineData("MEDICINA INTERNA", "")]
    [InlineData("MED.CIRUJANO", "")]
    [InlineData(null, "")]
    public void EspecialidadesSugeridas_SoloCodigosQueExistenEnLaLista(string? especialidad, string esperado)
    {
        var lista = new[] { "DERM", "COSM", "PED", "GER", "ORL", "GO", "ÁC" };
        Assert.Equal(esperado, string.Join(",", AppMedicoReglas.EspecialidadesSugeridas(especialidad, lista)));
    }

    [Fact]
    public void GenerarTemporal_OchoCaracteresSinLetrasConfusas()
    {
        var t = AppMedicoReglas.GenerarTemporal();
        Assert.Equal(8, t.Length);
        Assert.DoesNotContain(t, c => "0O1lI".Contains(c));
    }

    // ===================== Acceso y login =====================

    [Fact]
    public async Task AsignarAcceso_GuardaHashBCrypt_YDevuelveTemporalUnaVez()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);

        var res = await s.AsignarAcceso(10, null, usuarioId: 7);

        Assert.Equal("91379", res.Cmp);
        Assert.NotNull(res.ContrasenaTemporal);
        var acceso = await ctx.MedicoAccesos.SingleAsync();
        Assert.StartsWith("$2", acceso.ContrasenaHash);
        Assert.DoesNotContain(res.ContrasenaTemporal!, acceso.ContrasenaHash);
        Assert.True(acceso.DebeCambiar);
        Assert.Equal(7, acceso.ModificadorId);

        var login = await s.Login(new MedicoLoginReq { Cmp = "91379", Contrasena = res.ContrasenaTemporal });
        Assert.True(login.Medico.DebeCambiarContrasena);
        Assert.Equal("ANA PEREZ", login.Medico.Nombre);
        Assert.Equal(10, login.Medico.MedicoId);
        Assert.Equal(1, login.Medico.SedeId);
        Assert.Equal(new[] { "DERM" }, login.Medico.EspecialidadesSugeridas);
    }

    [Fact]
    public async Task AsignarAcceso_MedicoSinCmp_NoSePuede()
    {
        using var ctx = await Datos();
        await Assert.ThrowsAsync<BadRequestException>(() => Servicio(ctx).AsignarAcceso(30, "secreto1", 1));
        await Assert.ThrowsAsync<NotFoundException>(() => Servicio(ctx).AsignarAcceso(999, "secreto1", 1));
    }

    [Fact]
    public async Task Login_ContrasenaIncorrecta_YBloqueoTrasCincoIntentos()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        await s.AsignarAcceso(20, "correcta1", 1);

        await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Login(new MedicoLoginReq { Cmp = "24669", Contrasena = "mala" }));
        await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Login(new MedicoLoginReq { Cmp = "99999", Contrasena = "correcta1" }));
        await Assert.ThrowsAsync<BadRequestException>(() => s.Login(new MedicoLoginReq { Cmp = "", Contrasena = "x" }));

        for (int i = 0; i < 4; i++)
            await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Login(new MedicoLoginReq { Cmp = "24669", Contrasena = "mala" }));

        // Quinto intento fallido acumulado: bloqueado aunque ahora ponga la correcta
        var ex = await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Login(new MedicoLoginReq { Cmp = "24669", Contrasena = "correcta1" }));
        Assert.Contains("intentos", ex.Message);
    }

    [Fact]
    public async Task Login_AccesoDesactivado_NoEntra()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        await s.AsignarAcceso(20, "correcta1", 1);
        await s.CambiarActivoAcceso(20, false, 1);

        var ex = await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Login(new MedicoLoginReq { Cmp = "24669", Contrasena = "correcta1" }));
        Assert.Contains("desactivado", ex.Message);
    }

    [Fact]
    public async Task Token_EsDeMedico_NoSirveComoTokenDelPersonal()
    {
        using var ctx = await Datos();
        var login = await EntrarComoPerez(Servicio(ctx));
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        var principal = handler.ValidateToken(login.Token, AppMedicoTokens.Validacion(Secreto), out _);
        Assert.Equal("91379", principal.FindFirst(AppMedicoTokens.ClaimCmp)?.Value);
        Assert.True(AppMedicoTokens.AccesoId(principal) > 0);
        // Los filtros del personal no lo confunden con un usuario
        Assert.Equal(0, JwtTokens.UsuarioId(principal));
        Assert.Equal(0, JwtTokens.Rol(principal));

        var parametrosPersonal = new TokenValidationParameters
        {
            ValidIssuer = JwtTokens.Issuer, ValidAudience = JwtTokens.Audience, IssuerSigningKey = JwtTokens.CrearClave(Secreto),
        };
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(login.Token, parametrosPersonal, out _));
    }

    [Fact]
    public async Task RestablecerContrasena_InvalidaLaSesionAnterior()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));
        Assert.Equal("91379", (await s.Perfil(sesion)).Cmp);

        await s.AsignarAcceso(11, "otraClave9", 1); // misma doctora, fila de Puno

        await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Perfil(sesion));
        Assert.Single(ctx.MedicoAccesos); // un solo acceso por CMP
    }

    [Fact]
    public async Task CambiarContrasena_ValidaActual_YDevuelveTokenNuevo()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));

        await Assert.ThrowsAsync<BadRequestException>(() => s.CambiarContrasena(sesion, new MedicoCambiarContrasenaReq { Actual = "mala", Nueva = "nueva123" }));
        await Assert.ThrowsAsync<BadRequestException>(() => s.CambiarContrasena(sesion, new MedicoCambiarContrasenaReq { Actual = "secreto1", Nueva = "123" }));

        var res = await s.CambiarContrasena(sesion, new MedicoCambiarContrasenaReq { Actual = "secreto1", Nueva = "nueva123" });
        Assert.False(res.Medico.DebeCambiarContrasena);
        await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Perfil(sesion)); // token viejo
        Assert.Equal("91379", (await s.Perfil(SesionDe(res))).Cmp);
        await s.Login(new MedicoLoginReq { Cmp = "091379", Contrasena = "nueva123" });
    }

    // ===================== Catálogo =====================

    [Fact]
    public async Task Catalogo_SoloActivos_PrecioListaA_YFiltros()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));

        var todo = await s.Catalogo(sesion, null, null, null, false, 1, 50);
        Assert.Equal(3, todo.Total);
        var ablandador = todo.Items.Single(i => i.Id == 1);
        Assert.Equal(27m, ablandador.Precio); // Lista A (MED), no B ni C
        Assert.Equal("A001", ablandador.Codigo);
        Assert.Equal(new[] { "DERM", "COSM" }, ablandador.Especialidades);
        Assert.Null(todo.Items.Single(i => i.Id == 3).Precio);

        Assert.Equal(new[] { 1, 3 }, (await s.Catalogo(sesion, null, "derm", null, false, 1, 50)).Items.Select(i => i.Id));
        Assert.Equal(new[] { 3 }, (await s.Catalogo(sesion, null, "DERM", "fmvet", false, 1, 50)).Items.Select(i => i.Id));
        Assert.Equal(new[] { 2 }, (await s.Catalogo(sesion, "aciclovir", null, null, false, 1, 50)).Items.Select(i => i.Id));
        Assert.Equal(new[] { 1 }, (await s.Catalogo(sesion, "acne", null, null, false, 1, 50)).Items.Select(i => i.Id));

        var pagina2 = await s.Catalogo(sesion, null, null, null, false, 2, 2);
        Assert.Equal(3, pagina2.Total);
        Assert.Single(pagina2.Items);
    }

    [Fact]
    public async Task Favoritos_SeMarcanPorCmp_YFiltran()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));

        await s.MarcarFavorito(sesion, 2, true);
        await s.MarcarFavorito(sesion, 2, true); // no duplica
        Assert.Single(ctx.MedicoProductoFavoritos);
        await Assert.ThrowsAsync<NotFoundException>(() => s.MarcarFavorito(sesion, 4, true)); // desactivado

        var favs = await s.Catalogo(sesion, null, null, null, true, 1, 50);
        Assert.Equal(new[] { 2 }, favs.Items.Select(i => i.Id));
        Assert.True(favs.Items[0].Favorito);

        await s.MarcarFavorito(sesion, 2, false);
        Assert.Empty((await s.Catalogo(sesion, null, null, null, true, 1, 50)).Items);
    }

    // ===================== Productos asignados (Lista de Precios → Médicos) =====================

    [Fact]
    public async Task Asignacion_DirectorioDeTodasLasSedes_AsignarYQuitar()
    {
        using var ctx = await Datos();
        var pm = new ProductoMedicoService(ctx);

        var directorio = await pm.MedicosAsignables();
        Assert.Equal(new[] { 10, 11, 20, 30 }, directorio.Select(m => m.Id).OrderBy(i => i));
        Assert.Equal("PUNO", directorio.Single(m => m.Id == 11).Sede);
        Assert.Equal("ANA PEREZ", directorio.Single(m => m.Id == 10).Nombre);

        var asignados = await pm.Asignar(1, new[] { 10, 20 }, usuarioId: 5);
        Assert.Equal(new[] { 10, 20 }, asignados.Select(a => a.MedicoId).OrderBy(i => i));
        Assert.Equal("91379", ctx.MedicoProductoAsignados.Single(a => a.MedicoId == 10).Cmp);
        Assert.Equal(5, ctx.MedicoProductoAsignados.Single(a => a.MedicoId == 10).CreadorId);

        // La misma médica en otra sede (mismo CMP) o la misma fila: no se duplica
        await pm.Asignar(1, new[] { 11, 10 }, usuarioId: 5);
        Assert.Equal(2, ctx.MedicoProductoAsignados.Count());

        await Assert.ThrowsAsync<NotFoundException>(() => pm.Asignar(1, new[] { 999 }, 5));
        await Assert.ThrowsAsync<NotFoundException>(() => pm.Asignar(4, new[] { 10 }, 5)); // producto desactivado
        await Assert.ThrowsAsync<BadRequestException>(() => pm.Asignar(1, Array.Empty<int>(), 5));

        await pm.Asignar(2, new[] { 30 }, 5); // médico sin CMP
        Assert.Equal(3, (await pm.Asignaciones()).Count);

        var quedan = await pm.Quitar(1, 20);
        Assert.Equal(new[] { 10 }, quedan.Select(a => a.MedicoId));
        await Assert.ThrowsAsync<NotFoundException>(() => pm.Quitar(1, 20));
    }

    [Fact]
    public async Task Asignacion_LaAppMuestraLosProductosAsignadosPorCmp()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));
        var pm = new ProductoMedicoService(ctx);

        // Asignado a su fila de Puno (#11): igual lo ve, porque es el mismo CMP
        await pm.Asignar(2, new[] { 11 }, 1);
        await pm.Asignar(3, new[] { 20 }, 1); // de otro médico

        var mios = await s.Catalogo(sesion, null, null, null, false, 1, 50, soloAsignados: true);
        Assert.Equal(new[] { 2 }, mios.Items.Select(i => i.Id));
        Assert.True(mios.Items[0].Asignado);

        var todo = await s.Catalogo(sesion, null, null, null, false, 1, 50);
        Assert.Equal(3, todo.Total); // el catálogo completo sigue igual
        Assert.Equal(new[] { 2 }, todo.Items.Where(i => i.Asignado).Select(i => i.Id));

        await s.MarcarFavorito(sesion, 1, true);
        Assert.Empty((await s.Catalogo(sesion, null, null, null, true, 1, 50, soloAsignados: true)).Items);
    }

    // ===================== Pedidos =====================

    [Fact]
    public async Task CrearPedido_PreciosDelServidor_MedicoDeLaSedeElegida()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));

        var pedido = await s.CrearPedido(sesion, new PedidoMedicoCreateReq
        {
            SedeId = 2,
            PacienteNombre = "  JUAN QUISPE ",
            PacienteDni = "12345678",
            Observacion = "Urgente",
            Items =
            {
                new PedidoMedicoItemReq { ProductoId = 1, Cantidad = 2, Observacion = "aplicar de noche" },
                new PedidoMedicoItemReq { ProductoId = 3, Cantidad = 1 },
            },
        });

        Assert.Equal(EstadosPedidoMedico.Pendiente, pedido.Estado);
        Assert.Equal(11, pedido.MedicoId); // fila de la doctora en Puno
        Assert.Equal("PUNO", pedido.Sede);
        Assert.Equal("JUAN QUISPE", pedido.PacienteNombre);
        Assert.Equal(54m, pedido.Total); // 2 × 27 + 1 × (sin precio = 0)
        Assert.Equal(2, pedido.CantidadItems);
        var d1 = pedido.Detalles.Single(d => d.ProductoId == 1);
        Assert.Equal(27m, d1.PrecioUnitario);
        Assert.Equal("ABLANDADOR DE COMEDONES SOL CSP 20 ML", d1.Producto);
        Assert.Equal("aplicar de noche", d1.Observacion);
        Assert.Equal("ANA PEREZ", pedido.Medico);

        // En una sede donde no está registrada se usa su primera fila del directorio
        var otro = await s.CrearPedido(sesion, new PedidoMedicoCreateReq { SedeId = 3, Items = { new() { ProductoId = 2, Cantidad = 1 } } });
        Assert.Equal(10, otro.MedicoId);
        // Sin sede: la de su primera fila (Central)
        var sinSede = await s.CrearPedido(sesion, new PedidoMedicoCreateReq { Items = { new() { ProductoId = 2, Cantidad = 1 } } });
        Assert.Equal(1, sinSede.SedeId);
    }

    [Theory]
    [InlineData(0, 1, null, null)]       // cantidad 0
    [InlineData(1000, 1, null, null)]    // cantidad excesiva
    [InlineData(1, 4, null, null)]       // producto desactivado
    [InlineData(1, 999, null, null)]     // producto inexistente
    [InlineData(1, 1, "1234", null)]     // documento corto
    [InlineData(1, 1, null, 99)]         // sede inexistente
    public async Task CrearPedido_DatosInvalidos_NoSeGuarda(int cantidad, int productoId, string? dni, int? sedeId)
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));

        await Assert.ThrowsAsync<BadRequestException>(() => s.CrearPedido(sesion, new PedidoMedicoCreateReq
        {
            SedeId = sedeId, PacienteDni = dni, Items = { new() { ProductoId = productoId, Cantidad = cantidad } },
        }));
        await Assert.ThrowsAsync<BadRequestException>(() => s.CrearPedido(sesion, new PedidoMedicoCreateReq()));
        Assert.Empty(ctx.PedidosMedico);
    }

    [Fact]
    public async Task MisPedidos_SoloLosPropios_YAnularSoloPendiente()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var perez = SesionDe(await EntrarComoPerez(s));
        await s.AsignarAcceso(20, "rojas123", 1);
        var rojas = SesionDe(await s.Login(new MedicoLoginReq { Cmp = "24669", Contrasena = "rojas123" }));

        var p1 = await s.CrearPedido(perez, new PedidoMedicoCreateReq { Items = { new() { ProductoId = 1, Cantidad = 1 } } });
        var p2 = await s.CrearPedido(perez, new PedidoMedicoCreateReq { Items = { new() { ProductoId = 2, Cantidad = 3 } } });
        await s.CrearPedido(rojas, new PedidoMedicoCreateReq { Items = { new() { ProductoId = 2, Cantidad = 1 } } });

        var mios = await s.MisPedidos(perez);
        Assert.Equal(new[] { p2.Id, p1.Id }, mios.Select(p => p.Id)); // más reciente primero
        Assert.Equal(3, mios[0].Detalles.Single().Cantidad);
        await Assert.ThrowsAsync<NotFoundException>(() => s.MiPedido(rojas, p1.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => s.AnularMiPedido(rojas, p1.Id));

        // El personal lo recibe: el médico ya no puede anularlo
        await s.CambiarEstado(p1.Id, new PedidoMedicoEstadoReq { Estado = "recibido" }, usuarioId: 5);
        await Assert.ThrowsAsync<BadRequestException>(() => s.AnularMiPedido(perez, p1.Id));

        var anulado = await s.AnularMiPedido(perez, p2.Id);
        Assert.Equal(EstadosPedidoMedico.Anulado, anulado.Estado);
    }

    [Fact]
    public async Task CambiarEstado_Personal_ReglasYMensajeParaElMedico()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var perez = SesionDe(await EntrarComoPerez(s));
        var p = await s.CrearPedido(perez, new PedidoMedicoCreateReq { SedeId = 1, Items = { new() { ProductoId = 1, Cantidad = 1 } } });

        Assert.Equal(1, await s.ContarPendientes(1));
        Assert.Equal(0, await s.ContarPendientes(2));

        await Assert.ThrowsAsync<BadRequestException>(() => s.CambiarEstado(p.Id, new PedidoMedicoEstadoReq { Estado = "VOLANDO" }, 5));
        await Assert.ThrowsAsync<BadRequestException>(() => s.CambiarEstado(p.Id, new PedidoMedicoEstadoReq { Estado = "ANULADO" }, 5)); // sin motivo
        await Assert.ThrowsAsync<NotFoundException>(() => s.CambiarEstado(999, new PedidoMedicoEstadoReq { Estado = "LISTO" }, 5));

        var listo = await s.CambiarEstado(p.Id, new PedidoMedicoEstadoReq { Estado = "listo", Respuesta = "Puede recogerlo desde las 4 pm" }, 5);
        Assert.Equal(EstadosPedidoMedico.Listo, listo.Estado);
        Assert.Equal("Puede recogerlo desde las 4 pm", (await s.MiPedido(perez, p.Id)).Respuesta);

        await s.CambiarEstado(p.Id, new PedidoMedicoEstadoReq { Estado = "ENTREGADO" }, 5);
        await Assert.ThrowsAsync<BadRequestException>(() => s.CambiarEstado(p.Id, new PedidoMedicoEstadoReq { Estado = "EN PROCESO" }, 5));

        var lista = await s.ListarPedidos(1, "entregado", 30);
        Assert.Equal(p.Id, Assert.Single(lista).Id);
        Assert.Empty(await s.ListarPedidos(2, null, null));
        Assert.Equal(5, (await ctx.PedidosMedico.SingleAsync()).ModificadorId);
    }

    // ===================== Traducción a PostgreSQL =====================

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
    public async Task Consultas_SeTraducenASqlDePostgres()
    {
        var filas = new SinFilas();
        using var ctx = new ApiContext(new DbContextOptionsBuilder<ApiContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x")
            .AddInterceptors(new SinBase(), filas).Options);

        // Sin ejecutar: el SQL que arma EF para el catálogo y la búsqueda por CMP
        var catalogo = AppMedicoConsultas.Catalogo(ctx, "acne", "DERM", "FM", new[] { 1, 2 }).ToQueryString();
        Assert.Contains("lista_precios_av", catalogo);
        Assert.Contains("especialidad1", catalogo);
        var cmp = AppMedicoConsultas.FilasDelCmp(ctx, "91379").ToQueryString();
        Assert.Contains("medicos", cmp);
        Assert.Contains("nombreCompleto", cmp);

        // Ejecutadas contra un PostgreSQL simulado (0 filas)
        var s = Servicio(ctx);
        Assert.Empty(await s.ListarPedidos(1, "PENDIENTE", 30));
        var pm = new ProductoMedicoService(ctx);
        Assert.Empty(await pm.Asignaciones());
        Assert.Empty(await pm.MedicosAsignables());
        Assert.Empty(await s.Accesos());
        await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Login(new MedicoLoginReq { Cmp = "91379", Contrasena = "x" }));
        await Assert.ThrowsAsync<NoAutorizadoException>(() => s.Perfil(new SesionMedico(1, 1)));

        Assert.Contains(filas.Sql, q => q.Contains("pedidos_medico") && q.Contains("sedes"));
        Assert.Contains(filas.Sql, q => q.Contains("medico_acceso"));
    }

    [Fact]
    public void SqlDeArranque_SinFkHaciaTablasAntiguas_YSeparadoPorSentencias()
    {
        var sentencias = DatabaseStartupMigrator.SentenciasSql(DatabaseStartupMigrator.LeerSqlEmbebido("app_medico.sql"));

        foreach (var tabla in new[] { "medico_acceso", "medico_producto_favorito", "pedidos_medico", "pedidos_medico_detalle" })
            Assert.Contains(sentencias, s => s.StartsWith($"CREATE TABLE IF NOT EXISTS {tabla} ("));
        // Un FK a medicos/sedes/pedidos/usuarios/lista_precios_av hacía fallar la creación en producción
        foreach (var antigua in new[] { "medicos(", "sedes(", "pedidos(", "usuarios(", "lista_precios_av(" })
            Assert.DoesNotContain(sentencias, s => s.Contains($"REFERENCES {antigua}"));
        Assert.All(sentencias, s => Assert.DoesNotContain("--", s));
    }

    // ===================== Convertir en pedido de Gestión de Pedidos =====================

    [Theory]
    [InlineData("PENDIENTE", EstadosPedidoMedico.Recibido)]
    [InlineData("EN PROCESO", EstadosPedidoMedico.EnProceso)]
    [InlineData("TERMINADO", EstadosPedidoMedico.Listo)]
    [InlineData("PT", EstadosPedidoMedico.Listo)]
    [InlineData("ENTREGADO", EstadosPedidoMedico.Entregado)]
    [InlineData("SEGUIMIENTO", EstadosPedidoMedico.Entregado)]
    [InlineData("DEVUELTO", EstadosPedidoMedico.Anulado)]
    [InlineData("RARO", EstadosPedidoMedico.Recibido)]
    public void DesdePedido_TraduceElEstadoDelPedidoReal(string real, string esperado) =>
        Assert.Equal(esperado, EstadosPedidoMedico.DesdePedido(real, EstadosPedidoMedico.Recibido));

    [Fact]
    public async Task Convertir_ElPacienteEsElMedico_SeRegistraUnaVez_YElEstadoSaleDelPedido()
    {
        using var ctx = await Datos();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));
        var pm = await s.CrearPedido(sesion, new PedidoMedicoCreateReq { SedeId = 1, Items = { new() { ProductoId = 1, Cantidad = 1 } } });

        // La doctora todavía no es paciente de Central: se registra con su misma persona
        var p1 = await s.PacienteDelMedico(pm.Id, sedeId: 1, usuarioId: 7);
        Assert.True(p1.Creado);
        Assert.Equal("ANA PEREZ", p1.Nombre);
        var paciente = await ctx.Pacientes.SingleAsync();
        Assert.Equal(1, paciente.PersonaId);
        Assert.Equal(1, paciente.SedeId);
        Assert.Equal(7, paciente.CreadorId);

        // La segunda vez se reutiliza
        var p2 = await s.PacienteDelMedico(pm.Id, sedeId: 1, usuarioId: 7);
        Assert.False(p2.Creado);
        Assert.Equal(p1.PacienteId, p2.PacienteId);
        Assert.Equal(1, await ctx.Pacientes.CountAsync());

        // Se crea el pedido real y se enlaza: el médico ve el estado del laboratorio
        ctx.Pedidos.Add(new Pedido { Id = 500, Estado = "PENDIENTE", SedeId = 1, MedicoId = 10, PacienteId = p1.PacienteId, CreadorId = 7, ModificadorId = 7 });
        await ctx.SaveChangesAsync();
        var enlazado = await s.CambiarEstado(pm.Id, new PedidoMedicoEstadoReq { Estado = EstadosPedidoMedico.Recibido, PedidoId = 500 }, 7);
        Assert.Equal(500, enlazado.PedidoId);
        Assert.Equal(EstadosPedidoMedico.Recibido, enlazado.Estado);

        var real = await ctx.Pedidos.SingleAsync(x => x.Id == 500);
        real.Estado = "TERMINADO";
        await ctx.SaveChangesAsync();
        var visto = await s.MiPedido(sesion, pm.Id);
        Assert.Equal(EstadosPedidoMedico.Listo, visto.Estado);
        Assert.Equal("TERMINADO", visto.PedidoEstado);

        // Ya enlazado: no se convierte otra vez ni se cambia el estado a mano
        await Assert.ThrowsAsync<BadRequestException>(() => s.PacienteDelMedico(pm.Id, 1, 7));
        await Assert.ThrowsAsync<BadRequestException>(() =>
            s.CambiarEstado(pm.Id, new PedidoMedicoEstadoReq { Estado = EstadosPedidoMedico.EnProceso }, 7));

        // Otro pedido de médico no puede enlazarse al mismo pedido real
        var otro = await s.CrearPedido(sesion, new PedidoMedicoCreateReq { SedeId = 1, Items = { new() { ProductoId = 2, Cantidad = 1 } } });
        await Assert.ThrowsAsync<BadRequestException>(() =>
            s.CambiarEstado(otro.Id, new PedidoMedicoEstadoReq { Estado = EstadosPedidoMedico.Recibido, PedidoId = 500 }, 7));
    }

    [Fact]
    public async Task Convertir_ReutilizaAlPacienteConElMismoDni()
    {
        using var ctx = await Datos();
        var persona = await ctx.Personas.SingleAsync(p => p.Id == 1);
        persona.Dni = "40112233";
        ctx.Personas.Add(new Persona { Id = 9, NombreCompleto = "ANA PEREZ (PACIENTE)", Dni = "40112233" });
        ctx.Pacientes.Add(new Paciente { Id = 70, PersonaId = 9, SedeId = 1, CreadorId = 1, ModificadorId = 1 });
        await ctx.SaveChangesAsync();
        var s = Servicio(ctx);
        var sesion = SesionDe(await EntrarComoPerez(s));
        var pm = await s.CrearPedido(sesion, new PedidoMedicoCreateReq { SedeId = 1, Items = { new() { ProductoId = 1, Cantidad = 1 } } });

        var p = await s.PacienteDelMedico(pm.Id, sedeId: 1, usuarioId: 7);
        Assert.False(p.Creado);
        Assert.Equal(70, p.PacienteId);
    }
}

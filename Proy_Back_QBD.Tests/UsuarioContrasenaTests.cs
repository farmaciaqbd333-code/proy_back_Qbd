using System.Net;
using System.Net.Http.Json;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Proy_back_QBD.Profiles;
using Proy_back_QBD.Services;

namespace Proy_Back_QBD.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hashear_GeneraHashVerificable()
    {
        string hash = PasswordHasher.Hashear("  secreta1 ");

        Assert.True(PasswordHasher.EsHash(hash));
        Assert.True(PasswordHasher.Verificar("secreta1", hash));
        Assert.False(PasswordHasher.Verificar("otra", hash));
    }

    [Theory]
    [InlineData("secreta1", "secreta1", true)]
    [InlineData("secreta1", "secreta1   ", true)]
    [InlineData("secreta1", "otra", false)]
    [InlineData("", "", false)]
    [InlineData("secreta1", null, false)]
    public void Verificar_ContrasenaLegada(string contrasena, string? almacenada, bool esperado)
    {
        Assert.Equal(esperado, PasswordHasher.Verificar(contrasena, almacenada));
    }

    [Fact]
    public void Verificar_ElHashNoSirveComoContrasena()
    {
        string hash = PasswordHasher.Hashear("secreta1");

        Assert.False(PasswordHasher.Verificar(hash, hash));
    }

    [Fact]
    public void Verificar_AceptaFormato2aDePgcrypto()
    {
        // "$2a$" es el formato de crypt() de pgcrypto; hash generado con el paquete bcrypt de Python
        const string hash = "$2a$05$CCCCCCCCCCCCCCCCCCCCC.E5YPO9kmyuRGyh0XouQYb4YMJKvyOeW";

        Assert.True(PasswordHasher.EsHash(hash));
        Assert.True(PasswordHasher.Verificar("U*U", hash));
    }

    [Fact]
    public void Verificar_HashConRellenoDeColumnaChar()
    {
        string hash = PasswordHasher.Hashear("secreta1");

        Assert.True(PasswordHasher.Verificar("secreta1", hash + "      "));
    }
}

public class UsuarioApiTests : IClassFixture<UsuarioApiTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("API-KEY", ApiKey);
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApiContext>) ||
                    d.ServiceType == typeof(IDbContextOptionsConfiguration<ApiContext>)).ToList())
                {
                    services.Remove(descriptor);
                }
                services.AddDbContext<ApiContext>(o => o.UseInMemoryDatabase(_dbName));
            });
        }
    }

    private const string ApiKey = "clave-de-prueba";
    private static int _secuencia = 100;
    private readonly Factory _factory;
    private readonly HttpClient _client;

    public UsuarioApiTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
    }

    private async Task<T> ConDb<T>(Func<ApiContext, Task<T>> accion)
    {
        using var scope = _factory.Services.CreateScope();
        return await accion(scope.ServiceProvider.GetRequiredService<ApiContext>());
    }

    private Task<int> CrearUsuario(string codigo, string contrasena, bool estado = true)
    {
        return ConDb(async db => await Sembrar(db, codigo, contrasena, estado));
    }

    private static async Task<int> Sembrar(ApiContext db, string codigo, string contrasena, bool estado = true)
    {
        // Sede y tipo con id explícito (Listar excluye el tipo 1); persona y usuario también los crea la API
        int referencia = Interlocked.Increment(ref _secuencia);
        var persona = new Persona { NombreCompleto = "ANA PEREZ", Dni = "12345678", FechaNacimiento = new DateOnly(1990, 5, 2) };
        var usuario = new Usuario
        {
            Contrasena = contrasena,
            Codigo = codigo,
            Estado = estado,
            SedeId = referencia,
            TipoId = referencia,
            Persona = persona,
        };
        db.Sedes.Add(new Sede { Id = referencia, Nombre = "Sede" });
        db.Set<TipoUsuario>().Add(new TipoUsuario { Id = referencia, Nombre = "Tipo" });
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario.Id;
    }

    private Task<string> ContrasenaGuardada(int id)
    {
        return ConDb(async db => (await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == id)).Contrasena);
    }

    private Task<HttpResponseMessage> Login(string codigo, string contrasena)
    {
        return _client.PostAsJsonAsync("/api/usuario/login", new { codigo, contrasena });
    }

    private async Task<object> CuerpoActualizacion(int id, string? contrasena)
    {
        var actual = await ConDb(db => db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == id));
        return new
        {
            contrasena,
            tipoId = actual.TipoId,
            sedeId = actual.SedeId,
            horarioEntrada = "08:00:00",
            horarioSalida = "17:00:00",
            persona = new { nombreCompleto = "ANA PEREZ", fechaNacimiento = "1990-05-02", dni = "12345678", telefono = "999", modificadorId = id },
            cqfp = "123",
            modificadorId = id,
        };
    }

    [Fact]
    public async Task SinApiKey_Rechaza()
    {
        var response = await _factory.CreateClient().GetAsync("/api/usuario");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Listar_NoDevuelveContrasenas()
    {
        await CrearUsuario("LST1", "plano-listar");

        var response = await _client.GetAsync("/api/usuario");
        string json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("LST1", json);
        Assert.DoesNotContain("contrasena", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("plano-listar", json);
    }

    [Fact]
    public async Task ObtenerPorId_NoDevuelveContrasena()
    {
        int id = await CrearUsuario("BYID1", "plano-byid");

        var response = await _client.GetAsync($"/api/usuario/{id}");
        string json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("BYID1", json);
        Assert.DoesNotContain("contrasena", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("plano-byid", json);
    }

    [Fact]
    public async Task Login_LegadoFunciona_YMigraAHash()
    {
        int id = await CrearUsuario("LEG1", "secreta1");

        var primero = await Login("LEG1", "secreta1");
        string guardada = await ContrasenaGuardada(id);
        var segundo = await Login("LEG1", "secreta1");

        Assert.Equal(HttpStatusCode.OK, primero.StatusCode);
        Assert.True(PasswordHasher.EsHash(guardada));
        Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
        Assert.DoesNotContain("contrasena", await primero.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ContrasenaIncorrecta_NoMigra()
    {
        int id = await CrearUsuario("LEG2", "secreta1");

        var response = await Login("LEG2", "mala");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("secreta1", await ContrasenaGuardada(id));
    }

    [Fact]
    public async Task Login_ConHash_NoAceptaElHashComoContrasena()
    {
        string hash = PasswordHasher.Hashear("secreta1");
        await CrearUsuario("HASH1", hash);

        Assert.Equal(HttpStatusCode.OK, (await Login("HASH1", "secreta1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Login("HASH1", hash)).StatusCode);
    }

    [Fact]
    public async Task Login_CodigoDuplicado_EligePorContrasena()
    {
        int primero = await CrearUsuario("DUP1", "clave-uno");
        int segundo = await CrearUsuario("DUP1", PasswordHasher.Hashear("clave-dos"));

        var response = await Login("DUP1", "clave-dos");
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(segundo.ToString(), body!["id"].ToString());
        Assert.Equal("clave-uno", await ContrasenaGuardada(primero));
    }

    [Fact]
    public async Task Login_UsuarioInactivo_Rechaza()
    {
        await CrearUsuario("INA1", "secreta1", estado: false);

        Assert.Equal(HttpStatusCode.NotFound, (await Login("INA1", "secreta1")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Actualizar_ContrasenaVacia_NoLaCambia(string? contrasena)
    {
        string hash = PasswordHasher.Hashear("secreta1");
        int id = await CrearUsuario("EDIT" + (contrasena?.Length ?? 9), hash);

        var response = await _client.PutAsJsonAsync($"/api/usuario/{id}", await CuerpoActualizacion(id, contrasena));
        string json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(hash, await ContrasenaGuardada(id));
        Assert.DoesNotContain("contrasena", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Actualizar_ContrasenaNueva_SeGuardaComoHash()
    {
        int id = await CrearUsuario("EDITN", "antigua1");

        var response = await _client.PutAsJsonAsync($"/api/usuario/{id}", await CuerpoActualizacion(id, " nueva123 "));
        string guardada = await ContrasenaGuardada(id);
        string json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(PasswordHasher.EsHash(guardada));
        Assert.True(PasswordHasher.Verificar("nueva123", guardada));
        Assert.False(PasswordHasher.Verificar("antigua1", guardada));
        Assert.DoesNotContain("contrasena", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Crear_GuardaHash_YNoLoDevuelve()
    {
        int referencia = await CrearUsuario("REF1", "secreta1");
        var existente = await ConDb(db => db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == referencia));

        var response = await _client.PostAsJsonAsync("/api/usuario", new
        {
            contrasena = "nueva123",
            tipoId = existente.TipoId,
            sedeId = existente.SedeId,
            persona = new { nombreCompleto = "LUIS DIAZ", fechaNacimiento = "1991-03-04", dni = "87654321", telefono = "999", creadorId = referencia },
            cqfp = "123",
            creadorId = referencia,
        });
        string json = await response.Content.ReadAsStringAsync();
        string guardada = await ConDb(async db => (await db.Usuarios.AsNoTracking().FirstAsync(u => u.Codigo == "LD0403")).Contrasena);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(PasswordHasher.Verificar("nueva123", guardada));
        Assert.True(PasswordHasher.EsHash(guardada));
        Assert.DoesNotContain("contrasena", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Crear_SinContrasena_DevuelveBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/usuario", new { sedeId = 1, contrasena = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CambiarEstado_NoDevuelveContrasena()
    {
        int id = await CrearUsuario("EST1", "plano-estado");

        var response = await _client.PutAsJsonAsync($"/api/usuario/estado/{id}", false);
        string json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("plano-estado", json);
        Assert.DoesNotContain("contrasena", json, StringComparison.OrdinalIgnoreCase);
    }

    private class FalloAlGuardar : SaveChangesInterceptor
    {
        public bool Activo { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Activo)
            {
                throw new DbUpdateException("value too long for type character varying(20)");
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    [Fact]
    public async Task Login_SiFallaGuardarElHash_NoBloqueaAlUsuario()
    {
        var interceptor = new FalloAlGuardar();
        var options = new DbContextOptionsBuilder<ApiContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        var mapper = new MapperConfiguration(c => c.AddProfile<UsuarioMappingProfile>()).CreateMapper();
        await using var db = new ApiContext(options);
        int id = await Sembrar(db, "FALLO1", "secreta1");
        interceptor.Activo = true;
        var service = new UserService(db, mapper, NullLogger<UserService>.Instance);

        var usuario = await service.ValidarLogin("FALLO1", "secreta1");

        Assert.NotNull(usuario);
        Assert.Equal(id, usuario!.Id);
        Assert.Equal("secreta1", (await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == id)).Contrasena);
    }
}

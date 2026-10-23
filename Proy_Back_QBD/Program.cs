using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Proy_back_QBD.Data;
using Proy_back_QBD.Profiles;
using Proy_back_QBD.Services;
using Proy_back_QBD.Services.Interfaces;
using System.Text.Json.Serialization;
using proy_back_Qbd.Services;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Models;
using Proy_back_QBD.Service.AjusteService;
using Proy_back_QBD.Interface;
using proy_back_Qbd.Services.Interfaces.INotaSalidaService;
using Proy_back_QBD.Services.NotaSalidaService;
using Proy_back_QBD.DependencyInjection;
using Proy_back_QBD.Middleware;

// Program.cs solo arma la aplicación. Cada parte de la configuración vive en su propia clase:
//   DependencyInjection/ServiceCollectionExtensions.cs → base de datos y servicios del negocio
//   DependencyInjection/WebApiExtensions.cs            → JWT, Swagger, CORS e integraciones (S3, OpenAI)
//   Middleware/ExceptionHandlingMiddleware.cs          → manejo de errores
//   Data/DatabaseStartupMigrator.cs                    → ajustes de base de datos al arrancar

Env.Load(); // Cargar variables de entorno desde el archivo .env
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
var builder = WebApplication.CreateBuilder(args);

// Configurar puerto dinámico para Render u otros entornos de hosting
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<PersonaMappingProfile>();  // Registra tu perfil explícitamente
    cfg.AddProfile<UsuarioMappingProfile>();
    cfg.AddProfile<SedeMappingProfile>();  // Registra tu perfil explícitamente
    cfg.AddProfile<AsistenciaMappingProfile>();  // Registra tu perfil explícitamente
    cfg.AddProfile<PacienteMappingProfile>();  // Registra tu perfil explícitamente
    cfg.AddProfile<MedicoMappingProfile>();  // Registra tu perfil explícitamente
    cfg.AddProfile<PedidoMap>();  // Registra tu perfil explícitamente
    cfg.AddProfile<FormulaMap>();  // Registra tu perfil explícitamente
    cfg.AddProfile<ProdTermsMap>();  // Registra tu perfil explícitamente
    cfg.AddProfile<CobroMap>();
    cfg.AddProfile<LaboratorioMap>();
    cfg.AddProfile<FormulaRMap>();
    cfg.AddProfile<FormulaCCMap>();
    cfg.AddProfile<InsumoRMap>();
    cfg.AddProfile<InsumoMap>();
    cfg.AddProfile<EmpaqueMap>();
    cfg.AddProfile<ProductoMap>();
    cfg.AddProfile<OrdenCompraMap>();
    cfg.AddProfile<DetalleOrdenCompraMap>();
    cfg.AddProfile<CotizacionMap>();
});

builder.Configuration
    .AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ===================== Servicios =====================
builder.Services.AddApplicationServices();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerDocumentation();
builder.Services.AddPersistence(builder.Configuration, builder.Environment);
builder.Services.AddCorsPolicy();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddIntegraciones(builder.Configuration);

// ===================== Pipeline =====================
var app = builder.Build();

app.UseManejoDeErrores();
app.UseSwaggerDocumentation();
app.UseHttpsRedirection();
app.UseCors(WebApiExtensions.CorsPolicy);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("API started. Swagger is available at /swagger.");

app.AplicarCambiosDeEsquemaAlIniciar();

app.Run();

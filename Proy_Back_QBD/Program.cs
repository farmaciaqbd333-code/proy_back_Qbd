using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Proy_back_QBD.Data;
using Proy_back_QBD.Profiles;
using Proy_back_QBD.Services;
using System.Reflection;
using Proy_back_QBD.Services.Interfaces;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using proy_back_Qbd.Services;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Models;
using Microsoft.AspNetCore.Diagnostics;
using proy_back_Qbd.Exceptions;
using Proy_back_QBD.Service.AjusteService;
using Proy_back_QBD.Interface;
using proy_back_Qbd.Services.Interfaces.INotaSalidaService;
using Proy_back_QBD.Services.NotaSalidaService;
using Proy_back_QBD.DependencyInjection;
Env.Load(); // Cargar variables de entorno desde el archivo .env
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
var builder = WebApplication.CreateBuilder(args);

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
});

builder.Configuration
    .AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddApplicationServices();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Ingrese su API Key en el campo",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new List<string>()
        }
    });
});

// Configurar conexión a PostgreSQL
var configuration = builder.Configuration;

var baseConnectionString = configuration.GetConnectionString("DefaultConnection") ??
    $"Host={configuration["POSTGRES_HOST"]};" +
    $"Port={configuration["POSTGRES_PORT"]};" +
    $"Username={configuration["POSTGRES_USERNAME"]};" +
    $"Password={configuration["POSTGRES_PASSWORD"]};" +
    $"Database={configuration["POSTGRES_DB"]}";

var connectionString = baseConnectionString;
if (!connectionString.Contains("MaxPoolSize", StringComparison.OrdinalIgnoreCase))
{
    connectionString = connectionString.TrimEnd(';') + ";Pooling=true;MaxPoolSize=20;";
}

Console.WriteLine($"Connection String: {connectionString}");

builder.Services.AddDbContext<ApiContext>(options =>
{
    options.UseNpgsql(connectionString);

    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();

});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        builder.AllowAnyOrigin() // Especifica orígenes permitidos en producción
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        var error = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        context.Response.ContentType = "application/json";

        switch (error)
        {
            case BadRequestException:
                context.Response.StatusCode = 400;
                break;

            case NotFoundException:
                context.Response.StatusCode = 404;
                break;
            case ServerException:
                context.Response.StatusCode = 500;
                break;

            default:
                context.Response.StatusCode = 500;
                break;
        }

        await context.Response.WriteAsJsonAsync(new
        {
            message = error?.Message
        });
    });
});
// if (app.Environment.IsDevelopment())
// {
//     app.UseDeveloperExceptionPage();
// }
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
});
app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");
app.UseRouting();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("\nSwagger disponible en:".PadRight(30, ' ') + " http://localhost:5051/swagger" + "\n" + "API KEY:".PadRight(30, ' ') + "4554654654754");
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApiContext>();
//     var users = await db.CompraInsumos.FindAsync(53);
//     Console.WriteLine(users.FechaVencimiento);
// }
app.Run();
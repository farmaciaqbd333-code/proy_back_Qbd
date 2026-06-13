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
Env.Load(); // Cargar variables de entorno desde el archivo .env
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
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
    cfg.AddProfile<DetalleCompraLabMap>();
});

builder.Configuration
    .AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ISedeService, SedeService>();
builder.Services.AddScoped<IAsistenciaService, AsistenciaService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IMedicoService, MedicoService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IFormulaService, FormulaService>();
builder.Services.AddScoped<IFormulaRService, FormulaRService>();
builder.Services.AddScoped<IFormulaCCService, FormulaCCService>();
builder.Services.AddScoped<IInsumoRService, InsumoRService>();
builder.Services.AddScoped<IInsumoService, InsumoService>();
builder.Services.AddScoped<IProdTermService, ProdTermService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ILaboratorioService, LaboratorioService>();
builder.Services.AddScoped<IEspecialidadService, EspecialidadService>();
builder.Services.AddScoped<ICobroService, CobroService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<IEmpaqueService, EmpaqueService>();
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<IEconomatoService, EconomatoService>();
builder.Services.AddScoped<IDetalleOrdenCompraService, DetalleOrdenCompraService>();
builder.Services.AddScoped<ICompraLaboratorioService, CompraLaboratorioService>();
builder.Services.AddScoped<IPaqueteService, PaqueteService>();
builder.Services.AddScoped<IMesonService, MesonService>();
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

var connectionString = configuration.GetConnectionString("DefaultConnection") ??
    $"Host={configuration["POSTGRES_HOST"]};" +
    $"Port={configuration["POSTGRES_PORT"]};" +
    $"Username={configuration["POSTGRES_USERNAME"]};" +
    $"Password={configuration["POSTGRES_PASSWORD"]};" +
    $"Database={configuration["POSTGRES_DB"]}";

Console.WriteLine($"Connection String: {connectionString}");

builder.Services.AddDbContext<ApiContext>(options =>
    options.UseNpgsql(connectionString)
           .EnableSensitiveDataLogging()
           .EnableDetailedErrors()
    );
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

app.Run();
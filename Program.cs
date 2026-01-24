using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Proy_back_QBD.Data;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Proy_back_QBD.Profiles;
using Proy_back_QBD.Services;
using System.Reflection;
using Proy_back_QBD.Util;
using Proy_back_QBD.Services.Interfaces;
using Proy_back_QBD.Models;
using Microsoft.OpenApi.Models;
Env.Load(); // Cargar variables de entorno desde el archivo .env

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
});
builder.Configuration
    .AddEnvironmentVariables();
builder.Logging.ClearProviders(); // Limpia los proveedores de logging por defecto
builder.Logging.AddConsole();
builder.Services.AddScoped<IUserService, UserService>();
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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Ingrese su API Key en el campo",
        Name = "X-Api-Key", // Nombre del encabezado que el middleware espera
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
app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(options =>
{

    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
});
app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");
app.UseRouting();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("\nSwagger disponible en:".PadRight(30,' ') + " http://localhost:5051/swagger" + "\n" + "API KEY:".PadRight(30, ' ') + "4554654654754");
app.Run();
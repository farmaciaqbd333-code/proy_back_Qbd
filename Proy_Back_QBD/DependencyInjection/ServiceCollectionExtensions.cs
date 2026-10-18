using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;
using Proy_back_QBD.Services.Interfaces;
using proy_back_Qbd.Services;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Service.AjusteService;
using Proy_back_QBD.Interface;
using proy_back_Qbd.Services.Interfaces.INotaSalidaService;
using Proy_back_QBD.Services.NotaSalidaService;
using proy_back_Qbd.Repositories.Interfaces;
using proy_back_Qbd.Repositories;

namespace Proy_back_QBD.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var connectionString = GetPostgresConnectionString(configuration);

            services.AddDbContext<ApiContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.EnableDetailedErrors();
                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            return services;
        }

        private static string GetPostgresConnectionString(IConfiguration configuration)
        {
            // 1. Variable única para la URL externa (DATABASE_URL o POSTGRES_URL)
            var rawUrl = configuration["DATABASE_URL"] 
                ?? configuration["POSTGRES_URL"] 
                ?? configuration.GetConnectionString("DefaultConnection");

            NpgsqlConnectionStringBuilder builder;

            if (!string.IsNullOrWhiteSpace(rawUrl))
            {
                rawUrl = rawUrl.Trim().Trim('"', '\'');

                if (rawUrl.StartsWith("jdbc:", StringComparison.OrdinalIgnoreCase))
                {
                    rawUrl = rawUrl.Substring(5);
                }

                // Formato URI (postgresql://user:password@host:port/database)
                if (rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                    rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(rawUrl);
                    var userInfo = uri.UserInfo.Split(':', 2);
                    var username = Uri.UnescapeDataString(userInfo[0]);
                    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
                    var database = uri.LocalPath.Trim('/');

                    builder = new NpgsqlConnectionStringBuilder
                    {
                        Host = uri.Host,
                        Port = uri.Port > 0 ? uri.Port : 5432,
                        Username = username,
                        Password = password,
                        Database = string.IsNullOrEmpty(database) ? "postgres" : database
                    };

                    if (!string.IsNullOrEmpty(uri.Query))
                    {
                        var query = uri.Query.TrimStart('?');
                        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var kv = pair.Split('=', 2);
                            var key = Uri.UnescapeDataString(kv[0]).ToLowerInvariant();
                            var val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
                            if (key == "sslmode" && Enum.TryParse<SslMode>(val, true, out var parsedMode))
                            {
                                builder.SslMode = parsedMode;
                            }
                        }
                    }
                }
                else
                {
                    // Formato ADO.NET (Host=...;Database=...;...)
                    builder = new NpgsqlConnectionStringBuilder(rawUrl);
                }
            }
            else
            {
                // 2. Fallback: Variables individuales si no se definió DATABASE_URL
                builder = new NpgsqlConnectionStringBuilder
                {
                    Host = configuration["POSTGRES_HOST"] ?? "localhost",
                    Port = int.TryParse(configuration["POSTGRES_PORT"], out var p) ? p : 5432,
                    Username = configuration["POSTGRES_USERNAME"] ?? "postgres",
                    Password = configuration["POSTGRES_PASSWORD"] ?? "",
                    Database = configuration["POSTGRES_DB"] ?? "postgres"
                };
            }

            // Parámetros de estabilidad y rendimiento
            builder.Pooling = true;
            if (builder.MaxPoolSize <= 0 || builder.MaxPoolSize == 100)
            {
                builder.MaxPoolSize = 20;
            }
            builder.KeepAlive = 30;
            builder.Timeout = 30;

            // Para servidores cloud remotos (como Render), SSL es obligatorio y requiere confiar en el certificado
            if (builder.Host != "localhost" && builder.Host != "127.0.0.1")
            {
                if (builder.SslMode == SslMode.Disable || builder.SslMode == SslMode.Prefer)
                {
                    builder.SslMode = SslMode.Require;
                }
                builder.TrustServerCertificate = true;
            }

            return builder.ConnectionString;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IKardexService, KardexService>();
            services.AddScoped<ISedeService, SedeService>();
            services.AddScoped<IAsistenciaService, AsistenciaService>();
            services.AddScoped<IPacienteService, PacienteService>();
            services.AddScoped<IMedicoService, MedicoService>();
            services.AddScoped<IPedidoService, PedidoService>();
            services.AddScoped<IFormulaService, FormulaService>();
            services.AddScoped<IFormulaRService, FormulaRService>();
            services.AddScoped<IFormulaCCService, FormulaCCService>();
            services.AddScoped<IInsumoRService, InsumoRService>();
            services.AddScoped<IInsumoService, InsumoService>();
            services.AddScoped<IProdTermService, ProdTermService>();
            services.AddScoped<IProductoService, ProductoService>();
            services.AddScoped<ILaboratorioService, LaboratorioService>();
            services.AddScoped<IEspecialidadService, EspecialidadService>();
            services.AddScoped<ICobroService, CobroService>();
            services.AddScoped<ICajaService, CajaService>();
            services.AddScoped<IEmpaqueService, EmpaqueService>();
            services.AddScoped<IOrdenCompraService, OrdenCompraService>();
            services.AddScoped<IEconomatoService, EconomatoService>();
            services.AddScoped<ICompraLaboratorioService, CompraLaboratorioService>();
            services.AddScoped<IPaqueteService, PaqueteService>();
            services.AddScoped<IMesonService, MesonService>();
            services.AddScoped<IAjusteService, AjusteService>();
            services.AddScoped<IProductoIntermedioService, ProductoIntermedioService>();
            services.AddScoped<INotaSalidaService, NotaSalidaService>();
            services.AddScoped<IRecepcionRepository, RecepcionRepository>();
            services.AddScoped<IRecepcionService, RecepcionService>();
            services.AddScoped<ILaboratorioRepository, LaboratorioRepository>();
            services.AddScoped<IUnitOfWork, UnitWork>();
            services.AddScoped<ISupplyRepository, SupplyRepository>();
            services.AddScoped<ICotizacionService, CotizacionService>();

            return services;
        }
    }
}

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
            // 1 variable para la URL externa o connection string de PostgreSQL
            var rawUrl = configuration["DATABASE_URL"] 
                ?? configuration["POSTGRES_URL"] 
                ?? configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrWhiteSpace(rawUrl))
            {
                rawUrl = rawUrl.Trim().Trim('"', '\'');

                // Si viene en formato URL estándar (postgresql://user:password@host:port/database)
                if (rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                    rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(rawUrl);
                    var userInfo = uri.UserInfo.Split(':', 2);
                    var username = Uri.UnescapeDataString(userInfo[0]);
                    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
                    var database = uri.LocalPath.Trim('/');

                    var builder = new NpgsqlConnectionStringBuilder
                    {
                        Host = uri.Host,
                        Port = uri.Port > 0 ? uri.Port : 5432,
                        Username = username,
                        Password = password,
                        Database = string.IsNullOrEmpty(database) ? "postgres" : database,
                        Pooling = true,
                        MaxPoolSize = 20
                    };

                    // En servidores cloud externos (Render, Supabase, Neon, etc.), habilitar SSL y confiar en el certificado
                    if (builder.Host != "localhost" && builder.Host != "127.0.0.1")
                    {
                        builder.SslMode = SslMode.Require;
                        builder.TrustServerCertificate = true;
                    }

                    return builder.ConnectionString;
                }

                // Si ya está en formato ADO.NET (Host=...;Database=...;...)
                var directBuilder = new NpgsqlConnectionStringBuilder(rawUrl);
                if (!rawUrl.Contains("MaxPoolSize", StringComparison.OrdinalIgnoreCase))
                {
                    directBuilder.MaxPoolSize = 20;
                }
                if (!rawUrl.Contains("Pooling", StringComparison.OrdinalIgnoreCase))
                {
                    directBuilder.Pooling = true;
                }
                if (directBuilder.Host != "localhost" && directBuilder.Host != "127.0.0.1")
                {
                    if (!rawUrl.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
                        !rawUrl.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
                    {
                        directBuilder.SslMode = SslMode.Require;
                    }
                    if (!rawUrl.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase) &&
                        !rawUrl.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
                    {
                        directBuilder.TrustServerCertificate = true;
                    }
                }
                return directBuilder.ConnectionString;
            }

            // Fallback: Variables individuales si no se definió DATABASE_URL
            var fallback = $"Host={configuration["POSTGRES_HOST"]};" +
                $"Port={configuration["POSTGRES_PORT"] ?? "5432"};" +
                $"Username={configuration["POSTGRES_USERNAME"]};" +
                $"Password={configuration["POSTGRES_PASSWORD"]};" +
                $"Database={configuration["POSTGRES_DB"]}";

            if (!fallback.Contains("MaxPoolSize", StringComparison.OrdinalIgnoreCase))
            {
                fallback = fallback.TrimEnd(';') + ";Pooling=true;MaxPoolSize=20;";
            }

            return fallback;
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

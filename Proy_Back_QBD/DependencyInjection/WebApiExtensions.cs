using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Proy_back_QBD.Integraciones;
using Proy_back_QBD.Security;
using System.Reflection;

namespace Proy_back_QBD.DependencyInjection
{
    /// <summary>
    /// Configuración de la API separada por responsabilidad (antes todo estaba en Program.cs):
    /// seguridad (JWT), documentación (Swagger), CORS e integraciones externas.
    /// </summary>
    public static class WebApiExtensions
    {
        public const string CorsPolicy = "AllowSpecificOrigins";

        // ===================== Seguridad =====================

        /// <summary>
        /// Autenticación JWT + política por defecto: toda ruta exige sesión salvo [AllowAnonymous].
        /// La clave viene de JWT_SECRET (mínimo 32 caracteres).
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var secret = configuration["JWT_SECRET"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            {
                secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
                configuration["JWT_SECRET"] = secret;
                Console.WriteLine("ADVERTENCIA: JWT_SECRET no está configurado (o tiene menos de 32 caracteres). " +
                                  "Se usa una clave temporal: las sesiones se cerrarán cada vez que se reinicie el servidor.");
            }

            services.Configure<JwtOptions>(o => o.Secret = secret);
            services.AddSingleton<ITokenService, JwtTokenService>();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = JwtTokens.Issuer,
                        ValidateAudience = true,
                        ValidAudience = JwtTokens.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = JwtTokens.CrearClave(secret),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = "sub",
                        RoleClaimType = JwtTokens.ClaimRol,
                    };
                });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            return services;
        }

        // ===================== Documentación =====================

        /// <summary>Swagger con comentarios XML y botón para pegar el token JWT.</summary>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Token JWT obtenido en POST /api/usuario/login (pegue solo el token)",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new List<string>()
                    }
                });
            });
            return services;
        }

        public static WebApplication UseSwaggerDocumentation(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
            });
            return app;
        }

        // ===================== CORS =====================

        /// <summary>
        /// Política CORS. Hoy acepta cualquier origen (sin cambios respecto a antes);
        /// restringirla a los dominios propios es el hallazgo A2 de la auditoría.
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            return services;
        }

        // ===================== Integraciones externas =====================

        /// <summary>
        /// Servicios externos detrás de interfaces (S3 y OpenAI). Las credenciales vienen de
        /// variables de entorno: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION, AWS_S3_BUCKET, OPENAI_API_KEY.
        /// </summary>
        public static IServiceCollection AddIntegraciones(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AwsS3Options>(o =>
            {
                o.AccessKeyId = configuration["AWS_ACCESS_KEY_ID"];
                o.SecretAccessKey = configuration["AWS_SECRET_ACCESS_KEY"];
                o.Region = configuration["AWS_REGION"] ?? "us-east-1";
                o.Bucket = configuration["AWS_S3_BUCKET"];
            });
            services.AddSingleton<IAlmacenamientoArchivos, S3AlmacenamientoArchivos>();

            services.Configure<OpenAIOptions>(o =>
            {
                o.ApiKey = configuration["OPENAI_API_KEY"];
            });
            services.AddHttpClient<IAsistenteIA, OpenAIAsistente>(c => c.Timeout = TimeSpan.FromSeconds(90));

            services.AddSingleton<ILimiteConsultas>(_ => new LimiteConsultasPorMinuto(20));

            return services;
        }
    }
}

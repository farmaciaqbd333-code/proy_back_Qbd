using proy_back_Qbd.Exceptions;

namespace Proy_back_QBD.Middleware
{
    /// <summary>
    /// Manejo de errores en un solo lugar: traduce las excepciones a una respuesta uniforme
    /// { "message": "..." } con su código HTTP.
    /// - BadRequestException → 400 con su mensaje.
    /// - NotFoundException   → 404 con su mensaje.
    /// - Cualquier otra      → 500 con un mensaje genérico (el detalle queda en el log, no se expone).
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogError(ex, "Error después de iniciar la respuesta en {Path}", context.Request.Path);
                    throw;
                }

                var (status, mensaje) = ex switch
                {
                    BadRequestException => (StatusCodes.Status400BadRequest, ex.Message),
                    NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                    Proy_back_QBD.Services.AppMedico.NoAutorizadoException => (StatusCodes.Status401Unauthorized, ex.Message),
                    _ => ErrorDeBaseDeDatos(ex) ?? (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intente nuevamente."),
                };

                if (status == StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(ex, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
                }

                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = mensaje });
            }
        }

        /// <summary>
        /// Errores de datos que rechaza PostgreSQL al guardar: se explican en vez del 500 genérico,
        /// para que quien registra sepa qué corregir (registro repetido, campo obligatorio, texto largo...).
        /// </summary>
        internal static (int Status, string Mensaje)? ErrorDeBaseDeDatos(Exception ex)
        {
            Npgsql.PostgresException? pg = null;
            for (Exception? e = ex; e != null && pg == null; e = e.InnerException)
            {
                pg = e as Npgsql.PostgresException;
            }
            if (pg == null)
            {
                return null;
            }

            string donde = pg.TableName != null ? $" (tabla {pg.TableName}{(pg.ColumnName != null ? ", campo " + pg.ColumnName : "")})" : "";
            return pg.SqlState switch
            {
                "23505" when pg.ConstraintName?.EndsWith("_pkey") == true =>
                    (StatusCodes.Status409Conflict, $"No se pudo guardar: el número interno (id) que asignó la base ya está usado{donde}. Hay que resincronizar la secuencia de ids de esa tabla."),
                "23505" => (StatusCodes.Status409Conflict, $"Ya existe un registro con esos mismos datos{donde}: {pg.Detail ?? pg.MessageText}"),
                "23502" => (StatusCodes.Status400BadRequest, $"Falta un dato obligatorio{donde}."),
                "23503" => (StatusCodes.Status400BadRequest, $"Un dato seleccionado no existe o ya no es válido{donde}: {pg.Detail ?? pg.MessageText}"),
                "22001" => (StatusCodes.Status400BadRequest, $"Un texto es más largo de lo permitido{donde}: {pg.MessageText}"),
                "23514" => (StatusCodes.Status400BadRequest, $"Un valor no cumple las reglas de la base{donde}: {pg.ConstraintName}"),
                _ => null,
            };
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseManejoDeErrores(this IApplicationBuilder app) =>
            app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

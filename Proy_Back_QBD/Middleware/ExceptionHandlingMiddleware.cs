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
                    _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intente nuevamente."),
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
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseManejoDeErrores(this IApplicationBuilder app) =>
            app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

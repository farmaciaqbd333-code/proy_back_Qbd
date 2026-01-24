public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string API_KEY_HEADER_NAME = "X-Api-Key"; // El nombre del encabezado en el que esperamos la clave
    private readonly string VALID_API_KEY;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        VALID_API_KEY = configuration["API-KEY"];
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Verificar si el encabezado 'X-Api-Key' está presente
        if (!httpContext.Request.Headers.ContainsKey(API_KEY_HEADER_NAME))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest; // Si no está presente, retorno error 400
            await httpContext.Response.WriteAsync("API Key es requerida.");
            return;
        }

        var apiKey = httpContext.Request.Headers[API_KEY_HEADER_NAME].ToString();

        // Verificar si la API Key es válida
        if (apiKey != VALID_API_KEY)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized; // Si la clave es incorrecta, retorno error 401
            await httpContext.Response.WriteAsync("API Key inválida.");
            return;
        }

        // Si la clave es válida, proceder con la solicitud
        await _next(httpContext);
    }
}

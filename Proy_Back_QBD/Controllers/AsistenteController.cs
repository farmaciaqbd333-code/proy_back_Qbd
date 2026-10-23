using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Integraciones;
using Proy_back_QBD.Security;

namespace Proy_back_QBD.Controllers;

/// <summary>Un mensaje del chat del asistente.</summary>
public class AsistenteMensaje
{
    /// <summary>"system", "user" o "assistant".</summary>
    public string? Role { get; set; }

    /// <summary>Texto del mensaje.</summary>
    public string? Content { get; set; }
}

/// <summary>Petición al asistente farmacéutico.</summary>
public class AsistenteChatReq
{
    /// <summary>Historial de la conversación.</summary>
    public List<AsistenteMensaje>? Messages { get; set; }

    /// <summary>Máximo de tokens de la respuesta (se limita a 2000).</summary>
    public int? Max_tokens { get; set; }

    /// <summary>Temperatura (0 a 1).</summary>
    public double? Temperature { get; set; }
}

/// <summary>
/// Asistente de IA. El controlador valida y limita; la llamada al proveedor la hace <see cref="IAsistenteIA"/>.
/// </summary>
[ApiController]
[Route("api/asistente")]
public class AsistenteController : ControllerBase
{
    private const int MaxMensajes = 40;
    private const int MaxCaracteresPorMensaje = 20000;
    private const int MaxTokens = 2000;
    private static readonly HashSet<string> RolesValidos = new() { "system", "user", "assistant" };

    private readonly IAsistenteIA _asistente;
    private readonly ILimiteConsultas _limite;
    private readonly ILogger<AsistenteController> _logger;

    public AsistenteController(IAsistenteIA asistente, ILimiteConsultas limite, ILogger<AsistenteController> logger)
    {
        _asistente = asistente;
        _limite = limite;
        _logger = logger;
    }

    /// <summary>
    /// Envía la conversación al asistente y devuelve la respuesta en formato OpenAI
    /// (choices[0].message.content).
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AsistenteChatReq request, CancellationToken ct)
    {
        if (!_asistente.EstaConfigurado)
        {
            _logger.LogError("Asistente IA no configurado: falta la variable OPENAI_API_KEY.");
            return StatusCode(503, Error("El asistente no está configurado en el servidor."));
        }

        // Límite por usuario (token) o, si no hay, por IP
        var clave = JwtTokens.UsuarioId(User) is var id && id > 0
            ? $"u:{id}"
            : $"ip:{HttpContext.Connection.RemoteIpAddress}";
        if (!_limite.Permitir(clave))
        {
            return StatusCode(429, Error("Demasiadas consultas seguidas. Espere un minuto e intente nuevamente."));
        }

        var mensajes = (request?.Messages ?? new List<AsistenteMensaje>())
            .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Content) && RolesValidos.Contains((m.Role ?? "").ToLowerInvariant()))
            .TakeLast(MaxMensajes)
            .Select(m => new MensajeChat(
                (m.Role ?? "user").ToLowerInvariant(),
                m.Content!.Length > MaxCaracteresPorMensaje ? m.Content.Substring(0, MaxCaracteresPorMensaje) : m.Content))
            .ToList();

        if (mensajes.Count == 0)
        {
            return BadRequest(Error("Debe enviar al menos un mensaje."));
        }

        try
        {
            var respuesta = await _asistente.ChatAsync(
                mensajes,
                Math.Clamp(request?.Max_tokens ?? MaxTokens, 1, MaxTokens),
                Math.Clamp(request?.Temperature ?? 0.7, 0, 1),
                ct);

            if (!respuesta.Exito || respuesta.Json == null)
            {
                return StatusCode(502, Error("El asistente no está disponible en este momento. Intente nuevamente."));
            }
            return Content(respuesta.Json, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando al asistente IA");
            return StatusCode(502, Error("No se pudo conectar con el asistente. Intente nuevamente."));
        }
    }

    /// <summary>Formato de error que el frontend ya lee (error.error.message).</summary>
    private static object Error(string mensaje) => new { error = new { message = mensaje } };
}

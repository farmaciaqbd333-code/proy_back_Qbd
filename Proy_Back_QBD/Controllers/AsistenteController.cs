using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

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
/// Asistente de IA (OpenAI) llamado desde el servidor. La clave vive solo en la
/// variable de entorno OPENAI_API_KEY del backend (nunca en el frontend).
/// </summary>
[ApiController]
[Route("api/asistente")]
public class AsistenteController : ControllerBase
{
    private const string Modelo = "gpt-4o-mini";
    private const int MaxMensajes = 40;
    private const int MaxCaracteresPorMensaje = 20000;
    private const int MaxTokens = 2000;
    private const int LimitePorMinuto = 20;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };
    private static readonly ConcurrentDictionary<string, (DateTime Inicio, int Cuenta)> Uso = new();
    private static readonly HashSet<string> RolesValidos = new() { "system", "user", "assistant" };

    private readonly IConfiguration _configuration;
    private readonly ILogger<AsistenteController> _logger;

    public AsistenteController(IConfiguration configuration, ILogger<AsistenteController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Envía la conversación al asistente y devuelve la respuesta en el mismo formato de OpenAI
    /// (choices[0].message.content).
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AsistenteChatReq request)
    {
        var apiKey = _configuration["OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Asistente IA no configurado: falta la variable OPENAI_API_KEY.");
            return StatusCode(503, new { error = new { message = "El asistente no está configurado en el servidor." } });
        }

        if (!PermitirSolicitud())
        {
            return StatusCode(429, new { error = new { message = "Demasiadas consultas seguidas. Espere un minuto e intente nuevamente." } });
        }

        var mensajes = (request?.Messages ?? new List<AsistenteMensaje>())
            .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Content) && RolesValidos.Contains((m.Role ?? "").ToLowerInvariant()))
            .TakeLast(MaxMensajes)
            .Select(m => new JsonObject
            {
                ["role"] = (m.Role ?? "user").ToLowerInvariant(),
                ["content"] = m.Content!.Length > MaxCaracteresPorMensaje ? m.Content.Substring(0, MaxCaracteresPorMensaje) : m.Content,
            })
            .ToList();

        if (mensajes.Count == 0)
        {
            return BadRequest(new { error = new { message = "Debe enviar al menos un mensaje." } });
        }

        var cuerpo = new JsonObject
        {
            ["model"] = Modelo,
            ["messages"] = new JsonArray(mensajes.Cast<JsonNode>().ToArray()),
            ["max_tokens"] = Math.Clamp(request?.Max_tokens ?? MaxTokens, 1, MaxTokens),
            ["temperature"] = Math.Clamp(request?.Temperature ?? 0.7, 0, 1),
        };

        try
        {
            using var mensajeHttp = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            mensajeHttp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            mensajeHttp.Content = new StringContent(cuerpo.ToJsonString(), Encoding.UTF8, "application/json");

            using var respuesta = await Http.SendAsync(mensajeHttp);
            var json = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI respondió {Status}: {Cuerpo}", (int)respuesta.StatusCode, json);
                return StatusCode(502, new { error = new { message = "El asistente no está disponible en este momento. Intente nuevamente." } });
            }

            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando a OpenAI");
            return StatusCode(502, new { error = new { message = "No se pudo conectar con el asistente. Intente nuevamente." } });
        }
    }

    /// <summary>Límite simple: N consultas por minuto por IP.</summary>
    private bool PermitirSolicitud()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
        var ahora = DateTime.UtcNow;
        var estado = Uso.AddOrUpdate(
            ip,
            _ => (ahora, 1),
            (_, previo) => (ahora - previo.Inicio).TotalSeconds >= 60 ? (ahora, 1) : (previo.Inicio, previo.Cuenta + 1));
        return estado.Cuenta <= LimitePorMinuto;
    }
}

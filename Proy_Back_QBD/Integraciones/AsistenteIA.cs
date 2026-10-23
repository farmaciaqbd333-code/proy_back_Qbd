using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Proy_back_QBD.Integraciones
{
    /// <summary>Configuración de OpenAI (variable OPENAI_API_KEY).</summary>
    public class OpenAIOptions
    {
        public string? ApiKey { get; set; }
        public string Modelo { get; set; } = "gpt-4o-mini";
    }

    /// <summary>Un mensaje de la conversación.</summary>
    public record MensajeChat(string Rol, string Contenido);

    /// <summary>Respuesta del proveedor: el JSON tal cual (formato OpenAI) o un error.</summary>
    public record RespuestaAsistente(bool Exito, string? Json);

    /// <summary>Asistente de IA.</summary>
    public interface IAsistenteIA
    {
        bool EstaConfigurado { get; }

        Task<RespuestaAsistente> ChatAsync(IReadOnlyList<MensajeChat> mensajes, int maxTokens, double temperatura, CancellationToken ct = default);
    }

    /// <summary>Implementación con la API de OpenAI (chat/completions).</summary>
    public sealed class OpenAIAsistente : IAsistenteIA
    {
        private const string Url = "https://api.openai.com/v1/chat/completions";

        private readonly HttpClient _http;
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAIAsistente> _logger;

        public OpenAIAsistente(HttpClient http, IOptions<OpenAIOptions> options, ILogger<OpenAIAsistente> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public async Task<RespuestaAsistente> ChatAsync(IReadOnlyList<MensajeChat> mensajes, int maxTokens, double temperatura, CancellationToken ct = default)
        {
            if (!EstaConfigurado)
            {
                throw new InvalidOperationException("Asistente IA no configurado.");
            }

            var cuerpo = new JsonObject
            {
                ["model"] = _options.Modelo,
                ["messages"] = new JsonArray(mensajes
                    .Select(m => (JsonNode)new JsonObject { ["role"] = m.Rol, ["content"] = m.Contenido })
                    .ToArray()),
                ["max_tokens"] = maxTokens,
                ["temperature"] = temperatura,
            };

            using var peticion = new HttpRequestMessage(HttpMethod.Post, Url);
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            peticion.Content = new StringContent(cuerpo.ToJsonString(), Encoding.UTF8, "application/json");

            using var respuesta = await _http.SendAsync(peticion, ct);
            var json = await respuesta.Content.ReadAsStringAsync(ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI respondió {Status}: {Cuerpo}", (int)respuesta.StatusCode, json);
                return new RespuestaAsistente(false, null);
            }
            return new RespuestaAsistente(true, json);
        }
    }

    /// <summary>Límite de consultas por minuto por clave (IP o usuario).</summary>
    public interface ILimiteConsultas
    {
        bool Permitir(string clave);
    }

    /// <summary>Ventana fija de 60 segundos en memoria.</summary>
    public sealed class LimiteConsultasPorMinuto : ILimiteConsultas
    {
        private readonly int _maximo;
        private readonly ConcurrentDictionary<string, (DateTime Inicio, int Cuenta)> _uso = new();

        public LimiteConsultasPorMinuto(int maximo = 20)
        {
            _maximo = maximo;
        }

        public bool Permitir(string clave)
        {
            var ahora = DateTime.UtcNow;
            var estado = _uso.AddOrUpdate(
                clave,
                _ => (ahora, 1),
                (_, previo) => (ahora - previo.Inicio).TotalSeconds >= 60 ? (ahora, 1) : (previo.Inicio, previo.Cuenta + 1));
            return estado.Cuenta <= _maximo;
        }
    }
}

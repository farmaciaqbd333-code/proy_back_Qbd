using System.Net;
using Microsoft.Extensions.Options;

namespace Proy_back_QBD.Integraciones
{
    /// <summary>Configuración de PeruDevs (variable PERUDEVS_API_KEY).</summary>
    public class PeruDevsOptions
    {
        public string? ApiKey { get; set; }
        public string Url { get; set; } = "https://api.perudevs.com/api/v1";
    }

    /// <summary>Respuesta del proveedor: el código HTTP y el JSON tal cual lo devolvió.</summary>
    public record RespuestaDocumento(HttpStatusCode Status, string Json);

    /// <summary>Consulta de documentos de identidad: DNI (RENIEC) y RUC (SUNAT).</summary>
    public interface IConsultaDocumentos
    {
        bool EstaConfigurado { get; }

        Task<RespuestaDocumento> ConsultarDniAsync(string dni, CancellationToken ct = default);

        Task<RespuestaDocumento> ConsultarRucAsync(string ruc, CancellationToken ct = default);
    }

    /// <summary>
    /// Implementación con la API de PeruDevs. La clave solo vive en el servidor,
    /// así que nunca llega al navegador.
    /// </summary>
    public sealed class PeruDevsConsultaDocumentos : IConsultaDocumentos
    {
        private readonly HttpClient _http;
        private readonly PeruDevsOptions _options;
        private readonly ILogger<PeruDevsConsultaDocumentos> _logger;

        public PeruDevsConsultaDocumentos(HttpClient http, IOptions<PeruDevsOptions> options, ILogger<PeruDevsConsultaDocumentos> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_options.ApiKey);

        public Task<RespuestaDocumento> ConsultarDniAsync(string dni, CancellationToken ct = default)
            => ConsultarAsync("dni/complete", dni, ct);

        public Task<RespuestaDocumento> ConsultarRucAsync(string ruc, CancellationToken ct = default)
            => ConsultarAsync("ruc", ruc, ct);

        private async Task<RespuestaDocumento> ConsultarAsync(string recurso, string documento, CancellationToken ct)
        {
            if (!EstaConfigurado)
            {
                throw new InvalidOperationException("PeruDevs no configurado.");
            }

            var url = $"{_options.Url.TrimEnd('/')}/{recurso}" +
                      $"?document={Uri.EscapeDataString(documento)}&key={Uri.EscapeDataString(_options.ApiKey!)}";

            using var respuesta = await _http.GetAsync(url, ct);
            var json = await respuesta.Content.ReadAsStringAsync(ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                // No se registra la URL: lleva la clave.
                _logger.LogWarning("PeruDevs ({Recurso}) respondió {Status}", recurso, (int)respuesta.StatusCode);
            }
            return new RespuestaDocumento(respuesta.StatusCode, json);
        }
    }
}

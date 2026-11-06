using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Integraciones;
using Proy_back_QBD.Security;

namespace Proy_back_QBD.Controllers;

/// <summary>
/// Consulta de DNI y RUC. El frontend llama aquí (con su sesión) y el servidor consulta
/// al proveedor con la clave PERUDEVS_API_KEY, que nunca se expone en el navegador.
/// La respuesta es el JSON del proveedor tal cual (estado, mensaje, resultado).
/// </summary>
[ApiController]
[Route("api/documento")]
public partial class DocumentoController : ControllerBase
{
    private readonly IConsultaDocumentos _consulta;
    private readonly ILimiteConsultas _limite;
    private readonly ILogger<DocumentoController> _logger;

    public DocumentoController(IConsultaDocumentos consulta, ILimiteConsultas limite, ILogger<DocumentoController> logger)
    {
        _consulta = consulta;
        _limite = limite;
        _logger = logger;
    }

    /// <summary>Datos de una persona por su DNI (8 dígitos).</summary>
    [HttpGet("dni/{dni}")]
    public Task<IActionResult> Dni(string dni, CancellationToken ct)
        => Consultar(dni, DniValido(), "El DNI debe tener 8 dígitos.", _consulta.ConsultarDniAsync, ct);

    /// <summary>Datos de una empresa por su RUC (11 dígitos).</summary>
    [HttpGet("ruc/{ruc}")]
    public Task<IActionResult> Ruc(string ruc, CancellationToken ct)
        => Consultar(ruc, RucValido(), "El RUC debe tener 11 dígitos.", _consulta.ConsultarRucAsync, ct);

    private async Task<IActionResult> Consultar(
        string documento,
        Regex formato,
        string mensajeFormato,
        Func<string, CancellationToken, Task<RespuestaDocumento>> consultar,
        CancellationToken ct)
    {
        if (!_consulta.EstaConfigurado)
        {
            _logger.LogError("Consulta de documentos no configurada: falta la variable PERUDEVS_API_KEY.");
            return StatusCode(503, Error("La consulta de documentos no está configurada en el servidor."));
        }

        documento = (documento ?? "").Trim();
        if (!formato.IsMatch(documento))
        {
            return BadRequest(Error(mensajeFormato));
        }

        var clave = JwtTokens.UsuarioId(User) is var id && id > 0
            ? $"doc:u:{id}"
            : $"doc:ip:{HttpContext.Connection.RemoteIpAddress}";
        if (!_limite.Permitir(clave))
        {
            return StatusCode(429, Error("Demasiadas consultas seguidas. Espere un minuto e intente nuevamente."));
        }

        RespuestaDocumento respuesta;
        try
        {
            respuesta = await consultar(documento, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo contactar a PeruDevs.");
            return StatusCode(502, Error("No se pudo contactar al servicio de consulta. Intente nuevamente."));
        }

        // Un 401/403 del proveedor significa que la clave del servidor es inválida.
        // No se reenvía tal cual: el frontend cerraría la sesión del usuario al ver un 401.
        var status = respuesta.Status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => HttpStatusCode.BadGateway,
            _ => respuesta.Status,
        };

        if (string.IsNullOrWhiteSpace(respuesta.Json))
        {
            return StatusCode((int)status);
        }
        return new ContentResult
        {
            StatusCode = (int)status,
            Content = respuesta.Json,
            ContentType = "application/json; charset=utf-8",
        };
    }

    private static object Error(string mensaje) => new { estado = false, mensaje };

    [GeneratedRegex(@"^\d{8}$")]
    private static partial Regex DniValido();

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex RucValido();
}

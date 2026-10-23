using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Integraciones;

namespace Proy_back_QBD.Controllers;

/// <summary>
/// Datos para subir un archivo (imagen de pedido o factura).
/// </summary>
public class SubirArchivoReq
{
    /// <summary>Archivo a subir.</summary>
    public IFormFile? Archivo { get; set; }

    /// <summary>Carpeta destino: "pedidos" o "facturas".</summary>
    public string? Carpeta { get; set; }
}

/// <summary>
/// Recibe archivos y los guarda en el almacenamiento configurado (S3).
/// El controlador solo valida la petición; el guardado lo hace <see cref="IAlmacenamientoArchivos"/>.
/// </summary>
[ApiController]
[Route("api/archivo")]
public class ArchivoController : ControllerBase
{
    private const long MaxBytes = 15 * 1024 * 1024; // 15 MB

    private static readonly HashSet<string> CarpetasPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { "pedidos", "facturas" };

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic", ".heif", ".bmp", ".tif", ".tiff", ".pdf" };

    private readonly IAlmacenamientoArchivos _almacenamiento;
    private readonly ILogger<ArchivoController> _logger;

    public ArchivoController(IAlmacenamientoArchivos almacenamiento, ILogger<ArchivoController> logger)
    {
        _almacenamiento = almacenamiento;
        _logger = logger;
    }

    /// <summary>
    /// Sube un archivo y devuelve su URL pública.
    /// </summary>
    [HttpPost("subir")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Subir([FromForm] SubirArchivoReq request, CancellationToken ct)
    {
        var archivo = request.Archivo;
        if (archivo == null || archivo.Length == 0)
        {
            return BadRequest(new { message = "Debe enviar un archivo." });
        }
        if (archivo.Length > MaxBytes)
        {
            return BadRequest(new { message = "El archivo supera el tamaño máximo de 15 MB." });
        }

        var carpeta = string.IsNullOrWhiteSpace(request.Carpeta) ? "pedidos" : request.Carpeta.Trim();
        if (!CarpetasPermitidas.Contains(carpeta))
        {
            return BadRequest(new { message = "Carpeta no permitida." });
        }

        var extension = Path.GetExtension(archivo.FileName ?? string.Empty);
        if (string.IsNullOrEmpty(extension) || !ExtensionesPermitidas.Contains(extension))
        {
            return BadRequest(new { message = "Tipo de archivo no permitido. Use imágenes (JPG, PNG, WEBP, GIF, HEIC) o PDF." });
        }

        if (!_almacenamiento.EstaConfigurado)
        {
            _logger.LogError("Almacenamiento no configurado: faltan AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY / AWS_S3_BUCKET.");
            return StatusCode(503, new { message = "El almacenamiento de archivos no está configurado en el servidor." });
        }

        var key = $"{carpeta}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{NombreSeguro(archivo.FileName)}{extension.ToLowerInvariant()}";

        try
        {
            await using var stream = archivo.OpenReadStream();
            var subido = await _almacenamiento.SubirAsync(stream, key, archivo.ContentType, ct);
            return Ok(new { url = subido.Url, key = subido.Key });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subiendo archivo ({Key})", key);
            return StatusCode(500, new { message = "No se pudo subir el archivo. Intente nuevamente." });
        }
    }

    /// <summary>Solo letras, números, punto y guion; máximo 80 caracteres.</summary>
    private static string NombreSeguro(string? nombreArchivo)
    {
        var nombreBase = Path.GetFileNameWithoutExtension(nombreArchivo ?? "archivo");
        var limpio = new string(nombreBase.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '.' ? c : '_').ToArray());
        return limpio.Length > 80 ? limpio.Substring(0, 80) : limpio;
    }
}

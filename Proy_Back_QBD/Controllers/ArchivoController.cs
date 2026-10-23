using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

namespace Proy_back_QBD.Controllers;

/// <summary>
/// Datos para subir un archivo (imagen de pedido o factura) a S3.
/// </summary>
public class SubirArchivoReq
{
    /// <summary>Archivo a subir.</summary>
    public IFormFile? Archivo { get; set; }

    /// <summary>Carpeta destino: "pedidos" o "facturas".</summary>
    public string? Carpeta { get; set; }
}

/// <summary>
/// Sube archivos a S3 desde el servidor. Las credenciales de AWS viven solo en las
/// variables de entorno del backend (nunca en el frontend):
/// AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION, AWS_S3_BUCKET.
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

    private readonly IConfiguration _configuration;
    private readonly ILogger<ArchivoController> _logger;

    public ArchivoController(IConfiguration configuration, ILogger<ArchivoController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sube un archivo a S3 y devuelve su URL pública.
    /// </summary>
    [HttpPost("subir")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Subir([FromForm] SubirArchivoReq request)
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

        var accessKey = _configuration["AWS_ACCESS_KEY_ID"];
        var secretKey = _configuration["AWS_SECRET_ACCESS_KEY"];
        var region = _configuration["AWS_REGION"] ?? "us-east-1";
        var bucket = _configuration["AWS_S3_BUCKET"];

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(bucket))
        {
            _logger.LogError("Almacenamiento S3 no configurado: faltan variables AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY / AWS_S3_BUCKET.");
            return StatusCode(503, new { message = "El almacenamiento de archivos no está configurado en el servidor." });
        }

        // Nombre seguro: solo letras, números, punto y guion (igual que hacía el frontend)
        var nombreBase = Path.GetFileNameWithoutExtension(archivo.FileName ?? "archivo");
        var nombreLimpio = new string(nombreBase.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '.' ? c : '_').ToArray());
        if (nombreLimpio.Length > 80) nombreLimpio = nombreLimpio.Substring(0, 80);
        var key = $"{carpeta}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{nombreLimpio}{extension.ToLowerInvariant()}";

        try
        {
            var credenciales = new BasicAWSCredentials(accessKey, secretKey);
            using var cliente = new AmazonS3Client(credenciales, RegionEndpoint.GetBySystemName(region));
            await using var stream = archivo.OpenReadStream();

            var put = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream,
                ContentType = string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType,
            };

            await cliente.PutObjectAsync(put);

            var url = $"https://{bucket}.s3.{region}.amazonaws.com/{key}";
            return Ok(new { url, key });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subiendo archivo a S3 ({Key})", key);
            return StatusCode(500, new { message = "No se pudo subir el archivo. Intente nuevamente." });
        }
    }
}

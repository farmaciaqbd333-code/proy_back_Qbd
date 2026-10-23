using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Proy_back_QBD.Integraciones
{
    /// <summary>Configuración de S3 (variables AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION, AWS_S3_BUCKET).</summary>
    public class AwsS3Options
    {
        public string? AccessKeyId { get; set; }
        public string? SecretAccessKey { get; set; }
        public string Region { get; set; } = "us-east-1";
        public string? Bucket { get; set; }
    }

    /// <summary>Resultado de una subida.</summary>
    public record ArchivoSubido(string Url, string Key);

    /// <summary>Almacenamiento de archivos (imágenes de pedidos, facturas).</summary>
    public interface IAlmacenamientoArchivos
    {
        /// <summary>false si faltan credenciales o bucket.</summary>
        bool EstaConfigurado { get; }

        Task<ArchivoSubido> SubirAsync(Stream contenido, string key, string contentType, CancellationToken ct = default);
    }

    /// <summary>Implementación con Amazon S3. Las credenciales viven solo en el servidor.</summary>
    public sealed class S3AlmacenamientoArchivos : IAlmacenamientoArchivos, IDisposable
    {
        private readonly AwsS3Options _options;
        private readonly Lazy<AmazonS3Client> _cliente;

        public S3AlmacenamientoArchivos(IOptions<AwsS3Options> options)
        {
            _options = options.Value;
            _cliente = new Lazy<AmazonS3Client>(() => new AmazonS3Client(
                new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
                RegionEndpoint.GetBySystemName(string.IsNullOrWhiteSpace(_options.Region) ? "us-east-1" : _options.Region)));
        }

        public bool EstaConfigurado =>
            !string.IsNullOrWhiteSpace(_options.AccessKeyId) &&
            !string.IsNullOrWhiteSpace(_options.SecretAccessKey) &&
            !string.IsNullOrWhiteSpace(_options.Bucket);

        public async Task<ArchivoSubido> SubirAsync(Stream contenido, string key, string contentType, CancellationToken ct = default)
        {
            if (!EstaConfigurado)
            {
                throw new InvalidOperationException("Almacenamiento S3 no configurado.");
            }

            await _cliente.Value.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key,
                InputStream = contenido,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            }, ct);

            var region = string.IsNullOrWhiteSpace(_options.Region) ? "us-east-1" : _options.Region;
            return new ArchivoSubido($"https://{_options.Bucket}.s3.{region}.amazonaws.com/{key}", key);
        }

        public void Dispose()
        {
            if (_cliente.IsValueCreated)
            {
                _cliente.Value.Dispose();
            }
        }
    }
}

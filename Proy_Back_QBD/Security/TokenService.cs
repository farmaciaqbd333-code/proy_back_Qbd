using Microsoft.Extensions.Options;
using Proy_back_QBD.Dto.Response;

namespace Proy_back_QBD.Security
{
    /// <summary>Configuración de los tokens JWT (variable de entorno JWT_SECRET).</summary>
    public class JwtOptions
    {
        /// <summary>Clave de firma, mínimo 32 caracteres.</summary>
        public string Secret { get; set; } = string.Empty;
    }

    /// <summary>Emite el token de sesión de un usuario.</summary>
    public interface ITokenService
    {
        (string Token, DateTime Expira) Generar(UsuarioLoginDataRes usuario);

        /// <summary>Token nuevo con los mismos datos del usuario autenticado.</summary>
        (string Token, DateTime Expira) Renovar(System.Security.Claims.ClaimsPrincipal usuario);
    }

    /// <summary>Implementación con JWT firmado (HMAC-SHA256).</summary>
    public class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public (string Token, DateTime Expira) Generar(UsuarioLoginDataRes usuario) =>
            JwtTokens.Generar(usuario, _options.Secret);

        public (string Token, DateTime Expira) Renovar(System.Security.Claims.ClaimsPrincipal usuario) =>
            JwtTokens.Renovar(usuario, _options.Secret);
    }
}

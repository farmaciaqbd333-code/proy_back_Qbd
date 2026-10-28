using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Proy_back_QBD.Dto.Response;

namespace Proy_back_QBD.Security
{
    /// <summary>
    /// Emisión de tokens JWT para la sesión de cada usuario.
    /// La clave de firma es la variable de entorno JWT_SECRET (mínimo 32 caracteres).
    /// </summary>
    public static class JwtTokens
    {
        public const string Issuer = "qbd-api";
        public const string Audience = "qbd-web";

        /// <summary>Nombre del claim con el tipo de usuario (1 = ADMIN, 2, 3, 4, 8, 9...).</summary>
        public const string ClaimRol = "rol";
        /// <summary>Nombre del claim con la sede del usuario.</summary>
        public const string ClaimSede = "sede";
        /// <summary>Nombre del claim con el acceso a Central (idGeneral).</summary>
        public const string ClaimIdGeneral = "idGeneral";

        /// <summary>Duración de la sesión.</summary>
        public static readonly TimeSpan Duracion = TimeSpan.FromHours(12);

        public static SymmetricSecurityKey CrearClave(string secreto) =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secreto));

        /// <summary>Genera el token del usuario que acaba de iniciar sesión.</summary>
        public static (string Token, DateTime Expira) Generar(UsuarioLoginDataRes usuario, string secreto)
        {
            var expira = DateTime.UtcNow.Add(Duracion);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, (usuario.Id ?? 0).ToString()),
                new(ClaimRol, (usuario.TipoId ?? 0).ToString()),
                new(ClaimSede, (usuario.SedeId ?? 0).ToString()),
                new(ClaimIdGeneral, (usuario.IdGeneral ?? 0).ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };
            if (!string.IsNullOrWhiteSpace(usuario.NombreCompleto))
            {
                claims.Add(new Claim("nombre", usuario.NombreCompleto));
            }

            var credenciales = new SigningCredentials(CrearClave(secreto), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                notBefore: DateTime.UtcNow.AddSeconds(-5),
                expires: expira,
                signingCredentials: credenciales);

            return (new JwtSecurityTokenHandler().WriteToken(token), expira);
        }

        /// <summary>
        /// Emite un token nuevo (con 12 horas más) con los mismos datos del token actual.
        /// Se usa para que la sesión no se corte mientras el usuario sigue trabajando.
        /// </summary>
        public static (string Token, DateTime Expira) Renovar(ClaimsPrincipal user, string secreto)
        {
            var expira = DateTime.UtcNow.Add(Duracion);
            string Valor(string tipo) => user.FindFirst(tipo)?.Value ?? "0";

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, Valor(JwtRegisteredClaimNames.Sub)),
                new(ClaimRol, Valor(ClaimRol)),
                new(ClaimSede, Valor(ClaimSede)),
                new(ClaimIdGeneral, Valor(ClaimIdGeneral)),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };
            var nombre = user.FindFirst("nombre")?.Value;
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                claims.Add(new Claim("nombre", nombre));
            }

            var credenciales = new SigningCredentials(CrearClave(secreto), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                notBefore: DateTime.UtcNow.AddSeconds(-5),
                expires: expira,
                signingCredentials: credenciales);

            return (new JwtSecurityTokenHandler().WriteToken(token), expira);
        }

        /// <summary>Id del usuario del token (0 si no hay).</summary>
        public static int UsuarioId(ClaimsPrincipal user) =>
            int.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : 0;

        /// <summary>Tipo de usuario del token (0 si no hay).</summary>
        public static int Rol(ClaimsPrincipal user) =>
            int.TryParse(user.FindFirst(ClaimRol)?.Value, out var rol) ? rol : 0;
    }

    /// <summary>Utilidades para contraseñas con BCrypt.</summary>
    public static class Contrasenas
    {
        /// <summary>true si el valor guardado ya es un hash BCrypt.</summary>
        public static bool EsHash(string? valor) =>
            !string.IsNullOrEmpty(valor) && valor.StartsWith("$2") && valor.Length >= 59;

        public static string Hashear(string contrasena) =>
            BCrypt.Net.BCrypt.HashPassword(contrasena.Trim(), workFactor: 11);

        /// <summary>
        /// Verifica la contraseña. Acepta hash BCrypt o, mientras dure la migración,
        /// la contraseña antigua guardada en texto plano.
        /// </summary>
        public static bool Verificar(string contrasena, string? guardada)
        {
            if (string.IsNullOrEmpty(guardada)) return false;
            if (EsHash(guardada))
            {
                try { return BCrypt.Net.BCrypt.Verify(contrasena.Trim(), guardada); }
                catch { return false; }
            }
            return string.Equals(guardada.Trim(), contrasena.Trim(), StringComparison.Ordinal);
        }
    }
}

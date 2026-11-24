using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Proy_back_QBD.Security
{
    /// <summary>
    /// Tokens de la app de médicos. Son JWT firmados con la misma clave que los del sistema,
    /// pero con OTRA audiencia ("qbd-app-medico") y su propio esquema de autenticación:
    ///  - un token de médico NO sirve en las rutas del personal (su audiencia es "qbd-web");
    ///  - un token del personal NO sirve en las rutas del médico.
    /// El "sub" es "medico:{id de medico_acceso}" (no numérico) para que los filtros del personal
    /// (<see cref="UsuarioDeSesionFilter"/>, <see cref="TurnoActivoFilter"/>, <see cref="HorarioIngresoFilter"/>)
    /// nunca lo confundan con un usuario.
    /// </summary>
    public static class AppMedicoTokens
    {
        public const string Esquema = "AppMedico";
        public const string Politica = "Medico";
        public const string Audience = "qbd-app-medico";

        public const string ClaimTipo = "tipo";
        public const string TipoMedico = "medico";
        public const string ClaimAcceso = "acceso";
        public const string ClaimCmp = "cmp";
        public const string ClaimVersion = "ver";

        /// <summary>Sesión larga: es una app móvil. Se invalida antes si el personal restablece la contraseña.</summary>
        public static readonly TimeSpan Duracion = TimeSpan.FromDays(30);

        public static (string Token, DateTime Expira) Generar(int accesoId, string cmp, int version, string nombre, string secreto)
        {
            var expira = DateTime.UtcNow.Add(Duracion);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, $"medico:{accesoId}"),
                new(ClaimTipo, TipoMedico),
                new(ClaimAcceso, accesoId.ToString()),
                new(ClaimCmp, cmp),
                new(ClaimVersion, version.ToString()),
                new("nombre", nombre ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            };

            var token = new JwtSecurityToken(
                issuer: JwtTokens.Issuer,
                audience: Audience,
                claims: claims,
                notBefore: DateTime.UtcNow.AddSeconds(-5),
                expires: expira,
                signingCredentials: new SigningCredentials(JwtTokens.CrearClave(secreto), SecurityAlgorithms.HmacSha256));

            return (new JwtSecurityTokenHandler().WriteToken(token), expira);
        }

        public static TokenValidationParameters Validacion(string secreto) => new()
        {
            ValidateIssuer = true,
            ValidIssuer = JwtTokens.Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtTokens.CrearClave(secreto),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimCmp,
        };

        /// <summary>Id de medico_acceso del token (0 si no es un token de médico).</summary>
        public static int AccesoId(ClaimsPrincipal user) =>
            user.FindFirst(ClaimTipo)?.Value == TipoMedico && int.TryParse(user.FindFirst(ClaimAcceso)?.Value, out var id) ? id : 0;

        public static int Version(ClaimsPrincipal user) =>
            int.TryParse(user.FindFirst(ClaimVersion)?.Value, out var v) ? v : 0;
    }
}

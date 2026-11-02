using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Proy_back_QBD.Services
{
    public static class PasswordHasher
    {
        private const int WorkFactor = 11;
        private static readonly Regex FormatoBCrypt = new(@"^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$", RegexOptions.Compiled);

        public static bool EsHash(string? almacenada)
        {
            return almacenada != null && FormatoBCrypt.IsMatch(almacenada.Trim());
        }

        public static string Hashear(string contrasena)
        {
            // El login histórico comparaba contra el valor sin espacios: se conserva ese criterio
            return BCrypt.Net.BCrypt.HashPassword(contrasena.Trim(), WorkFactor);
        }

        // Acepta hashes BCrypt y, hasta terminar la migración, contraseñas legadas en texto plano
        public static bool Verificar(string contrasena, string? almacenada)
        {
            if (string.IsNullOrEmpty(contrasena) || string.IsNullOrWhiteSpace(almacenada))
            {
                return false;
            }

            string valor = almacenada.Trim();
            if (EsHash(valor))
            {
                return BCrypt.Net.BCrypt.Verify(contrasena, valor);
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(contrasena),
                Encoding.UTF8.GetBytes(valor));
        }
    }
}

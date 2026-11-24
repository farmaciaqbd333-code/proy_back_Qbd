using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Proy_back_QBD.Services.AppMedico
{
    /// <summary>Reglas pequeñas y puras de la app de médicos (fáciles de probar).</summary>
    public static class AppMedicoReglas
    {
        public const int MinContrasena = 6;
        public const int MaxContrasena = 72; // límite de BCrypt
        public const int MaxIntentos = 5;
        public static readonly TimeSpan Bloqueo = TimeSpan.FromMinutes(15);
        public const int MaxItemsPorPedido = 60;
        public const int MaxCantidad = 999;

        /// <summary>
        /// CMP normalizado: sin espacios, en mayúsculas y sin ceros a la izquierda
        /// ("091379" y "91379" son el mismo médico). Vacío si no hay CMP.
        /// </summary>
        public static string NormalizarCmp(string? cmp)
        {
            if (string.IsNullOrWhiteSpace(cmp)) return "";
            var limpio = new string(cmp.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
            var sinCeros = limpio.TrimStart('0');
            return sinCeros.Length == 0 ? limpio : sinCeros;
        }

        public static string? ValidarContrasena(string? contrasena)
        {
            var c = contrasena?.Trim() ?? "";
            if (c.Length < MinContrasena) return $"La contraseña debe tener al menos {MinContrasena} caracteres.";
            if (c.Length > MaxContrasena) return $"La contraseña no puede tener más de {MaxContrasena} caracteres.";
            return null;
        }

        public static string Hashear(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena.Trim(), workFactor: 11);

        /// <summary>Solo acepta hash BCrypt (a diferencia de los usuarios, aquí nunca hubo texto plano).</summary>
        public static bool Verificar(string? contrasena, string? hash)
        {
            if (string.IsNullOrEmpty(contrasena) || string.IsNullOrEmpty(hash) || !hash.StartsWith("$2")) return false;
            try { return BCrypt.Net.BCrypt.Verify(contrasena.Trim(), hash); }
            catch { return false; }
        }

        /// <summary>Contraseña temporal de 8 caracteres sin letras confusas (0/O, 1/l/I).</summary>
        public static string GenerarTemporal()
        {
            const string alfabeto = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var sb = new StringBuilder(8);
            for (int i = 0; i < 8; i++) sb.Append(alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)]);
            return sb.ToString();
        }

        private static readonly Regex DocumentoPaciente = new("^[0-9A-Za-z]{8,12}$", RegexOptions.Compiled);

        /// <summary>DNI (8 dígitos) o carné de extranjería / pasaporte (hasta 12).</summary>
        public static bool DocumentoValido(string? doc) => DocumentoPaciente.IsMatch(doc ?? "");

        /// <summary>Mayúsculas y sin tildes.</summary>
        public static string SinTildes(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var normal = texto.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normal.Length);
            foreach (var ch in normal)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Especialidad del directorio de médicos → códigos de especialidad de la Lista de Precios
        private static readonly (string Clave, string[] Codigos)[] Mapa =
        {
            ("DERMOEST", new[] { "DERM", "COSM" }),
            ("DERMATO", new[] { "DERM" }),
            ("COSMET", new[] { "COSM" }),
            ("ESTETIC", new[] { "COSM" }),
            ("GINECO", new[] { "GO" }),
            ("OBSTETR", new[] { "GO" }),
            ("PEDIATR", new[] { "PED" }),
            ("GERIATR", new[] { "GER" }),
            ("OTORRINO", new[] { "ORL" }),
            ("TRAUMATO", new[] { "TRAU" }),
            ("REUMATO", new[] { "REUM" }),
            ("OFTALMO", new[] { "OFT" }),
            ("UROLOG", new[] { "URO" }),
            ("GASTRO", new[] { "GAST" }),
            ("ENDOCRINO", new[] { "ENDO" }),
            ("NEUMO", new[] { "NEUM" }),
        };

        /// <summary>
        /// Códigos de especialidad de la Lista de Precios que corresponden a la especialidad del médico.
        /// Solo devuelve códigos que existen en la lista (<paramref name="codigosLista"/>). Si no hay
        /// correspondencia (p. ej. MEDICINA INTERNA, MED. CIRUJANO) devuelve vacío: el médico ve todo.
        /// </summary>
        public static List<string> EspecialidadesSugeridas(string? especialidadMedico, IEnumerable<string> codigosLista)
        {
            var esp = SinTildes(especialidadMedico);
            if (esp.Length == 0) return new();
            var lista = codigosLista.Where(c => !string.IsNullOrWhiteSpace(c))
                .ToDictionary(c => SinTildes(c), c => c, StringComparer.Ordinal);

            var resultado = new List<string>();
            void Agregar(string codigo)
            {
                if (lista.TryGetValue(SinTildes(codigo), out var real) && !resultado.Contains(real)) resultado.Add(real);
            }

            foreach (var (clave, codigos) in Mapa)
                if (esp.Contains(clave)) foreach (var c in codigos) Agregar(c);

            // Regla general: un código de 3+ letras con el que empieza alguna palabra de la especialidad
            // (GER → GERIATRIA, PED → PEDIATRIA...).
            var palabras = Regex.Split(esp, "[^A-Z]+").Where(p => p.Length > 0).ToArray();
            foreach (var (normal, real) in lista)
                if (normal.Length >= 3 && palabras.Any(p => p.StartsWith(normal, StringComparison.Ordinal))) Agregar(real);

            return resultado;
        }
    }
}

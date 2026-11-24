using Proy_back_QBD.Security;

namespace Proy_back_QBD.Services
{
    /// <summary>
    /// Estado que se muestra en "Control de Usuarios", calculado en vivo:
    /// <list type="bullet">
    /// <item>BAJA: dado de baja a mano (<c>usuarios.estado = false</c>). Manda sobre todo y es lo único que impide iniciar sesión.</item>
    /// <item>ACTIVO: está en su turno, de la hora de entrada − 30 min a la de salida + 30 min, y ese día marcó su ENTRADA.
    /// El administrador siempre sale activo.</item>
    /// <item>INACTIVO: fuera de esa ventana, sin entrada marcada o sin horario.</item>
    /// </list>
    /// Las horas son de Lima (UTC−5, sin horario de verano).
    /// </summary>
    public static class EstadoTurno
    {
        public const string Activo = "ACTIVO";
        public const string Inactivo = "INACTIVO";
        public const string Baja = "BAJA";

        public static readonly TimeSpan Margen = TimeSpan.FromMinutes(30);

        /// <summary>Desfase de Lima respecto a UTC, el mismo que usan las consultas de asistencias.</summary>
        public static readonly TimeSpan DesfaseLima = TimeSpan.FromHours(-5);

        public static DateTime AhoraLima() => DateTime.UtcNow.Add(DesfaseLima);

        /// <summary>
        /// Pasa una fecha de la base a hora de Lima según cómo llega:
        /// - Utc: se le restan 5 h.
        /// - Local: se convierte desde la zona del servidor (en producción ya es Lima: TZ=America/Lima).
        /// - Unspecified: ya está en hora de Lima (así la entrega Npgsql en este servidor).
        /// fecha_creacion de asistencias llega en hora de Lima: igual que hora_marcada.
        /// </summary>
        public static DateTime ALima(DateTime fecha) => fecha.Kind switch
        {
            DateTimeKind.Utc => DateTime.SpecifyKind(fecha.Add(DesfaseLima), DateTimeKind.Unspecified),
            DateTimeKind.Local => DateTime.SpecifyKind(fecha.ToUniversalTime().Add(DesfaseLima), DateTimeKind.Unspecified),
            _ => fecha
        };

        /// <summary>Día de Lima en que se registró una marca.</summary>
        public static DateOnly DiaLima(DateTime fechaCreacion) => DateOnly.FromDateTime(ALima(fechaCreacion));

        /// <summary>
        /// Desde cuándo traer marcas para el turno de hoy y el de ayer: inicio de anteayer en Lima,
        /// como instante UTC (así Npgsql lo compara bien venga la columna como venga).
        /// </summary>
        public static DateTime DesdeParaConsulta(DateTime ahoraLima) =>
            DateTime.SpecifyKind(ahoraLima.Date.AddDays(-2) - DesfaseLima, DateTimeKind.Utc);

        /// <summary>
        /// Puede iniciar sesión: el administrador siempre; los demás solo de su entrada − 30 min
        /// a su salida + 30 min (turno de hoy o el de ayer si cruza la medianoche). Sin horario, no.
        /// </summary>
        public static bool PuedeIngresar(int? tipoId, TimeOnly? horarioEntrada, TimeOnly? horarioSalida, DateTime ahoraLima)
        {
            if (tipoId == Roles.Admin)
            {
                return true;
            }
            if (horarioEntrada == null || horarioSalida == null)
            {
                return false;
            }
            DateOnly hoy = DateOnly.FromDateTime(ahoraLima);
            foreach (DateOnly dia in new[] { hoy, hoy.AddDays(-1) })
            {
                DateTime entrada = dia.ToDateTime(horarioEntrada.Value);
                DateTime salida = dia.ToDateTime(horarioSalida.Value);
                if (salida <= entrada)
                {
                    salida = salida.AddDays(1);
                }
                if (ahoraLima >= entrada - Margen && ahoraLima <= salida + Margen)
                {
                    return true;
                }
            }
            return false;
        }

        // ===== Asistencia del día (columna ASISTENCIA de Control de Usuarios) =====
        public const string Trabajando = "TRABAJANDO";
        public const string Salio = "SALIO";
        public const string SinMarcar = "SIN_MARCAR";

        /// <summary>Una entrada de hace más de esto ya no cuenta como turno en curso.</summary>
        public static readonly TimeSpan VigenciaEntrada = TimeSpan.FromHours(20);

        public sealed record AsistenciaDelDia(string Estado, DateTime? Entrada, DateTime? Salida);

        /// <summary>
        /// TRABAJANDO: marcó su entrada (en las últimas 20 h) y todavía no su salida.
        /// SALIO: después de esa entrada marcó su salida. SIN_MARCAR: no tiene entrada reciente.
        /// Las horas van en hora de Lima.
        /// </summary>
        public static AsistenciaDelDia AsistenciaHoy(
            IEnumerable<DateTime> entradasLima,
            IEnumerable<DateTime> salidasLima,
            DateTime ahoraLima)
        {
            var vigentes = entradasLima.Where(e => e <= ahoraLima && ahoraLima - e <= VigenciaEntrada).ToList();
            if (vigentes.Count == 0)
            {
                return new AsistenciaDelDia(SinMarcar, null, null);
            }
            DateTime ultima = vigentes.Max();
            var salidas = salidasLima.ToList();

            // Hora que se muestra: la PRIMERA entrada del turno en curso (como el reporte de asistencia).
            // Si marcó la entrada dos veces (08:55 y 09:00), el turno empezó a las 08:55; antes se mostraba la última.
            // El turno empieza después de la última salida anterior a la última entrada.
            DateTime? salidaPrevia = salidas.Where(s => s < ultima).DefaultIfEmpty().Max();
            DateTime primera = vigentes
                .Where(e => salidaPrevia == null || salidaPrevia == default(DateTime) || e > salidaPrevia)
                .Min();

            DateTime? salida = salidas
                .Where(s => s >= ultima && s <= ahoraLima)
                .DefaultIfEmpty()
                .Max();
            return salida == null || salida == default(DateTime)
                ? new AsistenciaDelDia(Trabajando, primera, null)
                : new AsistenciaDelDia(Salio, primera, salida);
        }

        public const string MotivoSinHorario = "sin horario cargado";
        public const string MotivoSinEntrada = "sin entrada hoy";
        public const string MotivoFueraDeHorario = "fuera de horario";

        /// <summary>
        /// Por qué un usuario sale INACTIVO (null si está ACTIVO o de BAJA):
        /// sin horario cargado, sin entrada marcada (hoy ni ayer) o fuera de su horario.
        /// </summary>
        public static string? Motivo(
            bool estadoManual,
            int? tipoId,
            TimeOnly? horarioEntrada,
            TimeOnly? horarioSalida,
            IReadOnlyCollection<DateOnly> diasConEntrada,
            DateTime ahoraLima)
        {
            if (Calcular(estadoManual, tipoId, horarioEntrada, horarioSalida, diasConEntrada, ahoraLima) != Inactivo)
            {
                return null;
            }
            if (horarioEntrada == null || horarioSalida == null)
            {
                return MotivoSinHorario;
            }
            DateOnly hoy = DateOnly.FromDateTime(ahoraLima);
            return diasConEntrada.Contains(hoy) || diasConEntrada.Contains(hoy.AddDays(-1))
                ? MotivoFueraDeHorario
                : MotivoSinEntrada;
        }

        /// <param name="estadoManual">Columna usuarios.estado: false = dado de baja.</param>
        /// <param name="diasConEntrada">Días (de Lima) en que el usuario marcó su entrada.</param>
        /// <param name="ahoraLima">Fecha y hora actual en Lima.</param>
        public static string Calcular(
            bool estadoManual,
            int? tipoId,
            TimeOnly? horarioEntrada,
            TimeOnly? horarioSalida,
            IReadOnlyCollection<DateOnly> diasConEntrada,
            DateTime ahoraLima)
        {
            if (!estadoManual)
            {
                return Baja;
            }
            if (tipoId == Roles.Admin)
            {
                return Activo;
            }
            if (horarioEntrada == null || horarioSalida == null || diasConEntrada.Count == 0)
            {
                return Inactivo;
            }

            // Se mira el turno que empezó hoy y el de ayer, por si el turno cruza la medianoche.
            DateOnly hoy = DateOnly.FromDateTime(ahoraLima);
            foreach (DateOnly dia in new[] { hoy, hoy.AddDays(-1) })
            {
                if (!diasConEntrada.Contains(dia))
                {
                    continue;
                }
                DateTime entrada = dia.ToDateTime(horarioEntrada.Value);
                DateTime salida = dia.ToDateTime(horarioSalida.Value);
                if (salida <= entrada)
                {
                    salida = salida.AddDays(1);
                }
                if (ahoraLima >= entrada - Margen && ahoraLima <= salida + Margen)
                {
                    return Activo;
                }
            }
            return Inactivo;
        }
    }
}

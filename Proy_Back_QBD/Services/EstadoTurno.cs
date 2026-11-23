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

        /// <summary>Día de Lima en que se registró una marca (fecha_creacion se guarda en UTC).</summary>
        public static DateOnly DiaLima(DateTime fechaCreacionUtc) => DateOnly.FromDateTime(fechaCreacionUtc.Add(DesfaseLima));

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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;

namespace Proy_back_QBD.Security
{
    /// <summary>
    /// Un usuario INACTIVO (fuera de su turno o sin entrada marcada hoy) no puede registrar,
    /// modificar ni eliminar nada. Sí puede iniciar sesión, mirar pantallas y marcar asistencia.
    ///
    /// Se revisa en cada petición que escribe (POST, PUT, PATCH, DELETE) con el mismo cálculo que
    /// muestra "Control de Usuarios" (<see cref="EstadoTurno"/>). Quedan libres:
    ///   - Admin, Supervisor y S1 (gestionan usuarios y deben poder corregir horarios).
    ///   - Asistencia (para poder marcar la entrada y salir del estado INACTIVO).
    ///   - Peticiones sin sesión (login) y POST que solo consultan (Listar, Obtener, Validar...).
    /// </summary>
    public sealed class TurnoActivoFilter : IAsyncActionFilter
    {
        public const string Codigo = "FUERA_DE_TURNO";

        private static readonly string[] ControladoresLibres = { "Asistencia" };

        // POST que en realidad solo consultan datos
        private static readonly string[] PrefijosDeConsulta =
            { "Listar", "Obtener", "Buscar", "Consultar", "Validar", "Verificar", "Calcular", "Exportar", "Generar" };

        private readonly ApiContext _context;

        public TurnoActivoFilter(ApiContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string? motivo = await MotivoDeBloqueo(context);
            if (motivo != null)
            {
                context.Result = new ObjectResult(new { codigo = Codigo, message = motivo })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
            await next();
        }

        private async Task<string?> MotivoDeBloqueo(ActionExecutingContext context)
        {
            var metodo = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo)) return null;

            var user = context.HttpContext.User;
            int idUsuario = JwtTokens.UsuarioId(user);
            if (idUsuario <= 0) return null; // sin sesión: login y rutas públicas
            if (Roles.PuedeGestionarUsuarios(JwtTokens.Rol(user))) return null;

            if (context.ActionDescriptor is ControllerActionDescriptor accion)
            {
                if (ControladoresLibres.Contains(accion.ControllerName, StringComparer.OrdinalIgnoreCase)) return null;
                if (HttpMethods.IsPost(metodo) &&
                    PrefijosDeConsulta.Any(p => accion.ActionName.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return null;
            }

            var usuario = await _context.Usuarios
                .Where(u => u.Id == idUsuario)
                .Select(u => new { u.Estado, u.TipoId, u.HorarioEntrada, u.HorarioSalida })
                .FirstOrDefaultAsync();
            if (usuario == null) return null;

            DateTime ahoraLima = EstadoTurno.AhoraLima();
            DateOnly hoy = DateOnly.FromDateTime(ahoraLima);
            DateTime desdeUtc = hoy.AddDays(-1).ToDateTime(TimeOnly.MinValue) - EstadoTurno.DesfaseLima;
            var dias = (await _context.Asistencias
                    .Where(a => a.CreadorId == idUsuario && a.Tipo != null && a.Tipo.ToLower() == "entrada" && a.FechaCreacion >= desdeUtc)
                    .Select(a => a.FechaCreacion)
                    .ToListAsync())
                .Select(EstadoTurno.DiaLima)
                .ToHashSet();

            string estado = EstadoTurno.Calcular(usuario.Estado, usuario.TipoId, usuario.HorarioEntrada, usuario.HorarioSalida, dias, ahoraLima);
            if (estado == EstadoTurno.Activo) return null;

            if (estado == EstadoTurno.Baja)
                return "Tu usuario está dado de BAJA: no puedes registrar ni modificar datos.";
            return EstadoTurno.Motivo(usuario.Estado, usuario.TipoId, usuario.HorarioEntrada, usuario.HorarioSalida, dias, ahoraLima) switch
            {
                EstadoTurno.MotivoSinHorario => "Tu usuario no tiene horario cargado y figura INACTIVO: pide al administrador que registre tu horario.",
                EstadoTurno.MotivoSinEntrada => "Estás INACTIVO: marca tu ENTRADA en Mi asistencia para poder registrar o modificar datos.",
                _ => "Estás INACTIVO: estás fuera de tu horario de trabajo, no puedes registrar ni modificar datos."
            };
        }
    }
}

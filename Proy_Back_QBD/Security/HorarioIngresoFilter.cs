using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;

namespace Proy_back_QBD.Security
{
    /// <summary>
    /// Cierra la sesión de quien ya está dentro cuando sale de su horario de ingreso
    /// (entrada − 30 min a salida + 30 min). El administrador no tiene restricción.
    ///
    /// Revisa cada petición con sesión (también las consultas) y responde 401 con
    /// codigo FUERA_DE_HORARIO_INGRESO: el frontend entonces cierra la sesión y vuelve al login.
    /// El horario de cada usuario se guarda unos minutos en memoria para no ir a la base en cada petición.
    /// </summary>
    public sealed class HorarioIngresoFilter : IAsyncActionFilter
    {
        public const string Codigo = "FUERA_DE_HORARIO_INGRESO";
        private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(5);

        private readonly ApiContext _context;
        private readonly IMemoryCache _cache;

        public HorarioIngresoFilter(ApiContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private sealed record Horario(int? TipoId, TimeOnly? Entrada, TimeOnly? Salida);

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            int idUsuario = JwtTokens.UsuarioId(user);
            if (idUsuario > 0 && JwtTokens.Rol(user) != Roles.Admin)
            {
                Horario? h = await _cache.GetOrCreateAsync($"horario-ingreso:{idUsuario}", async entrada =>
                {
                    entrada.AbsoluteExpirationRelativeToNow = DuracionCache;
                    return await _context.Usuarios
                        .Where(u => u.Id == idUsuario)
                        .Select(u => new Horario(u.TipoId, u.HorarioEntrada, u.HorarioSalida))
                        .FirstOrDefaultAsync();
                });

                if (h != null && !EstadoTurno.PuedeIngresar(h.TipoId, h.Entrada, h.Salida, EstadoTurno.AhoraLima()))
                {
                    string mensaje = h.Entrada == null || h.Salida == null
                        ? "Tu usuario no tiene horario cargado: se cerró tu sesión."
                        : $"Terminó tu horario de ingreso ({h.Entrada.Value.Add(-EstadoTurno.Margen):HH\\:mm} a {h.Salida.Value.Add(EstadoTurno.Margen):HH\\:mm}): se cerró tu sesión.";
                    context.Result = new ObjectResult(new { codigo = Codigo, message = mensaje })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                    return;
                }
            }
            await next();
        }
    }
}

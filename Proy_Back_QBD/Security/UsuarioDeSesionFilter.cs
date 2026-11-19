using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Proy_back_QBD.Security
{
    /// <summary>
    /// "Quién lo hizo" sale SIEMPRE de la sesión (token), no de lo que mande el front.
    ///
    /// En cada petición con sesión, antes de llegar al controlador, recorre los datos recibidos
    /// (cuerpo y parámetros, incluidas las listas internas) y pone el usuario del token en:
    ///   - ModificadorId / IdModificador: en cualquier petición que no sea GET.
    ///   - CreadorId / IdCreador: solo al crear (POST), para no cambiar el creador al editar.
    /// Sin sesión (sin token válido) no toca nada.
    ///
    /// Motivo: el front caía en el usuario 1 cuando no encontraba la sesión y los registros
    /// quedaban a nombre de otra persona.
    /// </summary>
    public sealed class UsuarioDeSesionFilter : IActionFilter
    {
        private static readonly HashSet<string> CamposCreador = new(StringComparer.OrdinalIgnoreCase) { "CreadorId", "IdCreador" };
        private static readonly HashSet<string> CamposModificador = new(StringComparer.OrdinalIgnoreCase) { "ModificadorId", "IdModificador" };
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CacheProps = new();
        private const int ProfundidadMaxima = 5;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var metodo = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo)) return;

            int idUsuario = JwtTokens.UsuarioId(context.HttpContext.User);
            if (idUsuario <= 0) return;

            bool esCreacion = HttpMethods.IsPost(metodo);
            bool Aplica(string nombre) => CamposModificador.Contains(nombre) || (esCreacion && CamposCreador.Contains(nombre));

            // Parámetros sueltos (p. ej. [FromQuery] int idCreador)
            foreach (var nombre in context.ActionArguments.Keys.ToList())
            {
                var valor = context.ActionArguments[nombre];
                if (Aplica(nombre) && (valor is int || valor is long || valor is null))
                {
                    var param = context.ActionDescriptor.Parameters.FirstOrDefault(p => string.Equals(p.Name, nombre, StringComparison.OrdinalIgnoreCase));
                    var tipo = Nullable.GetUnderlyingType(param?.ParameterType ?? typeof(int)) ?? param?.ParameterType ?? typeof(int);
                    if (tipo == typeof(int)) context.ActionArguments[nombre] = idUsuario;
                    else if (tipo == typeof(long)) context.ActionArguments[nombre] = (long)idUsuario;
                    continue;
                }
                if (valor != null) Recorrer(valor, idUsuario, Aplica, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        private static void Recorrer(object obj, int idUsuario, Func<string, bool> aplica, HashSet<object> vistos, int profundidad)
        {
            if (profundidad > ProfundidadMaxima || !vistos.Add(obj)) return;

            if (obj is IEnumerable lista && obj is not string)
            {
                foreach (var item in lista)
                    if (item != null && EsTipoPropio(item.GetType())) Recorrer(item, idUsuario, aplica, vistos, profundidad + 1);
                return;
            }

            var tipo = obj.GetType();
            if (!EsTipoPropio(tipo)) return;

            foreach (var prop in CacheProps.GetOrAdd(tipo, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.CanRead && p.GetIndexParameters().Length == 0).ToArray()))
            {
                var tipoProp = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (aplica(prop.Name) && prop.CanWrite)
                {
                    if (tipoProp == typeof(int)) prop.SetValue(obj, idUsuario);
                    else if (tipoProp == typeof(long)) prop.SetValue(obj, (long)idUsuario);
                    continue;
                }

                if (tipoProp.IsPrimitive || tipoProp.IsEnum || tipoProp == typeof(string) || tipoProp == typeof(decimal) ||
                    tipoProp == typeof(DateTime) || tipoProp == typeof(DateOnly) || tipoProp == typeof(TimeOnly) || tipoProp == typeof(Guid))
                    continue;

                object? hijo;
                try { hijo = prop.GetValue(obj); } catch { continue; }
                if (hijo != null) Recorrer(hijo, idUsuario, aplica, vistos, profundidad + 1);
            }
        }

        /// <summary>Solo se recorren las clases del propio proyecto (DTOs y modelos), no las de .NET.</summary>
        private static bool EsTipoPropio(Type t) =>
            t.Assembly == typeof(UsuarioDeSesionFilter).Assembly && !t.IsPrimitive && !t.IsEnum;
    }
}

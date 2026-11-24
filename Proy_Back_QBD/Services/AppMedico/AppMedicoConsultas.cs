using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services.AppMedico
{
    /// <summary>Fila del directorio de médicos (una por sede).</summary>
    public sealed record FilaMedicoCmp(int Id, string? Cmp, int SedeId, string? Nombre, string? Especialidad);

    /// <summary>
    /// Consultas de la app de médicos separadas del servicio para poder revisar en pruebas
    /// que EF las traduce a SQL de PostgreSQL (ToQueryString).
    /// </summary>
    public static class AppMedicoConsultas
    {
        /// <summary>
        /// Candidatos por CMP. Filtra con EndsWith (el CMP puede tener ceros a la izquierda);
        /// la comparación exacta normalizada se hace después en memoria.
        /// </summary>
        public static IQueryable<FilaMedicoCmp> FilasDelCmp(ApiContext ctx, string cmpNormal) =>
            ctx.Medicos.AsNoTracking()
                .Where(m => m.Cmp != null && m.Cmp.EndsWith(cmpNormal))
                .Select(m => new FilaMedicoCmp(m.Id ?? 0, m.Cmp, m.SedeId,
                    m.Persona != null ? m.Persona.NombreCompleto : null,
                    m.Especialidad != null ? m.Especialidad.Nombre : null));

        /// <summary>Catálogo activo con los filtros de la app. <paramref name="soloIds"/> limita a esos productos (favoritos).</summary>
        public static IQueryable<ListaPrecio> Catalogo(ApiContext ctx, string? search, string? especialidad, string? categoria,
            IReadOnlyCollection<int>? soloIds)
        {
            var query = ctx.ListaPrecios.AsNoTracking().Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                var cat = categoria.Trim().ToUpper();
                query = query.Where(p => p.Categoria == cat);
            }
            if (!string.IsNullOrWhiteSpace(especialidad))
            {
                var esp = especialidad.Trim().ToUpper();
                query = query.Where(p => p.Especialidad1 == esp || p.Especialidad2 == esp || p.Especialidad3 == esp);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpper();
                query = query.Where(p =>
                    p.Producto.ToUpper().Contains(term) ||
                    (p.Ifas != null && p.Ifas.ToUpper().Contains(term)) ||
                    (p.Indicaciones != null && p.Indicaciones.ToUpper().Contains(term)) ||
                    (p.FormaFarmaceutica != null && p.FormaFarmaceutica.ToUpper().Contains(term)) ||
                    (p.Enfermedades != null && p.Enfermedades.ToUpper().Contains(term)) ||
                    (p.CodigoA != null && p.CodigoA.ToUpper().Contains(term)));
            }
            if (soloIds != null)
            {
                var ids = soloIds.ToList();
                query = query.Where(p => ids.Contains(p.Id));
            }
            return query;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Controllers
{
    /// <summary>Lista de precios (fórmulas magistrales, veterinarias, catálogo QbD, etc.).</summary>
    [ApiController]
    [Route("api/lista-precios")]
    public class ListaPrecioController : ControllerBase
    {
        private readonly ApiContext _context;

        public ListaPrecioController(ApiContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista los precios activos. Filtros opcionales: texto (código o nombre) y categoría.
        /// Sin filtros devuelve toda la lista (≈1,700 registros).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? search, [FromQuery] string? categoria, [FromQuery] int? take)
        {
            var query = _context.ListaPrecios.AsNoTracking().Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                var cat = categoria.Trim().ToUpper();
                query = query.Where(p => p.Categoria != null && p.Categoria.ToUpper() == cat);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpper();
                query = query.Where(p => p.Nombre.ToUpper().Contains(term) || (p.Codigo != null && p.Codigo.ToUpper().Contains(term)));
            }

            query = query.OrderBy(p => p.Nombre);
            if (take.HasValue && take.Value > 0)
            {
                query = query.Take(take.Value);
            }

            var lista = await query
                .Select(p => new
                {
                    p.Id,
                    p.Codigo,
                    p.Nombre,
                    p.Categoria,
                    p.Precio,
                    p.PrecioFraccion,
                    p.Clase
                })
                .ToListAsync();

            return Ok(lista);
        }

        /// <summary>Categorías disponibles con su cantidad de registros.</summary>
        [HttpGet("categorias")]
        public async Task<IActionResult> Categorias()
        {
            var cats = await _context.ListaPrecios.AsNoTracking()
                .Where(p => p.Activo && p.Categoria != null)
                .GroupBy(p => p.Categoria!)
                .Select(g => new { Categoria = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToListAsync();
            return Ok(cats);
        }
    }
}

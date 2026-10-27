using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Proy_back_QBD.Security;

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

        /// <summary>Crea un precio nuevo (solo ADMIN).</summary>
        [HttpPost]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Crear([FromBody] ListaPrecioReq request)
        {
            var error = Validar(request);
            if (error != null) return BadRequest(new { message = error });

            var nuevo = new ListaPrecio
            {
                Codigo = Limpiar(request.Codigo),
                Nombre = request.Nombre!.Trim().ToUpper(),
                Categoria = Limpiar(request.Categoria) ?? "FORMULA MAGISTRAL",
                Precio = request.Precio,
                PrecioFraccion = request.PrecioFraccion ?? 0,
                Clase = "FORMULA MAGISTRAL",
                Origen = "MANUAL",
                Activo = true
            };
            _context.ListaPrecios.Add(nuevo);
            await _context.SaveChangesAsync();
            return Ok(nuevo);
        }

        /// <summary>Modifica un precio (solo ADMIN).</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ListaPrecioReq request)
        {
            var error = Validar(request);
            if (error != null) return BadRequest(new { message = error });

            var item = await _context.ListaPrecios.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
            if (item == null) return NotFound(new { message = "Precio no encontrado." });

            item.Codigo = Limpiar(request.Codigo);
            item.Nombre = request.Nombre!.Trim().ToUpper();
            item.Categoria = Limpiar(request.Categoria) ?? item.Categoria;
            item.Precio = request.Precio;
            if (request.PrecioFraccion.HasValue) item.PrecioFraccion = request.PrecioFraccion.Value;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        /// <summary>Quita un precio de la lista (se desactiva, no se borra). Solo ADMIN.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var item = await _context.ListaPrecios.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
            if (item == null) return NotFound(new { message = "Precio no encontrado." });
            item.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Precio eliminado." });
        }

        private static string? Validar(ListaPrecioReq? r)
        {
            if (r == null) return "Datos incompletos.";
            if (string.IsNullOrWhiteSpace(r.Nombre)) return "Ingrese la descripción.";
            if (r.Precio < 0) return "El precio no puede ser negativo.";
            return null;
        }

        private static string? Limpiar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpper() == "CATALOGO QBD" ? "CATALOGO QbD" : valor.Trim().ToUpper();
    }

    /// <summary>Datos para crear o modificar un precio.</summary>
    public class ListaPrecioReq
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Categoria { get; set; }
        public decimal Precio { get; set; }
        public decimal? PrecioFraccion { get; set; }
    }
}

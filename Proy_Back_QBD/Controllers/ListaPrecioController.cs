using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Proy_back_QBD.Security;

namespace Proy_back_QBD.Controllers
{
    /// <summary>
    /// Lista de precios (fórmulas magistrales, veterinarias, catálogo QbD, etc.).
    /// Cada producto tiene un precio GENERAL y cada sede puede tener su PRECIO PROPIO
    /// (tabla lista_precios_sede). Si la sede no tiene precio propio, se usa el general.
    /// </summary>
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
        /// Lista los precios activos. Con sedeId devuelve el precio de esa sede (o el general si no tiene).
        /// Filtros opcionales: texto (código o nombre) y categoría.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int? sedeId, [FromQuery] string? search, [FromQuery] string? categoria, [FromQuery] int? take)
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
                .Select(p => new { p.Id, p.Codigo, p.Nombre, p.Categoria, p.Precio, p.PrecioFraccion, p.Clase })
                .ToListAsync();

            // Precios propios de la sede
            var preciosSede = new Dictionary<int, decimal>();
            if (sedeId.HasValue && sedeId.Value > 0)
            {
                preciosSede = await _context.ListaPreciosSede.AsNoTracking()
                    .Where(s => s.IdSede == sedeId.Value)
                    .ToDictionaryAsync(s => s.IdListaPrecio, s => s.Precio);
            }

            var resultado = lista.Select(p =>
            {
                var tienePropio = preciosSede.TryGetValue(p.Id, out var precioSede);
                return new
                {
                    p.Id,
                    p.Codigo,
                    p.Nombre,
                    p.Categoria,
                    Precio = tienePropio ? precioSede : p.Precio,
                    PrecioGeneral = p.Precio,
                    PrecioSede = tienePropio,
                    p.PrecioFraccion,
                    p.Clase
                };
            });

            return Ok(resultado);
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

        /// <summary>Crea un producto nuevo. Su precio queda como precio general (solo ADMIN).</summary>
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

        /// <summary>
        /// Modifica un producto (solo ADMIN). Código, descripción y categoría son de todas las sedes.
        /// El precio:
        ///  - con IdSede y sin AplicarATodas → precio propio solo de esa sede;
        ///  - con AplicarATodas (o sin IdSede) → precio general y se quitan los precios propios de las sedes.
        /// </summary>
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
            if (request.PrecioFraccion.HasValue) item.PrecioFraccion = request.PrecioFraccion.Value;

            bool soloSede = request.IdSede.HasValue && request.IdSede.Value > 0 && !request.AplicarATodas;
            if (soloSede)
            {
                var propio = await _context.ListaPreciosSede
                    .FirstOrDefaultAsync(s => s.IdListaPrecio == id && s.IdSede == request.IdSede!.Value);
                if (propio == null)
                {
                    _context.ListaPreciosSede.Add(new ListaPrecioSede
                    {
                        IdListaPrecio = id,
                        IdSede = request.IdSede!.Value,
                        Precio = request.Precio,
                        FechaModificacion = DateTime.UtcNow
                    });
                }
                else
                {
                    propio.Precio = request.Precio;
                    propio.FechaModificacion = DateTime.UtcNow;
                }
            }
            else
            {
                item.Precio = request.Precio;
                var propios = await _context.ListaPreciosSede.Where(s => s.IdListaPrecio == id).ToListAsync();
                _context.ListaPreciosSede.RemoveRange(propios);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = soloSede ? "Precio de la sede actualizado." : "Precio general actualizado para todas las sedes." });
        }

        /// <summary>Quita el precio propio de una sede: vuelve a usar el precio general (solo ADMIN).</summary>
        [HttpDelete("{id:int}/sede/{sedeId:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> QuitarPrecioSede(int id, int sedeId)
        {
            var propio = await _context.ListaPreciosSede.FirstOrDefaultAsync(s => s.IdListaPrecio == id && s.IdSede == sedeId);
            if (propio == null) return NotFound(new { message = "La sede no tiene precio propio para este producto." });
            _context.ListaPreciosSede.Remove(propio);
            await _context.SaveChangesAsync();
            return Ok(new { message = "La sede vuelve a usar el precio general." });
        }

        /// <summary>Quita un producto de la lista (se desactiva, no se borra). Solo ADMIN.</summary>
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

        /// <summary>Sede a la que se aplica el precio (si no se marca AplicarATodas).</summary>
        public int? IdSede { get; set; }

        /// <summary>true = el precio pasa a ser el general de todas las sedes.</summary>
        public bool AplicarATodas { get; set; }
    }
}

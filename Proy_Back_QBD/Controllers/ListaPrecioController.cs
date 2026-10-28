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
                query = query.Where(p => p.Nombre.ToUpper().Contains(term)
                    || (p.Codigo != null && p.Codigo.ToUpper().Contains(term))
                    || (p.Enfermedad != null && p.Enfermedad.ToUpper().Contains(term)));
            }

            query = query.OrderBy(p => p.Nombre);
            if (take.HasValue && take.Value > 0)
            {
                query = query.Take(take.Value);
            }

            var lista = await query
                .Select(p => new { p.Id, p.Codigo, p.Nombre, p.Categoria, p.Enfermedad, p.Precio, p.PrecioFraccion, p.Clase })
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
                    p.Enfermedad,
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
                Enfermedad = Limpiar(request.Enfermedad),
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
            item.Enfermedad = Limpiar(request.Enfermedad);
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

        /// <summary>
        /// Carga masiva desde Excel (solo ADMIN). Cada fila se busca por ID; si no tiene ID, por CÓDIGO;
        /// si no existe, se crea. El precio va a la sede indicada o, con AplicarATodas, al precio general.
        /// </summary>
        [HttpPost("importar")]
        [Authorize(Roles = Roles.SoloAdmin)]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Importar([FromBody] ListaPrecioImportReq request)
        {
            if (request?.Filas == null || request.Filas.Count == 0)
                return BadRequest(new { message = "El archivo no tiene filas para importar." });

            bool soloSede = request.IdSede.HasValue && request.IdSede.Value > 0 && !request.AplicarATodas;

            var todos = await _context.ListaPrecios.Where(p => p.Activo).ToListAsync();
            var porId = todos.ToDictionary(p => p.Id);
            var porCodigo = todos.Where(p => !string.IsNullOrWhiteSpace(p.Codigo))
                .GroupBy(p => p.Codigo!.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.First());

            var propiosSede = soloSede
                ? await _context.ListaPreciosSede.Where(s => s.IdSede == request.IdSede!.Value).ToDictionaryAsync(s => s.IdListaPrecio)
                : new Dictionary<int, ListaPrecioSede>();

            int creados = 0, actualizados = 0, sinCambios = 0;
            var errores = new List<string>();
            var nuevosConPrecioSede = new List<(ListaPrecio Item, decimal Precio)>();

            for (int i = 0; i < request.Filas.Count; i++)
            {
                var f = request.Filas[i];
                int filaExcel = f.Fila ?? (i + 2);
                var nombre = (f.Nombre ?? "").Trim().ToUpper();
                var codigo = Limpiar(f.Codigo);

                if (f.Precio == null || f.Precio < 0)
                {
                    errores.Add($"Fila {filaExcel}: precio vacío o inválido.");
                    continue;
                }

                ListaPrecio? item = null;
                if (f.Id.HasValue && f.Id.Value > 0) porId.TryGetValue(f.Id.Value, out item);
                if (item == null && codigo != null) porCodigo.TryGetValue(codigo, out item);

                if (item == null)
                {
                    if (string.IsNullOrWhiteSpace(nombre))
                    {
                        errores.Add($"Fila {filaExcel}: producto nuevo sin descripción.");
                        continue;
                    }
                    item = new ListaPrecio
                    {
                        Codigo = codigo,
                        Nombre = nombre,
                        Categoria = Limpiar(f.Categoria) ?? "FORMULA MAGISTRAL",
                        Enfermedad = Limpiar(f.Enfermedad),
                        Precio = f.Precio.Value,
                        Clase = "FORMULA MAGISTRAL",
                        Origen = "EXCEL",
                        Activo = true
                    };
                    _context.ListaPrecios.Add(item);
                    if (codigo != null) porCodigo[codigo] = item;
                    creados++;
                    continue;
                }

                bool cambio = false;
                if (!string.IsNullOrWhiteSpace(nombre) && item.Nombre != nombre) { item.Nombre = nombre; cambio = true; }
                if (codigo != null && item.Codigo != codigo) { item.Codigo = codigo; cambio = true; }
                var cat = Limpiar(f.Categoria);
                if (cat != null && item.Categoria != cat) { item.Categoria = cat; cambio = true; }
                if (f.Enfermedad != null)
                {
                    var enf = Limpiar(f.Enfermedad);
                    if (item.Enfermedad != enf) { item.Enfermedad = enf; cambio = true; }
                }

                var precio = f.Precio.Value;
                if (soloSede)
                {
                    if (propiosSede.TryGetValue(item.Id, out var propio))
                    {
                        if (propio.Precio != precio) { propio.Precio = precio; propio.FechaModificacion = DateTime.UtcNow; cambio = true; }
                    }
                    else if (item.Precio != precio)
                    {
                        var nuevo = new ListaPrecioSede { IdListaPrecio = item.Id, IdSede = request.IdSede!.Value, Precio = precio, FechaModificacion = DateTime.UtcNow };
                        _context.ListaPreciosSede.Add(nuevo);
                        propiosSede[item.Id] = nuevo;
                        cambio = true;
                    }
                }
                else if (item.Precio != precio)
                {
                    item.Precio = precio;
                    cambio = true;
                }

                if (cambio) actualizados++; else sinCambios++;
            }

            // Con "aplicar a todas", los precios importados pasan a ser generales: se quitan los propios de esos productos
            if (!soloSede)
            {
                var ids = request.Filas.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToList();
                if (ids.Count > 0)
                {
                    var propios = await _context.ListaPreciosSede.Where(s => ids.Contains(s.IdListaPrecio)).ToListAsync();
                    _context.ListaPreciosSede.RemoveRange(propios);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                creados,
                actualizados,
                sinCambios,
                errores,
                message = $"Importación lista: {creados} nuevos, {actualizados} actualizados, {sinCambios} sin cambios" + (errores.Count > 0 ? $", {errores.Count} con error." : ".")
            });
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

    /// <summary>Fila del Excel de importación.</summary>
    public class ListaPrecioImportFila
    {
        public int? Fila { get; set; }
        public int? Id { get; set; }
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Categoria { get; set; }

        /// <summary>null = la columna no vino en el Excel (no se toca); "" = se borra.</summary>
        public string? Enfermedad { get; set; }
        public decimal? Precio { get; set; }
    }

    /// <summary>Importación masiva desde Excel.</summary>
    public class ListaPrecioImportReq
    {
        public List<ListaPrecioImportFila> Filas { get; set; } = new();
        public int? IdSede { get; set; }
        public bool AplicarATodas { get; set; }
    }

    /// <summary>Datos para crear o modificar un precio.</summary>
    public class ListaPrecioReq
    {
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Categoria { get; set; }
        public string? Enfermedad { get; set; }
        public decimal Precio { get; set; }
        public decimal? PrecioFraccion { get; set; }

        /// <summary>Sede a la que se aplica el precio (si no se marca AplicarATodas).</summary>
        public int? IdSede { get; set; }

        /// <summary>true = el precio pasa a ser el general de todas las sedes.</summary>
        public bool AplicarATodas { get; set; }
    }
}

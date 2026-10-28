using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Proy_back_QBD.Security;

namespace Proy_back_QBD.Controllers
{
    /// <summary>
    /// Lista de precios (Excel PREC. AV) en UNA SOLA TABLA.
    /// Cada producto tiene 4 listas, cada una con su código y su precio:
    /// S (precio P), A (MED), B (TERC) y C (PUB). El mismo precio vale para todas las sedes.
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
        /// Lista los productos activos. Filtros opcionales: texto (código de cualquier lista, producto,
        /// IFAS o indicaciones) y especialidad.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? search, [FromQuery] string? especialidad, [FromQuery] int? take)
        {
            var query = _context.ListaPrecios.AsNoTracking().Where(p => p.Activo);

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
                    (p.CodigoS != null && p.CodigoS.Contains(term)) ||
                    (p.CodigoA != null && p.CodigoA.Contains(term)) ||
                    (p.CodigoB != null && p.CodigoB.Contains(term)) ||
                    (p.CodigoC != null && p.CodigoC.Contains(term)));
            }

            query = query.OrderBy(p => p.Producto);
            if (take.HasValue && take.Value > 0)
            {
                query = query.Take(take.Value);
            }

            try
            {
                return Ok(await query.ToListAsync());
            }
            catch (Exception ex)
            {
                var detalle = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"[LISTA-PRECIOS] Error al listar: {detalle}");
                return StatusCode(500, new { message = $"No se pudo leer la lista de precios: {detalle}" });
            }
        }

        /// <summary>Especialidades usadas en la lista, con su cantidad de productos.</summary>
        [HttpGet("especialidades")]
        public async Task<IActionResult> Especialidades()
        {
            var filas = await _context.ListaPrecios.AsNoTracking()
                .Where(p => p.Activo)
                .Select(p => new { p.Especialidad1, p.Especialidad2, p.Especialidad3 })
                .ToListAsync();

            var conteo = filas
                .SelectMany(f => new[] { f.Especialidad1, f.Especialidad2, f.Especialidad3 })
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .GroupBy(e => e!)
                .Select(g => new { Especialidad = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToList();
            return Ok(conteo);
        }

        /// <summary>Crea un producto (solo ADMIN).</summary>
        [HttpPost]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Crear([FromBody] ListaPrecioReq request)
        {
            var error = Validar(request);
            if (error != null) return BadRequest(new { message = error });

            var nuevo = new ListaPrecio { FechaIngreso = DateTime.Now, Activo = true };
            Aplicar(nuevo, request);
            _context.ListaPrecios.Add(nuevo);
            await _context.SaveChangesAsync();
            return Ok(nuevo);
        }

        /// <summary>Modifica un producto (solo ADMIN).</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ListaPrecioReq request)
        {
            var error = Validar(request);
            if (error != null) return BadRequest(new { message = error });

            var item = await _context.ListaPrecios.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
            if (item == null) return NotFound(new { message = "Producto no encontrado." });

            Aplicar(item, request);
            item.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        /// <summary>Quita un producto de la lista (se desactiva, no se borra). Solo ADMIN.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var item = await _context.ListaPrecios.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
            if (item == null) return NotFound(new { message = "Producto no encontrado." });
            item.Activo = false;
            item.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Producto eliminado." });
        }

        /// <summary>
        /// Importación masiva desde Excel (solo ADMIN). Cada fila se busca por Id; si no tiene Id,
        /// por el código de la lista A. Si no existe, se crea.
        /// </summary>
        [HttpPost("importar")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Importar([FromBody] ListaPrecioImportReq request)
        {
            if (request?.Filas == null || request.Filas.Count == 0)
                return BadRequest(new { message = "El archivo no tiene filas." });

            var existentes = await _context.ListaPrecios.Where(p => p.Activo).ToListAsync();
            var porId = existentes.ToDictionary(p => p.Id);
            var porCodigoA = existentes.Where(p => !string.IsNullOrWhiteSpace(p.CodigoA))
                .GroupBy(p => p.CodigoA!.Trim())
                .ToDictionary(g => g.Key, g => g.First());

            int creados = 0, actualizados = 0, sinCambios = 0;
            var errores = new List<string>();

            for (int i = 0; i < request.Filas.Count; i++)
            {
                var f = request.Filas[i];
                int filaExcel = f.Fila ?? (i + 2);

                if (new[] { f.PrecioS, f.PrecioA, f.PrecioB, f.PrecioC }.Any(p => p.HasValue && p.Value < 0))
                {
                    errores.Add($"Fila {filaExcel}: hay un precio negativo.");
                    continue;
                }

                ListaPrecio? item = null;
                if (f.Id.HasValue && f.Id.Value > 0) porId.TryGetValue(f.Id.Value, out item);
                var codA = Texto(f.CodigoA);
                if (item == null && codA != null) porCodigoA.TryGetValue(codA, out item);

                if (item == null)
                {
                    if (string.IsNullOrWhiteSpace(f.Producto))
                    {
                        errores.Add($"Fila {filaExcel}: producto nuevo sin nombre.");
                        continue;
                    }
                    item = new ListaPrecio { FechaIngreso = DateTime.Now, Activo = true };
                    Aplicar(item, f);
                    _context.ListaPrecios.Add(item);
                    if (codA != null) porCodigoA[codA] = item;
                    creados++;
                    continue;
                }

                var antes = Firma(item);
                if (string.IsNullOrWhiteSpace(f.Producto)) f.Producto = item.Producto;
                Aplicar(item, f);
                if (Firma(item) != antes)
                {
                    item.FechaModificacion = DateTime.Now;
                    actualizados++;
                }
                else
                {
                    sinCambios++;
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

        // ===================== Ayudas =====================

        private static void Aplicar(ListaPrecio item, ListaPrecioReq r)
        {
            item.CodigoS = Texto(r.CodigoS);
            item.CodigoA = Texto(r.CodigoA);
            item.CodigoB = Texto(r.CodigoB);
            item.CodigoC = Texto(r.CodigoC);
            item.Producto = (r.Producto ?? "").Trim().ToUpper();
            item.Ifas = Texto(r.Ifas);
            item.PrecioS = r.PrecioS;
            item.PrecioA = r.PrecioA;
            item.PrecioB = r.PrecioB;
            item.PrecioC = r.PrecioC;
            item.Especialidad1 = Mayus(r.Especialidad1);
            item.Especialidad2 = Mayus(r.Especialidad2);
            item.Especialidad3 = Mayus(r.Especialidad3);
            item.Indicaciones = Texto(r.Indicaciones);
            item.Presentacion = Mayus(r.Presentacion);
            item.FormaFarmaceutica = Mayus(r.FormaFarmaceutica);
            item.Enfermedades = Mayus(r.Enfermedades);
        }

        private static string Firma(ListaPrecio p) => string.Join("|", new object?[]
        {
            p.CodigoS, p.CodigoA, p.CodigoB, p.CodigoC, p.Producto, p.Ifas,
            N(p.PrecioS), N(p.PrecioA), N(p.PrecioB), N(p.PrecioC),
            p.Especialidad1, p.Especialidad2, p.Especialidad3, p.Indicaciones, p.Presentacion, p.FormaFarmaceutica, p.Enfermedades
        });

        private static string? N(decimal? v) => v?.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

        private static string? Validar(ListaPrecioReq? r)
        {
            if (r == null) return "Datos incompletos.";
            if (string.IsNullOrWhiteSpace(r.Producto)) return "Ingrese el nombre del producto.";
            if (new[] { r.PrecioS, r.PrecioA, r.PrecioB, r.PrecioC }.Any(p => p.HasValue && p.Value < 0))
                return "Los precios no pueden ser negativos.";
            return null;
        }

        private static string? Texto(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        private static string? Mayus(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpper();
    }

    /// <summary>Datos de un producto de la lista de precios.</summary>
    public class ListaPrecioReq
    {
        public string? CodigoS { get; set; }
        public string? CodigoA { get; set; }
        public string? CodigoB { get; set; }
        public string? CodigoC { get; set; }
        public string? Producto { get; set; }
        public string? Ifas { get; set; }
        public decimal? PrecioS { get; set; }
        public decimal? PrecioA { get; set; }
        public decimal? PrecioB { get; set; }
        public decimal? PrecioC { get; set; }
        public string? Especialidad1 { get; set; }
        public string? Especialidad2 { get; set; }
        public string? Especialidad3 { get; set; }
        public string? Indicaciones { get; set; }
        public string? Presentacion { get; set; }
        public string? FormaFarmaceutica { get; set; }
        public string? Enfermedades { get; set; }
    }

    /// <summary>Fila del Excel de importación.</summary>
    public class ListaPrecioImportFila : ListaPrecioReq
    {
        public int? Fila { get; set; }
        public int? Id { get; set; }
    }

    /// <summary>Importación masiva desde Excel.</summary>
    public class ListaPrecioImportReq
    {
        public List<ListaPrecioImportFila> Filas { get; set; } = new();
    }
}

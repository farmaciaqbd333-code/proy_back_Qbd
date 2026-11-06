using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services
{
    /// <summary>
    /// Lista de precios (Excel PREC. AV) en una sola tabla.
    /// Cada producto tiene 4 listas, cada una con su código y su precio:
    /// S (precio P), A (MED), B (TERC) y C (PUB). El mismo precio vale para todas las sedes.
    /// </summary>
    public class ListaPrecioService : IListaPrecioService
    {
        private readonly ApiContext _context;

        public ListaPrecioService(ApiContext context)
        {
            _context = context;
        }

        public Task<List<ListaPrecio>> Listar(string? search, string? especialidad, int? take, string? categoria)
        {
            var query = _context.ListaPrecios.AsNoTracking().Where(p => p.Activo);

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

            return query.ToListAsync();
        }

        public async Task<List<EspecialidadConteoRes>> Especialidades()
        {
            var filas = await _context.ListaPrecios.AsNoTracking()
                .Where(p => p.Activo)
                .Select(p => new { p.Especialidad1, p.Especialidad2, p.Especialidad3 })
                .ToListAsync();

            return filas
                .SelectMany(f => new[] { f.Especialidad1, f.Especialidad2, f.Especialidad3 })
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .GroupBy(e => e!)
                .Select(g => new EspecialidadConteoRes(g.Key, g.Count()))
                .OrderByDescending(x => x.Total)
                .ToList();
        }

        public async Task<ListaPrecio> Crear(ListaPrecioReq request)
        {
            Validar(request);

            var nuevo = new ListaPrecio { FechaIngreso = DateTime.Now, Activo = true };
            Aplicar(nuevo, request);
            _context.ListaPrecios.Add(nuevo);
            await _context.SaveChangesAsync();
            return nuevo;
        }

        public async Task<ListaPrecio> Actualizar(int id, ListaPrecioReq request)
        {
            Validar(request);

            var item = await BuscarActivo(id);
            Aplicar(item, request);
            item.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return item;
        }

        /// <summary>Se desactiva, no se borra.</summary>
        public async Task Eliminar(int id)
        {
            var item = await BuscarActivo(id);
            item.Activo = false;
            item.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Cada fila se busca por Id; si no tiene Id, por el código de la lista A. Si no existe, se crea.
        /// </summary>
        public async Task<ListaPrecioImportRes> Importar(ListaPrecioImportReq request)
        {
            if (request?.Filas == null || request.Filas.Count == 0)
                throw new BadRequestException("El archivo no tiene filas.");

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

            var mensaje = $"Importación lista: {creados} nuevos, {actualizados} actualizados, {sinCambios} sin cambios"
                          + (errores.Count > 0 ? $", {errores.Count} con error." : ".");
            return new ListaPrecioImportRes(creados, actualizados, sinCambios, errores, mensaje);
        }

        // ===================== Ayudas =====================

        private async Task<ListaPrecio> BuscarActivo(int id) =>
            await _context.ListaPrecios.FirstOrDefaultAsync(p => p.Id == id && p.Activo)
            ?? throw new NotFoundException("Producto no encontrado.");

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
            item.Categoria = Mayus(r.Categoria) ?? (string.IsNullOrWhiteSpace(item.Categoria) ? "FM" : item.Categoria);
            item.ModoUso = Texto(r.ModoUso);
            item.Precauciones = Texto(r.Precauciones);
            item.Observaciones = Texto(r.Observaciones);
        }

        private static string Firma(ListaPrecio p) => string.Join("|", new object?[]
        {
            p.CodigoS, p.CodigoA, p.CodigoB, p.CodigoC, p.Producto, p.Ifas,
            N(p.PrecioS), N(p.PrecioA), N(p.PrecioB), N(p.PrecioC),
            p.Especialidad1, p.Especialidad2, p.Especialidad3, p.Indicaciones, p.Presentacion, p.FormaFarmaceutica, p.Enfermedades,
            p.ModoUso, p.Precauciones, p.Observaciones, p.Categoria
        });

        private static string? N(decimal? v) => v?.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

        private static void Validar(ListaPrecioReq? r)
        {
            if (r == null) throw new BadRequestException("Datos incompletos.");
            if (string.IsNullOrWhiteSpace(r.Producto)) throw new BadRequestException("Ingrese el nombre del producto.");
            if (new[] { r.PrecioS, r.PrecioA, r.PrecioB, r.PrecioC }.Any(p => p.HasValue && p.Value < 0))
                throw new BadRequestException("Los precios no pueden ser negativos.");
        }

        private static string? Texto(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

        private static string? Mayus(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToUpper();
    }
}

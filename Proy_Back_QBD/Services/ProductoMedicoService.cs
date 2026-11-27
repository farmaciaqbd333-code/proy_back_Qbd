using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Models.AppMedico;
using Proy_back_QBD.Services.AppMedico;

namespace Proy_back_QBD.Services
{
    /// <summary>
    /// Asignación de productos de la Lista de Precios a médicos (tabla medico_producto_asignado).
    /// El mismo producto puede tener varios médicos y el mismo médico varios productos.
    /// </summary>
    public interface IProductoMedicoService
    {
        /// <summary>Directorio completo de médicos (todas las sedes) para el selector.</summary>
        Task<List<MedicoAsignableRes>> MedicosAsignables();
        /// <summary>Todas las asignaciones (para mostrarlas en la tabla de la Lista de Precios).</summary>
        Task<List<ProductoMedicoRes>> Asignaciones();
        Task<List<ProductoMedicoRes>> DelProducto(int productoId);
        Task<List<ProductoMedicoRes>> Asignar(int productoId, IEnumerable<int> medicoIds, int usuarioId);
        Task<List<ProductoMedicoRes>> Quitar(int productoId, int medicoId);
    }

    public class ProductoMedicoService : IProductoMedicoService
    {
        private readonly ApiContext _context;

        public ProductoMedicoService(ApiContext context)
        {
            _context = context;
        }

        public async Task<List<MedicoAsignableRes>> MedicosAsignables()
        {
            var filas = await (
                from m in _context.Medicos.AsNoTracking()
                join s in _context.Sedes.AsNoTracking() on m.SedeId equals s.Id into sedes
                from s in sedes.DefaultIfEmpty()
                select new MedicoAsignableRes
                {
                    Id = m.Id ?? 0,
                    Nombre = m.Persona != null ? (m.Persona.NombreCompleto ?? "") : "",
                    Cmp = m.Cmp,
                    SedeId = m.SedeId ?? 0, // 0 = médico sin sede
                    Sede = s != null ? s.Nombre : null,
                    Especialidad = m.Especialidad != null ? m.Especialidad.Nombre : null,
                }).ToListAsync();

            foreach (var f in filas) f.Nombre = f.Nombre.Trim();
            return filas.Where(f => f.Id > 0).OrderBy(f => f.Nombre).ThenBy(f => f.Id).ToList();
        }

        public Task<List<ProductoMedicoRes>> Asignaciones() => Consultar(null);

        public async Task<List<ProductoMedicoRes>> DelProducto(int productoId)
        {
            await ProductoActivo(productoId);
            return await Consultar(productoId);
        }

        public async Task<List<ProductoMedicoRes>> Asignar(int productoId, IEnumerable<int> medicoIds, int usuarioId)
        {
            await ProductoActivo(productoId);
            var ids = (medicoIds ?? Enumerable.Empty<int>()).Where(i => i > 0).Distinct().ToList();
            if (ids.Count == 0) throw new BadRequestException("Elige al menos un médico.");

            var medicos = await _context.Medicos.AsNoTracking()
                .Where(m => m.Id != null && ids.Contains(m.Id.Value))
                .Select(m => new { Id = m.Id!.Value, m.Cmp })
                .ToListAsync();
            var faltan = ids.Except(medicos.Select(m => m.Id)).ToList();
            if (faltan.Count > 0)
                throw new NotFoundException($"No existe el médico #{string.Join(", #", faltan)}.");

            var set = _context.Set<MedicoProductoAsignado>();
            var actuales = await set.Where(a => a.ListaPrecioId == productoId).ToListAsync();
            var cmpsActuales = actuales.Select(a => a.Cmp).Where(c => !string.IsNullOrEmpty(c)).ToHashSet();
            var ahora = EstadoTurno.AhoraLima();

            foreach (var m in medicos)
            {
                var cmp = AppMedicoReglas.NormalizarCmp(m.Cmp);
                // Ya asignado: la misma fila, o el mismo médico (mismo CMP) registrado en otra sede.
                if (actuales.Any(a => a.MedicoId == m.Id)) continue;
                if (cmp.Length > 0 && cmpsActuales.Contains(cmp)) continue;

                var nueva = new MedicoProductoAsignado
                {
                    ListaPrecioId = productoId,
                    MedicoId = m.Id,
                    Cmp = cmp.Length > 0 ? cmp : null,
                    FechaCreacion = ahora,
                    CreadorId = usuarioId > 0 ? usuarioId : null,
                };
                set.Add(nueva);
                actuales.Add(nueva);
                if (cmp.Length > 0) cmpsActuales.Add(cmp);
            }

            await _context.SaveChangesAsync();
            return await Consultar(productoId);
        }

        public async Task<List<ProductoMedicoRes>> Quitar(int productoId, int medicoId)
        {
            var set = _context.Set<MedicoProductoAsignado>();
            var fila = await set.FirstOrDefaultAsync(a => a.ListaPrecioId == productoId && a.MedicoId == medicoId)
                ?? throw new NotFoundException("Ese médico no está asignado a este producto.");
            set.Remove(fila);
            await _context.SaveChangesAsync();
            return await Consultar(productoId);
        }

        private async Task ProductoActivo(int productoId)
        {
            if (!await _context.ListaPrecios.AnyAsync(p => p.Id == productoId && p.Activo))
                throw new NotFoundException("El producto no existe o ya no está en la lista.");
        }

        /// <summary>Asignaciones con el nombre y sede del médico (las de médicos borrados del directorio no salen).</summary>
        private async Task<List<ProductoMedicoRes>> Consultar(int? productoId)
        {
            var asignaciones = _context.Set<MedicoProductoAsignado>().AsNoTracking();
            if (productoId.HasValue) asignaciones = asignaciones.Where(a => a.ListaPrecioId == productoId.Value);

            var filas = await (
                from a in asignaciones
                join m in _context.Medicos.AsNoTracking() on (int?)a.MedicoId equals m.Id
                join s in _context.Sedes.AsNoTracking() on m.SedeId equals s.Id into sedes
                from s in sedes.DefaultIfEmpty()
                select new ProductoMedicoRes
                {
                    ProductoId = a.ListaPrecioId,
                    MedicoId = a.MedicoId,
                    Nombre = m.Persona != null ? (m.Persona.NombreCompleto ?? "") : "",
                    Cmp = m.Cmp,
                    Sede = s != null ? s.Nombre : null,
                    FechaCreacion = a.FechaCreacion,
                }).ToListAsync();

            foreach (var f in filas) f.Nombre = f.Nombre.Trim();
            return filas.OrderBy(f => f.ProductoId).ThenBy(f => f.Nombre).ToList();
        }
    }
}

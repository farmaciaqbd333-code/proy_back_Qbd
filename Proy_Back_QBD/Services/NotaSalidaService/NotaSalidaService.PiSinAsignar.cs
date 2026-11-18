using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Exceptions;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    /// <summary>
    /// Líneas de PI de notas de salida que no tienen su producto intermedio asignado.
    /// Al arrancar el backend se completan solas las que coinciden por lote con un solo PI;
    /// las que quedan aquí son ambiguas (o sin coincidencia) y se asignan a mano.
    /// </summary>
    public partial class NotaSalidaService
    {
        public async Task<List<LineaPiSinAsignarRes>> ObtenerLineasPiSinAsignar()
        {
            var lineas = await _context.NotaSalidaInsumos.AsNoTracking()
                .Where(n => n.IdCompraInsumo == null && n.IdProductoIntermedio == null)
                .Select(n => new
                {
                    n.Id,
                    n.IdNotaSalida,
                    n.Lote,
                    n.Cantidad,
                    n.Um,
                    IdSedeOrigen = n.NotaSalida != null ? n.NotaSalida.IdSedeOrigen : 0,
                    IdSedeDestino = n.NotaSalida != null ? (int?)n.NotaSalida.IdSedeDestino : null,
                    FechaSalida = n.NotaSalida != null ? (DateTime?)n.NotaSalida.FechaSalida : null
                })
                .ToListAsync();
            if (lineas.Count == 0) return new List<LineaPiSinAsignarRes>();

            var lotes = lineas
                .Select(l => (l.Lote ?? "").Trim().ToUpper())
                .Where(l => l.Length > 0)
                .Distinct()
                .ToList();

            var candidatos = await _context.ProductosIntermedios.AsNoTracking()
                .Where(p => p.Lote != null && lotes.Contains(p.Lote.Trim().ToUpper()))
                .Select(p => new PiCandidatoRes
                {
                    IdProductoIntermedio = p.Id,
                    IdInsumo = p.IdInsumo,
                    Descripcion = p.Insumo != null ? p.Insumo.Descripcion ?? "" : "",
                    Lote = p.Lote ?? "",
                    IdSede = p.IdSede,
                    Sede = p.Sede != null ? p.Sede.Nombre ?? "" : "",
                    FechaVencimiento = p.FechaVencimiento
                })
                .ToListAsync();

            var porLote = candidatos
                .GroupBy(c => c.Lote.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.IdProductoIntermedio).ToList());

            return lineas
                .Select(l =>
                {
                    var key = (l.Lote ?? "").Trim().ToUpper();
                    var lista = porLote.TryGetValue(key, out var c) ? c : new List<PiCandidatoRes>();
                    return new LineaPiSinAsignarRes
                    {
                        IdNotaSalidaInsumo = l.Id,
                        IdNotaSalida = l.IdNotaSalida,
                        Lote = l.Lote,
                        Cantidad = l.Cantidad,
                        Um = l.Um,
                        IdSedeOrigen = l.IdSedeOrigen,
                        IdSedeDestino = l.IdSedeDestino,
                        FechaSalida = l.FechaSalida,
                        Motivo = lista.Count == 0 ? "SIN_COINCIDENCIA" : "AMBIGUA",
                        Candidatos = lista
                    };
                })
                .OrderBy(l => l.Motivo)
                .ThenBy(l => l.IdNotaSalida)
                .ToList();
        }

        public async Task AsignarProductoIntermedio(int idNotaSalidaInsumo, int idProductoIntermedio)
        {
            var linea = await _context.NotaSalidaInsumos.FirstOrDefaultAsync(n => n.Id == idNotaSalidaInsumo)
                ?? throw new NotFoundException($"No existe la línea de nota de salida {idNotaSalidaInsumo}.");
            if (linea.IdCompraInsumo.HasValue)
                throw new BadRequestException("La línea es de materia prima, no de producto intermedio.");

            var pi = await _context.ProductosIntermedios.AsNoTracking()
                .Where(p => p.Id == idProductoIntermedio)
                .Select(p => new { p.Id, p.Lote })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException($"No existe el producto intermedio {idProductoIntermedio}.");

            linea.IdProductoIntermedio = pi.Id;
            if (string.IsNullOrWhiteSpace(linea.Lote)) linea.Lote = pi.Lote;
            await _context.SaveChangesAsync();
        }
    }
}

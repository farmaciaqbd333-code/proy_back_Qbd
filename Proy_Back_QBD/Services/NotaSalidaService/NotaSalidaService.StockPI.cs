using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using proy_back_Qbd.Models.ProductoIntermedio;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    /// <summary>
    /// Stock de un Producto Intermedio (registro PI) en una sede, calculado igual que el KARDEX:
    ///  1) Su propio stock (elaborado en esa sede): filas con IdProductoIntermedio = PI y sin nota de salida.
    ///  2) Lo recibido por Nota de Salida en esa sede con el mismo lote. Si varios registros PI del mismo
    ///     producto comparten lote, lo recibido se asigna a uno solo (el de menor Id), como en el kardex.
    /// Antes se sumaba cualquier fila con ese lote (de cualquier producto, registro o sede de destino),
    /// por eso un registro con 15 g aparecía con 194 g.
    /// </summary>
    public partial class NotaSalidaService
    {
        private static readonly string[] EstadosRecibidos = { "RECIBIDO", "RECEPCIONADO" };

        private async Task<List<StockInsumo>> ObtenerFilasStockPI(int idProductoIntermedio, int idSede)
        {
            var pi = await _context.ProductosIntermedios.AsNoTracking()
                .Where(p => p.Id == idProductoIntermedio)
                .Select(p => new { p.Id, p.IdInsumo, p.Lote })
                .FirstOrDefaultAsync();
            if (pi == null) return new List<StockInsumo>();

            // 1) Stock propio
            var filas = await _context.StockInsumos
                .Include(s => s.ProductoIntermedio)
                .Include(s => s.NotaSalidaInsumo)
                .Where(s => s.IdSede == idSede
                            && s.IdProductoIntermedio == pi.Id
                            && s.IdNotaSalidaInsumo == null)
                .ToListAsync();

            // 2) Recibido por Nota de Salida (mismo lote, mismo producto)
            var lote = (pi.Lote ?? "").Trim().ToUpper();
            if (lote.Length > 0)
            {
                // Registro PI "dueño" del lote para este producto (como el kardex)
                var idDueno = await _context.ProductosIntermedios.AsNoTracking()
                    .Where(p => p.IdInsumo == pi.IdInsumo && p.Lote != null && p.Lote.Trim().ToUpper() == lote)
                    .MinAsync(p => (int?)p.Id);

                if (idDueno == pi.Id)
                {
                    var recibidas = await _context.StockInsumos
                        .Include(s => s.ProductoIntermedio)
                        .Include(s => s.NotaSalidaInsumo)
                            .ThenInclude(n => n!.NotaSalida)
                        .Where(s => s.IdSede == idSede
                                    && s.IdNotaSalidaInsumo != null
                                    && s.NotaSalidaInsumo != null
                                    && s.NotaSalidaInsumo.IdCompraInsumo == null
                                    && s.NotaSalidaInsumo.Lote != null
                                    && s.NotaSalidaInsumo.Lote.Trim().ToUpper() == lote
                                    && s.NotaSalidaInsumo.NotaSalida != null
                                    && s.NotaSalidaInsumo.NotaSalida.IdSedeDestino == idSede
                                    && (EstadosRecibidos.Contains(s.NotaSalidaInsumo.NotaSalida.Estado)
                                        || s.NotaSalidaInsumo.NotaSalida.FechaRecepcion != null
                                        || (s.NotaSalidaInsumo.CantidadRecibida.HasValue && s.NotaSalidaInsumo.CantidadRecibida.Value > 0)))
                        .ToListAsync();

                    filas.AddRange(recibidas.Where(r => filas.All(f => f.Id != r.Id)));
                }
            }

            // Primero lo propio, luego lo recibido; dentro de cada grupo, mayor stock primero
            return filas
                .OrderBy(s => s.IdNotaSalidaInsumo != null ? 1 : 0)
                .ThenByDescending(s => s.StockDisponible)
                .ToList();
        }

        private async Task<decimal> ObtenerStockPI(int idProductoIntermedio, int idSede)
        {
            var filas = await ObtenerFilasStockPI(idProductoIntermedio, idSede);
            var total = filas.Sum(s => s.StockDisponible);

            // Diagnóstico temporal: muestra en la consola de la API de dónde sale el stock de cada registro PI
            _logger.LogWarning("[STOCK-PI v2] Registro PI{Reg} (id {Id}) sede {Sede}: total {Total} en {N} fila(s): {Detalle}",
                Alfanumerico.ConvertToBase36(idProductoIntermedio), idProductoIntermedio, idSede, total, filas.Count,
                string.Join(" | ", filas.Select(f =>
                    $"stock_insumo#{f.Id} stock={f.StockDisponible} um={f.UnidadMedida} tipo={f.Tipo} idPI={f.IdProductoIntermedio} idNSI={f.IdNotaSalidaInsumo} lote={f.NotaSalidaInsumo?.Lote}")));

            // Otras filas con el mismo registro PI en la sede que NO se cuentan (para comparar)
            var otras = await _context.StockInsumos.AsNoTracking()
                .Where(s => s.IdSede == idSede && s.IdProductoIntermedio == idProductoIntermedio)
                .Select(s => new { s.Id, s.StockDisponible, s.IdNotaSalidaInsumo, s.Tipo })
                .ToListAsync();
            _logger.LogWarning("[STOCK-PI v2] Registro PI{Reg}: todas las filas con idPI en la sede: {Otras}",
                Alfanumerico.ConvertToBase36(idProductoIntermedio),
                string.Join(" | ", otras.Select(o => $"#{o.Id} stock={o.StockDisponible} idNSI={o.IdNotaSalidaInsumo} tipo={o.Tipo}")));

            // También a un archivo en la carpeta del proyecto (diagnóstico temporal)
            try
            {
                var linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} PI{Alfanumerico.ConvertToBase36(idProductoIntermedio)} (id {idProductoIntermedio}) sede {idSede} TOTAL={total} | CONTADAS: " +
                    string.Join(" ; ", filas.Select(f => $"#{f.Id} stock={f.StockDisponible} um={f.UnidadMedida} idPI={f.IdProductoIntermedio} idNSI={f.IdNotaSalidaInsumo} lote={f.NotaSalidaInsumo?.Lote}")) +
                    " | TODAS_CON_IDPI: " +
                    string.Join(" ; ", otras.Select(o => $"#{o.Id} stock={o.StockDisponible} idNSI={o.IdNotaSalidaInsumo} tipo={o.Tipo}")) +
                    Environment.NewLine;
                File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), "diagnostico-stock-pi.txt"), linea);
            }
            catch { /* solo diagnóstico */ }

            return total;
        }

        /// <summary>Datos de un registro PI para la Nota de Salida, con el stock del kardex.</summary>
        private async Task<List<RegistrosListaRes>> ObtenerDatosRegistroPI(int idRegistro, int idSede)
        {
            var lista = await _context.ProductosIntermedios
                .Where(x => x.Id == idRegistro)
                .Select(x => new RegistrosListaRes
                {
                    IdArticulo = x.IdInsumo,
                    DescripcionArticulo = x.Insumo != null ? x.Insumo.Descripcion : "",
                    CodigoArticulo = UtilFamilia.CodigoProductoIntermedio(x.IdInsumo),
                    Lote = x.Lote,
                    Um = x.Um
                })
                .ToListAsync();

            foreach (var r in lista)
            {
                r.StockDisponible = await ObtenerStockPI(idRegistro, idSede);
            }
            return lista;
        }
    }
}

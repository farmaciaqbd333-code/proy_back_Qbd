using proy_back_Qbd.Models.ProductoIntermedio;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Ajuste;
using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;

namespace Proy_back_QBD.Service.AjusteService.Familias
{
    /// <summary>Ajustes de inventario de la familia PT (productos terminados).</summary>
    public class ProductoTerminadoAjusteStrategy : IAjusteFamiliaStrategy
    {
        private readonly ApiContext _context;

        public ProductoTerminadoAjusteStrategy(ApiContext context)
        {
            _context = context;
        }

        public string Familia => "PT";

        public async Task<List<TablaAjustesRes>> Listar(int idSede)
        {
            var compras = await _context.CompraProductos
                .Include(cp => cp.Producto)
                .Include(cp => cp.Compra)
                .Include(cp => cp.StockProductoTerminados)
                    .ThenInclude(sp => sp.AjusteProductos)
                .Include(cp => cp.NotaSalidaProductos)
                    .ThenInclude(nsp => nsp.NotaSalida)
                .Where(w =>
                    (w.Compra != null && w.Compra.IdSede == idSede) ||
                    w.NotaSalidaProductos.Any(nsp =>
                        nsp.NotaSalida != null &&
                        nsp.NotaSalida.IdSedeDestino == idSede &&
                        (nsp.NotaSalida.Estado == "RECIBIDO" || nsp.NotaSalida.Estado == "RECEPCIONADO" || nsp.NotaSalida.FechaRecepcion != null || (nsp.CantidadRecibida > 0))
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var compraPtIds = compras.Select(c => c.Id).ToList();

            var ajustesPtQuery = await _context.AjusteProductoTerminados
                .Include(a => a.StockProducto)
                .Where(a =>
                    (a.StockProducto != null && compraPtIds.Contains(a.StockProducto.IdCompraProducto) && (idSede == 0 || a.StockProducto.IdSede == idSede)) ||
                    compraPtIds.Contains(a.IdStockProducto)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompraPt = ajustesPtQuery
                .GroupBy(a => (a.StockProducto != null && a.StockProducto.IdCompraProducto > 0) ? a.StockProducto.IdCompraProducto : a.IdStockProducto)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultado = new List<TablaAjustesRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede)
                {
                    entradasLote += (s.CantidadRecibida.HasValue && s.CantidadRecibida.Value > 0 ? s.CantidadRecibida.Value : s.CantidadSolicitada);
                }
                entradasLote += s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeDestino == idSede && (nsp.NotaSalida.Estado == "RECIBIDO" || nsp.NotaSalida.Estado == "RECEPCIONADO" || nsp.NotaSalida.FechaRecepcion != null || (nsp.CantidadRecibida > 0)))
                    .Sum(nsp => (nsp.CantidadRecibida > 0 ? nsp.CantidadRecibida : nsp.Cantidad));

                decimal salidasNS = s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nsp => nsp.Cantidad);

                var stockSede = s.StockProductoTerminados.Where(w => w.IdSede == idSede).ToList();
                var ajustesDeCompra = ajustesPorCompraPt.ContainsKey(s.Id) ? ajustesPorCompraPt[s.Id] : new List<AjusteProducto>();
                var ultimoAjuste = ajustesDeCompra.FirstOrDefault();

                decimal saldo = 0m;
                string observacionAjuste = "";

                if (ultimoAjuste != null)
                {
                    saldo = ultimoAjuste.StockNuevo;
                    observacionAjuste = ultimoAjuste.Observacion ?? "";
                }
                else if (stockSede.Any())
                {
                    saldo = stockSede.Sum(sp => sp.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdProducto),
                    IdCompraFamilia = s.Id,
                    Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Producto?.Descripcion ?? "",
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Clasificacion = "PT",
                    Observacion = observacionAjuste
                });
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        public async Task Registrar(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
        {
            List<AjusteProducto> ajusteProductoTerminados = new AjusteMapper().CrearAjusteProductoTerminadoList(listaAjustes, idCreador);
            foreach (var item in ajusteProductoTerminados)
            {
                AjusteProducto ajusteProductoTerminado = item;
                StockProducto? stockProducto = await _context.StockProductos
                    .Where(w => w.IdCompraProducto == ajusteProductoTerminado.IdStockProducto && (idSede <= 0 || w.IdSede == idSede))
                    .FirstOrDefaultAsync();

                if (stockProducto == null)
                {
                    var cp = await _context.CompraProductos.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteProductoTerminado.IdStockProducto);
                    if (cp == null) continue;
                    int finalSede = idSede > 0 ? idSede : (cp?.Compra?.IdSede ?? 15);
                    stockProducto = new StockProducto
                    {
                        IdCompraProducto = ajusteProductoTerminado.IdStockProducto,
                        IdSede = finalSede,
                        StockDisponible = ajusteProductoTerminado.StockNuevo
                    };
                    _context.StockProductos.Add(stockProducto);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockProducto.StockDisponible = ajusteProductoTerminado.StockNuevo;
                }
                ajusteProductoTerminado.IdStockProducto = stockProducto.Id;
                _context.AjusteProductoTerminados.Add(ajusteProductoTerminado);
            }
        }

        public async Task<List<DetalleAjusteRes>> Detalle(int registroId, int? idInsumo, int? idSede)
        {
            List<DetalleAjusteRes> response = new();
            var query = _context.AjusteProductoTerminados
                .Include(a => a.StockProducto)
                    .ThenInclude(s => s.CompraProducto)
                .Include(a => a.Creador)
                    .ThenInclude(c => c.Persona)
                .AsNoTracking();

            if (registroId > 0)
            {
                query = query.Where(w => (w.StockProducto != null && w.StockProducto.IdCompraProducto == registroId) || w.IdStockProducto == registroId);
            }
            else if (idInsumo.HasValue && idInsumo.Value > 0)
            {
                query = query.Where(w =>
                    ((w.StockProducto != null && w.StockProducto.CompraProducto != null && w.StockProducto.CompraProducto.IdProducto == idInsumo.Value) ||
                     (_context.CompraProductos.Any(cp => cp.Id == w.IdStockProducto && cp.IdProducto == idInsumo.Value))) &&
                    (!idSede.HasValue || idSede.Value == 0 || (w.StockProducto != null && w.StockProducto.IdSede == idSede.Value)));
            }
            else
            {
                return response;
            }

            var rawList = await query
                .OrderByDescending(odb => odb.FechaCreacion)
                .ToListAsync();

            var idsCompra = rawList
                .Select(s => (s.StockProducto != null && s.StockProducto.IdCompraProducto > 0) ? s.StockProducto.IdCompraProducto : s.IdStockProducto)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var comprasDict = await _context.CompraProductos
                .Where(cp => idsCompra.Contains(cp.Id))
                .ToDictionaryAsync(cp => cp.Id);

            response = rawList.Select(s =>
            {
                int idCompra = (s.StockProducto != null && s.StockProducto.IdCompraProducto > 0) ? s.StockProducto.IdCompraProducto : s.IdStockProducto;
                comprasDict.TryGetValue(idCompra, out var cp);
                string reg = cp != null ? ("PT" + Alfanumerico.ConvertToBase36(cp.Id)) : (idCompra > 0 ? ("PT" + Alfanumerico.ConvertToBase36(idCompra)) : "-");
                string lote = cp?.Lote ?? s.StockProducto?.CompraProducto?.Lote ?? "-";

                return new DetalleAjusteRes
                {
                    Registro = reg,
                    Lote = lote,
                    FechaCreacion = s.FechaCreacion,
                    Usuario = s.Creador != null && s.Creador.Persona != null ? (s.Creador.Persona.NombreCompleto ?? "") : "No registrado",
                    Stock = s.StockAnterior,
                    Diferencia = s.Ajuste,
                    StockFinal = s.StockNuevo,
                    Observacion = s.Observacion ?? ""
                };
            }).ToList();
            return response;
        }
    }
}

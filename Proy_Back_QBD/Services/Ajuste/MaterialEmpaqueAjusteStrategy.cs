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
    /// <summary>Ajustes de inventario de la familia ME (material de empaque).</summary>
    public class MaterialEmpaqueAjusteStrategy : IAjusteFamiliaStrategy
    {
        private readonly ApiContext _context;

        public MaterialEmpaqueAjusteStrategy(ApiContext context)
        {
            _context = context;
        }

        public string Familia => "ME";

        public async Task<List<TablaAjustesRes>> Listar(int idSede)
        {
            var compras = await _context.CompraEmpaques
                .Include(ce => ce.Empaque)
                .Include(ce => ce.Compra)
                .Include(ce => ce.StockEmpaques)
                    .ThenInclude(se => se.AjusteEmpaques)
                .Include(ce => ce.NotaSalidaEmpaques)
                    .ThenInclude(nse => nse.NotaSalida)
                .Where(w =>
                    (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                    w.NotaSalidaEmpaques.Any(nse =>
                        nse.NotaSalida != null &&
                        nse.NotaSalida.IdSedeDestino == idSede &&
                        (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0))
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var compraEmpaqueIds = compras.Select(c => c.Id).ToList();

            var ajustesEmpaqueQuery = await _context.AjusteEmpaques
                .Include(a => a.StockEmpaque)
                .Where(a =>
                    (a.StockEmpaque != null && compraEmpaqueIds.Contains(a.StockEmpaque.IdCompraEmpaque) && (idSede == 0 || a.StockEmpaque.IdSede == idSede)) ||
                    compraEmpaqueIds.Contains(a.IdStockEmpaque)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompraEmpaque = ajustesEmpaqueQuery
                .GroupBy(a => (a.StockEmpaque != null && a.StockEmpaque.IdCompraEmpaque > 0) ? a.StockEmpaque.IdCompraEmpaque : a.IdStockEmpaque)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultado = new List<TablaAjustesRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede && (idSede == 15 || s.Compra.FechaLab != null))
                {
                    entradasLote += (s.CantidadRecibida.HasValue && s.CantidadRecibida.Value > 0 ? s.CantidadRecibida.Value : s.CantidadSolicitada);
                }
                entradasLote += s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede && (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0)))
                    .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad));

                decimal salidasNS = s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nse => nse.Cantidad);

                var stockSede = s.StockEmpaques.Where(w => w.IdSede == idSede).ToList();
                var ajustesDeCompra = ajustesPorCompraEmpaque.ContainsKey(s.Id) ? ajustesPorCompraEmpaque[s.Id] : new List<AjusteEmpaque>();
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
                    saldo = stockSede.Sum(se => se.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoEmpaque(s.IdEmpaque),
                    IdCompraFamilia = s.Id,
                    Registro = "ME" + Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Empaque?.Descripcion ?? "",
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Clasificacion = "ME",
                    Observacion = observacionAjuste
                });
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        public async Task Registrar(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
        {
            List<AjusteEmpaque> ajusteEmpaques = new AjusteMapper().CrearAjusteEmpaqueList(listaAjustes, idCreador);
            foreach (var item in ajusteEmpaques)
            {
                AjusteEmpaque ajusteEmpaque = item;
                StockEmpaque? stockEmpaque = await _context.StockEmpaques
                    .Where(w => w.IdCompraEmpaque == ajusteEmpaque.IdStockEmpaque && (idSede <= 0 || w.IdSede == idSede))
                    .FirstOrDefaultAsync();

                if (stockEmpaque == null)
                {
                    var ce = await _context.CompraEmpaques.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteEmpaque.IdStockEmpaque);
                    if (ce == null) continue;
                    int finalSede = idSede > 0 ? idSede : (ce?.Compra?.IdSede ?? 15);
                    stockEmpaque = new StockEmpaque
                    {
                        IdCompraEmpaque = ajusteEmpaque.IdStockEmpaque,
                        IdSede = finalSede,
                        StockDisponible = ajusteEmpaque.StockNuevo
                    };
                    _context.StockEmpaques.Add(stockEmpaque);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockEmpaque.StockDisponible = ajusteEmpaque.StockNuevo;
                }
                if (stockEmpaque != null) {
                    ajusteEmpaque.IdStockEmpaque = stockEmpaque.Id;
                    _context.AjusteEmpaques.Add(ajusteEmpaque);
                }
            }
        }

        public async Task<List<DetalleAjusteRes>> Detalle(int registroId, int? idInsumo, int? idSede)
        {
            List<DetalleAjusteRes> response = new();
            var query = _context.AjusteEmpaques
                .Include(a => a.StockEmpaque)
                    .ThenInclude(s => s.CompraEmpaque)
                .Include(a => a.Creador)
                    .ThenInclude(c => c.Persona)
                .AsNoTracking();

            if (registroId > 0)
            {
                query = query.Where(w => (w.StockEmpaque != null && w.StockEmpaque.IdCompraEmpaque == registroId) || w.IdStockEmpaque == registroId);
            }
            else if (idInsumo.HasValue && idInsumo.Value > 0)
            {
                query = query.Where(w =>
                    ((w.StockEmpaque != null && w.StockEmpaque.CompraEmpaque != null && w.StockEmpaque.CompraEmpaque.IdEmpaque == idInsumo.Value) ||
                     (_context.CompraEmpaques.Any(ce => ce.Id == w.IdStockEmpaque && ce.IdEmpaque == idInsumo.Value))) &&
                    (!idSede.HasValue || idSede.Value == 0 || (w.StockEmpaque != null && w.StockEmpaque.IdSede == idSede.Value)));
            }
            else
            {
                return response;
            }

            var rawList = await query
                .OrderByDescending(odb => odb.FechaCreacion)
                .ToListAsync();

            var idsCompra = rawList
                .Select(s => (s.StockEmpaque != null && s.StockEmpaque.IdCompraEmpaque > 0) ? s.StockEmpaque.IdCompraEmpaque : s.IdStockEmpaque)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var comprasDict = await _context.CompraEmpaques
                .Where(ce => idsCompra.Contains(ce.Id))
                .ToDictionaryAsync(ce => ce.Id);

            response = rawList.Select(s =>
            {
                int idCompra = (s.StockEmpaque != null && s.StockEmpaque.IdCompraEmpaque > 0) ? s.StockEmpaque.IdCompraEmpaque : s.IdStockEmpaque;
                comprasDict.TryGetValue(idCompra, out var ce);
                string reg = ce != null ? ("ME" + Alfanumerico.ConvertToBase36(ce.Id)) : (idCompra > 0 ? ("ME" + Alfanumerico.ConvertToBase36(idCompra)) : "-");
                string lote = ce?.Lote ?? s.StockEmpaque?.CompraEmpaque?.Lote ?? "-";

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

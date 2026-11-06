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
    /// <summary>Ajustes de inventario de la familia ECO (economato).</summary>
    public class EconomatoAjusteStrategy : IAjusteFamiliaStrategy
    {
        private readonly ApiContext _context;

        public EconomatoAjusteStrategy(ApiContext context)
        {
            _context = context;
        }

        public string Familia => "ECO";

        public async Task<List<TablaAjustesRes>> Listar(int idSede)
        {
            var compras = await _context.CompraEconomatos
                .Include(ce => ce.Economato)
                .Include(ce => ce.Compra)
                .Include(ce => ce.StockEconomatos)
                    .ThenInclude(se => se.AjusteEconomatos)
                .Include(ce => ce.NotaSalidaEconomatos)
                    .ThenInclude(nse => nse.NotaSalida)
                .Where(w =>
                    (w.Compra != null && w.Compra.IdSede == idSede) ||
                    w.NotaSalidaEconomatos.Any(nse =>
                        nse.NotaSalida != null &&
                        nse.NotaSalida.IdSedeDestino == idSede &&
                        (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0))
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var compraEcoIds = compras.Select(c => c.Id).ToList();

            var ajustesEcoQuery = await _context.AjusteEconomatos
                .Include(a => a.StockEconomato)
                .Where(a =>
                    (a.StockEconomato != null && compraEcoIds.Contains(a.StockEconomato.IdCompraEconomato) && (idSede == 0 || a.StockEconomato.IdSede == idSede)) ||
                    compraEcoIds.Contains(a.IdStockEconomato)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompraEco = ajustesEcoQuery
                .GroupBy(a => (a.StockEconomato != null && a.StockEconomato.IdCompraEconomato > 0) ? a.StockEconomato.IdCompraEconomato : a.IdStockEconomato)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultado = new List<TablaAjustesRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede)
                {
                    entradasLote += s.CantidadSolicitada;
                }
                entradasLote += s.NotaSalidaEconomatos
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede && (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0)))
                    .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad));

                decimal salidasNS = s.NotaSalidaEconomatos
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nse => nse.Cantidad);

                var stockSede = s.StockEconomatos.Where(w => w.IdSede == idSede).ToList();
                var ajustesDeCompra = ajustesPorCompraEco.ContainsKey(s.Id) ? ajustesPorCompraEco[s.Id] : new List<AjusteEconomato>();
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
                    Codigo = UtilFamilia.CodigoInsumo(s.IdEconomato),
                    IdCompraFamilia = s.Id,
                    Registro = "ECO" + Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Economato?.Descripcion ?? "",
                    Lote = "",
                    Saldo = saldo,
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Clasificacion = "ECO",
                    Observacion = observacionAjuste
                });
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        public async Task Registrar(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
        {
            List<AjusteEconomato> ajusteEconomatos = new AjusteMapper().CrearAjusteEconomatoList(listaAjustes, idCreador);
            foreach (var item in ajusteEconomatos)
            {
                AjusteEconomato ajusteEconomato = item;
                StockEconomato? stockEconomato = await _context.StockEconomatos
                    .Where(w => w.IdCompraEconomato == ajusteEconomato.IdStockEconomato && (idSede <= 0 || w.IdSede == idSede))
                    .FirstOrDefaultAsync();

                if (stockEconomato == null)
                {
                    var ce = await _context.CompraEconomatos.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteEconomato.IdStockEconomato);
                    if (ce == null) continue;
                    int finalSede = idSede > 0 ? idSede : (ce?.Compra?.IdSede ?? 15);
                    stockEconomato = new StockEconomato
                    {
                        IdCompraEconomato = ajusteEconomato.IdStockEconomato,
                        IdSede = finalSede,
                        StockDisponible = ajusteEconomato.StockNuevo
                    };
                    _context.StockEconomatos.Add(stockEconomato);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockEconomato.StockDisponible = ajusteEconomato.StockNuevo;
                }
                if (stockEconomato != null) {
                    ajusteEconomato.IdStockEconomato = stockEconomato.Id;
                    _context.AjusteEconomatos.Add(ajusteEconomato);
                }
            }
        }

        public async Task<List<DetalleAjusteRes>> Detalle(int registroId, int? idInsumo, int? idSede)
        {
            List<DetalleAjusteRes> response = new();
            var query = _context.AjusteEconomatos
                .Include(a => a.StockEconomato)
                    .ThenInclude(s => s.CompraEconomato)
                .Include(a => a.Creador)
                    .ThenInclude(c => c.Persona)
                .AsNoTracking();

            if (registroId > 0)
            {
                query = query.Where(w => (w.StockEconomato != null && w.StockEconomato.IdCompraEconomato == registroId) || w.IdStockEconomato == registroId);
            }
            else if (idInsumo.HasValue && idInsumo.Value > 0)
            {
                query = query.Where(w =>
                    ((w.StockEconomato != null && w.StockEconomato.CompraEconomato != null && w.StockEconomato.CompraEconomato.IdEconomato == idInsumo.Value) ||
                     (_context.CompraEconomatos.Any(ce => ce.Id == w.IdStockEconomato && ce.IdEconomato == idInsumo.Value))) &&
                    (!idSede.HasValue || idSede.Value == 0 || (w.StockEconomato != null && w.StockEconomato.IdSede == idSede.Value)));
            }
            else
            {
                return response;
            }

            var rawList = await query
                .OrderByDescending(odb => odb.FechaCreacion)
                .ToListAsync();

            var idsCompra = rawList
                .Select(s => (s.StockEconomato != null && s.StockEconomato.IdCompraEconomato > 0) ? s.StockEconomato.IdCompraEconomato : s.IdStockEconomato)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var comprasDict = await _context.CompraEconomatos
                .Where(ce => idsCompra.Contains(ce.Id))
                .ToDictionaryAsync(ce => ce.Id);

            response = rawList.Select(s =>
            {
                int idCompra = (s.StockEconomato != null && s.StockEconomato.IdCompraEconomato > 0) ? s.StockEconomato.IdCompraEconomato : s.IdStockEconomato;
                comprasDict.TryGetValue(idCompra, out var ce);
                string reg = ce != null ? ("ECO" + Alfanumerico.ConvertToBase36(ce.Id)) : (idCompra > 0 ? ("ECO" + Alfanumerico.ConvertToBase36(idCompra)) : "-");

                return new DetalleAjusteRes
                {
                    Registro = reg,
                    Lote = "-",
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Kardex.Stock
{
    /// <summary>Stock de la lista principal del Kardex para la familia ECO (economato).</summary>
    public class EconomatoStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<EconomatoStockStrategy> _logger;

        public EconomatoStockStrategy(ApiContext context, ILogger<EconomatoStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "ECO";

        public async Task<List<StockRes>> ObtenerStock(int idSede)
        {
            return await _context.Economatos.AsNoTracking()
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = s.Select(s => s.UnidadMedida).FirstOrDefault() ?? "Und",
                            Entradas = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(ce => ce.CantidadSolicitada)) +
                                       s.Sum(x => x.CompraEconomatos.Sum(x2 => x2.NotaSalidaEconomatos.Where(w => w.NotaSalida != null && w.NotaSalida.IdSedeDestino == idSede && (w.NotaSalida.Estado == "RECIBIDO" || w.NotaSalida.Estado == "RECEPCIONADO" || w.NotaSalida.FechaRecepcion != null || w.CantidadRecibida > 0)).Sum(x3 => x3.CantidadRecibida > 0 ? x3.CantidadRecibida : x3.Cantidad))),
                            Salidas = s.Sum(s => s.CompraEconomatos.Sum(s2 => s2.NotaSalidaEconomatos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.Cantidad))),
                            Ajustes = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEconomatos.Sum(s => s.AjusteEconomatos.Sum(s => s.Ajuste)))),
                            Baja = 0,
                            Tipo = null,
                            CodigoUbicacion = s.Select(s => s.EconomatoSedes.Where(w => w.IdSite == idSede).Select(s => s.Location).FirstOrDefault()).FirstOrDefault(),
                            Limite = s.Select(s => s.EconomatoSedes.Where(w => w.IdSite == idSede).Select(s => s.Limite).FirstOrDefault()).FirstOrDefault()
                        }).ToListAsync();
        }
    }
}

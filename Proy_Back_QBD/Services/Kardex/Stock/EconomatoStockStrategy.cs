using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Kardex.Ledger;
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
            // Stock = libro mayor por lote (misma regla que el kardex por lote, ajustes y nota de salida)
            var mayor = await new KardexLedgerEcoService(_context).CalcularAsync(idSede, null);

            var lista = await _context.Economatos.AsNoTracking()
                .Select(e => new StockRes()
                {
                    Codigo = e.Id + "",
                    Descripcion = e.Descripcion ?? "",
                    Um = e.UnidadMedida ?? "Und",
                    Tipo = null,
                    CodigoUbicacion = e.EconomatoSedes.Where(w => w.IdSite == idSede).Select(x => x.Location).FirstOrDefault(),
                    Limite = e.EconomatoSedes.Where(w => w.IdSite == idSede).Select(x => x.Limite).FirstOrDefault()
                }).ToListAsync();

            foreach (var item in lista)
            {
                LedgerResultado? m = int.TryParse(item.Codigo, out var id) && mayor.TryGetValue(id, out var r) ? r : null;
                item.Entradas = m?.TotalEntradas ?? 0m;
                item.Salidas = m?.TotalSalidas ?? 0m;
                item.Ajustes = m?.TotalAjustes ?? 0m;
                item.Baja = m?.TotalBaja ?? 0m;
                item.Stock = m?.Stock ?? 0m;
                item.Saldo = item.Stock;
            }
            return lista;
        }
    }
}

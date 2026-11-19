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
    /// <summary>Stock de la lista principal del Kardex para la familia PT (productos terminados).</summary>
    public class ProductoTerminadoStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<ProductoTerminadoStockStrategy> _logger;

        public ProductoTerminadoStockStrategy(ApiContext context, ILogger<ProductoTerminadoStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "PT";

        public async Task<List<StockRes>> ObtenerStock(int idSede)
        {
            // Stock = libro mayor por lote (misma regla que el kardex por lote, ajustes y nota de salida)
            var mayor = await new KardexLedgerPtService(_context).CalcularAsync(idSede, null);

            var lista = await _context.Productos.AsNoTracking()
                .Select(p => new StockRes()
                {
                    Codigo = p.Id + "",
                    Descripcion = p.Descripcion ?? "",
                    Um = "UND",
                    Tipo = null,
                    CodigoUbicacion = p.ProductoSedes.Where(w => w.IdSite == idSede).Select(x => x.Location).FirstOrDefault(),
                    Limite = p.ProductoSedes.Where(w => w.IdSite == idSede).Select(x => x.Limite).FirstOrDefault()
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

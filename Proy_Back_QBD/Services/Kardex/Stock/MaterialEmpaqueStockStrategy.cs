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
using proy_back_Qbd.Services.Kardex.Ledger;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Kardex.Stock
{
    /// <summary>Stock de la lista principal del Kardex para la familia ME (material de empaque).</summary>
    public class MaterialEmpaqueStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<MaterialEmpaqueStockStrategy> _logger;

        public MaterialEmpaqueStockStrategy(ApiContext context, ILogger<MaterialEmpaqueStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "ME";

        public async Task<List<StockRes>> ObtenerStock(int idSede)
        {
            // Stock = libro mayor por lote (misma regla que kardex, ajustes y nota de salida)
            var mayor = await new KardexLedgerMeService(_context).CalcularAsync(idSede, null);

            var lista = await _context.Empaques.AsNoTracking()
                .Select(e => new StockRes()
                {
                    Codigo = e.Id + "",
                    Descripcion = e.Descripcion ?? "",
                    Um = "UND",
                    Tipo = null,
                    CodigoUbicacion = e.EmpaqueSedes.Where(w => w.IdSite == idSede).Select(x => x.Location).FirstOrDefault() ?? e.CodigoUbicacion,
                    Limite = e.EmpaqueSedes.Where(w => w.IdSite == idSede).Select(x => x.Limite).FirstOrDefault()
                }).ToListAsync();

            foreach (var item in lista)
            {
                if (!int.TryParse(item.Codigo, out int empId) || !mayor.TryGetValue(empId, out var m))
                {
                    item.Entradas = 0m; item.Salidas = 0m; item.Ajustes = 0m; item.Baja = 0m; item.Stock = 0m; item.Saldo = 0m;
                    continue;
                }
                item.Entradas = m.TotalEntradas;
                item.Salidas = m.TotalSalidas;
                item.Ajustes = m.TotalAjustes;
                item.Baja = m.TotalBaja;
                item.Stock = m.Stock;
                item.Saldo = m.Stock;
            }

            return lista;
        }
    }
}

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
    /// <summary>Stock de la lista principal del Kardex para la familia PI (productos intermedios).</summary>
    public class ProductoIntermedioStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<ProductoIntermedioStockStrategy> _logger;

        public ProductoIntermedioStockStrategy(ApiContext context, ILogger<ProductoIntermedioStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "PI";

        public async Task<List<StockRes>> ObtenerStock(int idSede)
        {
            var insumos = await _context.Insumos.AsNoTracking()
                .Where(i => i.Clasificacion == "PI")
                .ToListAsync();

            var insumoIds = insumos.Select(i => i.Id).ToList();

            // Libro mayor por lote (misma regla que MP): entradas, salidas, ajustes (conteos) y bajas salen de movimientos
            var mayor = await new KardexLedgerPiService(_context).CalcularAsync(idSede, insumoIds);

            var ubicaciones = await _context.SiteSupplies.AsNoTracking()
                .Where(w => w.IdSite == idSede)
                .ToListAsync();

            var resultado = new List<StockRes>();

            foreach (var insumo in insumos)
            {
                mayor.PorInsumo.TryGetValue(insumo.Id, out var libro);
                decimal saldo = libro?.Stock ?? 0m;
                var siteSupply = ubicaciones.FirstOrDefault(w => w.IdSupply == insumo.Id);

                resultado.Add(new StockRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(insumo.Id),
                    Descripcion = insumo.Descripcion,
                    Um = insumo.UnidadMedida ?? "G",
                    Entradas = libro?.TotalEntradas ?? 0m,
                    Salidas = libro?.TotalSalidas ?? 0m,
                    Ajustes = libro?.TotalAjustes ?? 0m,
                    Baja = libro?.TotalBaja ?? 0m,
                    Stock = saldo,
                    Saldo = saldo,
                    Tipo = insumo.Tipo,
                    CodigoUbicacion = siteSupply?.Location,
                    Limite = siteSupply?.Limite
                });
            }

            return resultado;
        }
    }
}

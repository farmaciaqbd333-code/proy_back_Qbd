// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using proy_back_Qbd.Models.Stock;
// using proy_back_Qbd.Services.Interfaces;
// using Proy_back_QBD.Data;

// namespace proy_back_Qbd.Services
// {
//     public class StockService : IStockService
//     {
//         private readonly ApiContext _context;
//         public StockService(ApiContext context)
//         {
//             _context = context;
//         }
//         public async Task<List<StockGetRes>> StockGetRes()
//         {
//             List<StockGetRes> stockGetRes = await _context.DetalleCompras
//             .GroupBy(g => "MP-QbD-" + (g.IdInsumo).ToString("D4"))
//             .Select(s => new StockGetRes
//             {
//                 Codigo = s.Key,
//                 Descripcion = s.First().Insumo != null ? s.First().Insumo.Descripcion : "",
//                 Um = s.First().Um,
//                 Entradas = s.Sum(s2 => s2.CantidadSolicitada),
//                 Salidas = s.Sum(s2 => s2)
//             });
//         }
//     }
// }
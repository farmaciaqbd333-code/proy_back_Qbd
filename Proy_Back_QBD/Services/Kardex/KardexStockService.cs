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

namespace proy_back_Qbd.Services.Kardex
{
    /// <summary>
    /// Lista principal de stock y compras vencidas. El cálculo de cada familia
    /// vive en su propia estrategia (IStockFamiliaStrategy): para añadir una familia
    /// basta con crear una clase nueva y registrarla, sin modificar este servicio.
    /// </summary>
    public class KardexStockService : IKardexStockService
    {
        private readonly ApiContext _context;
        private static readonly List<string> FamiliasAptas = ["MP", "ME"];
        private readonly IReadOnlyDictionary<string, IStockFamiliaStrategy> _estrategias;

        public KardexStockService(ApiContext context, IEnumerable<IStockFamiliaStrategy> estrategias)
        {
            _context = context;
            _estrategias = estrategias.ToDictionary(e => e.Familia);
        }

        public async Task<List<StockRes>> StockListaPrincipal(string familia, int idSede)
        {
            if (familia == null || !_estrategias.TryGetValue(familia, out var estrategia))
                throw new BadRequestException("FAMILIA NO VALIDA");

            List<StockRes> responseMP = await estrategia.ObtenerStock(idSede);
            foreach (var item in responseMP)
            {
                if (item.Stock == null || item.Saldo == null)
                {
                    var calc = (item.Entradas ?? 0) - (item.Salidas ?? 0) + (item.Ajustes ?? 0) - (item.Baja ?? 0);
                    item.Stock ??= calc;
                    item.Saldo ??= calc;
                }
            }


            return responseMP;
        }

        public async Task<List<ComprasVencidasRes>> ObtenerComprasVencidas(string familia, int idSede)
        {
            List<ComprasVencidasRes> response = new();
            if (FamiliasAptas.Contains(familia))
            {
                if (familia == "MP")
                {
                    response = await _context.CompraInsumos.Where(w => w.Compra != null && w.Compra.IdSede == idSede && w.Compra.FechaLab != null).Select(s => new ComprasVencidasRes()
                    {
                        Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
                        Codigo = s.Insumo.Id.ToString("d4"),
                        Descripcion = s.Insumo.Descripcion,
                        Estado = (DateTime.UtcNow > s.FechaVencimiento) ? "VENCIDO" : s.FechaVencimiento <= DateTime.UtcNow.AddDays(7) ? "POR VENCER" : "VIGENTE",
                        Lote = s.Lote,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Saldo = s.StockInsumos.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible),
                        Cantidad = s.PaqueteInsumos.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario)

                    }).ToListAsync();
                }
                if (familia == "ME")
                {
                    response = await _context.CompraEmpaques.Where(w => w.Compra != null && w.Compra.IdSede == idSede && w.Compra.FechaLab != null).Select(s => new ComprasVencidasRes()
                    {
                        Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
                        Codigo = s.Empaque.Id.ToString("d4"),
                        Descripcion = s.Empaque.Descripcion,
                        Estado = (DateTime.UtcNow > s.FechaVencimiento) ? "VENCIDO" : s.FechaVencimiento <= DateTime.UtcNow.AddDays(7) ? "POR VENCER" : "VIGENTE",
                        Lote = s.Lote,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Saldo = s.StockEmpaques.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible),
                        Cantidad = s.PaqueteEmpaques.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario)
                    }).ToListAsync();
                }
                response = response.OrderBy(x =>
                {
                    var clean = System.Text.RegularExpressions.Regex.Replace(x.Codigo ?? "0", @"[^\d]", "");
                    return int.TryParse(clean, out int num) ? num : 0;
                }).ToList();
                return response;
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }

        }
    }
}

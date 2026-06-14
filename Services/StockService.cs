using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models.Stock;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class StockService : IStockService
    {
        private readonly ApiContext _context;
        public StockService(ApiContext context)
        {
            _context = context;
        }

        public async Task<StockGetRes> StockListaPrincipal()
        {
            StockGetRes response = new();
            List<StockMPRes> responseMP = await _context.CompraInsumos
            .GroupBy(g => new { g.IdInsumo, g.Insumo.Descripcion, g.Insumo.UnidadMedida })
            .Select(s => new StockMPRes()
            {
                Codigo = s.Key.IdInsumo + "",
                Descripcion = s.Key.Descripcion + "",
                Um = s.Key.UnidadMedida + "",
                Entradas = s.Sum(x => x.PaqueteInsumos != null ? x.PaqueteInsumos.Sum(pi => pi.Paquete.CantidadPaquete) : 0),
                Salidas = s.Sum(x => x.Insumo.Familia.DetalleNotaSalida.Sum(s2 => s2.Cantidad) + x.Insumo.ElaboracionBases.Sum(s => s.Cantidad)),
                Ajustes = s.Sum(s => s.AjusteInsumos.Sum(s => s.Ajuste)),
                Baja = s.Where(w => w.FechaVencimiento >= DateTimeOffset.UtcNow).Sum(s => s.PaqueteInsumos.Sum(s => s.Paquete.CantidadPaquete))
            }).ToListAsync()
            ;
            List<StockMERes> responseME = await _context.CompraEmpaques
            .GroupBy(g => new { g.IdEmpaque, g.Empaque.Descripcion, g.Um })
            .Select(s => new StockMERes()
            {
                Codigo = s.Key.IdEmpaque + "",
                Descripcion = s.Key.Descripcion + "",
                Um = s.Key.Um + "",
                Entradas = s.Sum(x => x.PaqueteEmpaques != null ? x.PaqueteEmpaques.Sum(pi => pi.Paquete.CantidadPaquete) : 0),
                Salidas = s.Sum(x => x.Empaque.Familia.DetalleNotaSalida.Sum(s2 => s2.Cantidad) + x.Empaque.ElaboracionBases.Count()),
                Ajustes = s.Sum(s => s.AjusteEmpaques.Sum(s => s.Ajuste)),
                Baja = s.Where(w => w.FechaVencimiento >= DateTimeOffset.UtcNow).Sum(s => s.PaqueteEmpaques.Sum(s => s.Paquete.CantidadPaquete))
            }).ToListAsync()
            ;
            response.MateriaPrimas = responseMP;

            return response;
        }
    }
}
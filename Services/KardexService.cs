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
    public class KardexService : IKardexService
    {
        private readonly ApiContext _context;
        public KardexService(ApiContext context)
        {
            _context = context;
        }

        public async Task<StockGetRes> StockListaPrincipal()
        {
            StockGetRes response = new();
            List<StockMPRes> responseMP = await _context.Insumos
            .GroupBy(g => new { g.Id })
            .Select(s => new StockMPRes()
            {
                Codigo = s.Key.Id + "",
                Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                Um = s.Select(x => x.UnidadMedida).FirstOrDefault() ?? string.Empty,
                Entradas = s.Sum(s => s.CompraInsumos.Sum(s => s.PaqueteInsumos.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario))),
                Salidas = s.Sum(x => x.DetalleNotaSalidaInsumo.Sum(s2 => s2.Cantidad) + x.ElaboracionBases.Sum(s => s.Cantidad)),
                Ajustes = s.Sum(s => s.CompraInsumos.Sum(s => s.AjusteInsumos.Sum(s => s.Ajuste))),
                Baja = s.Sum(s => s.CompraInsumos.Where(w => w.FechaVencimiento >= DateTimeOffset.UtcNow).Sum(s => s.PaqueteInsumos.Sum(s => s.Paquete.StockDisponible)))
            }).ToListAsync()
            ;
            List<StockMERes> responseME = await _context.Empaques
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockMERes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = "Und",
                            Entradas = s.Sum(s => s.CompraEmpaques.Sum(s => s.PaqueteEmpaques.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario))),
                            Salidas = s.Sum(x => x.DetalleNotaSalidaEmpaques.Sum(s2 => s2.Cantidad) + x.ElaboracionBases.Count()),
                            Ajustes = s.Sum(s => s.CompraEmpaques.Sum(s => s.AjusteEmpaques.Sum(s => s.Ajuste))),
                            Baja = s.Sum(s => s.CompraEmpaques.Where(w => w.FechaVencimiento >= DateTimeOffset.UtcNow).Sum(s => s.PaqueteEmpaques.Sum(s => s.Paquete.StockDisponible)))
                        }).ToListAsync()
                        ;
            response.MateriaPrimas = responseMP;
            response.Empaques = responseME;

            return response;
        }
    }
}
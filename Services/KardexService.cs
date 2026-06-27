using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
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

        public async Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int insumoId)
        {
            var resultado = new List<DetalleInsumoRes>();

            resultado = await _context.CompraInsumos
            .Where(w => w.IdInsumo == insumoId)
            .Select(s => new DetalleInsumoRes
            {
                Registro = Alfanumerico.ConvertToBase36(s.Id),
                Lote = s.Lote ?? "",
                Saldo = s.StockDisponible ?? 0,
                FechaFabricacion = s.FechaFabricacion,
                FechaVencimiento = s.FechaVencimiento
            })
            .ToListAsync();

            return resultado;
        }

        public async Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId)
        {
            var resultado = new List<DetalleEmpaqueRes>();
            resultado = await _context.CompraEmpaques
      .Where(w => w.IdEmpaque == empaqueId)
      .Select(s => new DetalleEmpaqueRes
      {
          Registro = Alfanumerico.ConvertToBase36(s.Id),
          Lote = s.Lote ?? "",
          Saldo = s.StockDisponible ?? 0,
          FechaFabricacion = s.FechaFabricacion,
          FechaVencimiento = s.FechaVencimiento
      })
      .ToListAsync();

            return resultado;
        }

        public async Task<ListarStockRes> StockListaPrincipal()
        {
            ListarStockRes response = new();
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
                Baja = s.Sum(x => x.CompraInsumos
    .Where(ci => ci.FechaVencimiento >= DateTimeOffset.UtcNow)
    .Sum(ci => ci.StockDisponible ?? 0))
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
                            Baja = s.Sum(x => x.CompraEmpaques
    .Where(ce => ce.FechaVencimiento >= DateTimeOffset.UtcNow)
    .Sum(ce => ce.StockDisponible ?? 0))
                        }).ToListAsync()
                        ;
            response.MateriaPrimas = responseMP;
            response.Empaques = responseME;

            return response;
        }

        public async Task RegistrarAjuste(CrearAjusteReq request)
        {
            if (request.Familia == "MP")
            {
                throw new NotImplementedException();
            }
            else if (request.Familia == "ME")
            {

            }
            else
            {
                throw new BadRequestException("Familia no admitida");
            }
        }
    }
}
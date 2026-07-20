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
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class KardexService : IKardexService
    {
        private readonly ApiContext _context;
        private static readonly List<string> FamiliasAptas = ["MP", "ME"];
        public KardexService(ApiContext context)
        {
            _context = context;
        }

        public async Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int insumoId)
        {
            var resultado = new List<DetalleInsumoRes>();

            resultado = await _context.CompraInsumos
            .Include(w => w.Compra)
            .Where(w => w.IdInsumo == insumoId)
            .Select(s => new DetalleInsumoRes
            {
                Registro = Alfanumerico.ConvertToBase36(s.Id),
                Lote = s.Lote ?? "",
                Saldo = s.StockDisponible,
                FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                FechaFabricacion = s.FechaFabricacion,
                FechaVencimiento = s.FechaVencimiento,
                Observacion = s.Observacion
            })
            .ToListAsync();

            return resultado;
        }

        public async Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId)
        {
            var resultado = new List<DetalleEmpaqueRes>();
            resultado = await _context.CompraEmpaques
      .Include(w => w.Compra)
      .Where(w => w.IdEmpaque == empaqueId)
      .Select(s => new DetalleEmpaqueRes
      {
          Registro = Alfanumerico.ConvertToBase36(s.Id),
          Lote = s.Lote ?? "",
          Saldo = s.StockDisponible,
          FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
          FechaFabricacion = s.FechaFabricacion,
          FechaVencimiento = s.FechaVencimiento,
          Observacion = s.Observacion
      })
      .ToListAsync();

            return resultado;
        }

        public async Task<List<StockRes>> StockListaPrincipal(string familia)
        {
            List<StockRes> responseMP = familia switch
            {
                "MP" => await ObtenerMateriaPrima("MP"),
                "PI" => await ObtenerMateriaPrima("PI"),
                "ME" => await ObtenerMateriaEmpaque(),
                "ECO" => await ObtenerEconomato(),
                "PT" => await ObtenerProductoTerminado(),
                _ => throw new BadRequestException("FAMILIA NO VALIDA")

            };

            return responseMP;
        }

        public async Task<List<ComprasVencidasRes>> ObtenerComprasVencidas(string familia)
        {
            List<ComprasVencidasRes> response = new();
            if (FamiliasAptas.Contains(familia))
            {
                if (familia == "MP")
                {
                    response = await _context.CompraInsumos.Select(s => new ComprasVencidasRes()
                    {
                        Registro = Alfanumerico.ConvertToBase36(s.Id),
                        Codigo = s.Insumo.Id.ToString("d4"),
                        Descripcion = s.Insumo.Descripcion,
                        Estado = (DateTimeOffset.UtcNow > s.FechaVencimiento) ? "VENCIDO" : s.FechaVencimiento <= DateTimeOffset.UtcNow.AddDays(7) ? "POR VENCER" : "VIGENTE",
                        Lote = s.Lote,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Saldo = s.StockDisponible,
                        Cantidad = s.PaqueteInsumos.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario)

                    }).ToListAsync();
                }
                if (familia == "ME")
                {
                    response = await _context.CompraEmpaques.Select(s => new ComprasVencidasRes()
                    {
                        Registro = Alfanumerico.ConvertToBase36(s.Id),
                        Codigo = s.Empaque.Id.ToString("d4"),
                        Descripcion = s.Empaque.Descripcion,
                        Estado = (DateTimeOffset.UtcNow > s.FechaVencimiento) ? "VENCIDO" : s.FechaVencimiento <= DateTimeOffset.UtcNow.AddDays(7) ? "POR VENCER" : "VIGENTE",
                        Lote = s.Lote,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                        Saldo = s.StockDisponible,
                        Cantidad = s.PaqueteEmpaques.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario)

                    }).ToListAsync();
                }
                return response;
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }

        }
        private async Task<List<StockRes>> ObtenerMateriaPrima(string clasificacion)
        {
            return await _context.Insumos
            .Where(i => i.Clasificacion == clasificacion || (clasificacion == "MP" && i.Clasificacion == null))
            .GroupBy(g => new { g.Id })
            .Select(s => new StockRes()
            {
                Codigo = s.Key.Id + "",
                Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                Um = s.Select(x => x.UnidadMedida).FirstOrDefault() ?? string.Empty,
                Entradas = s.Sum(s => s!.CompraInsumos!.Sum(s => s.PaqueteInsumos!.Sum(s => s.Paquete!.CantidadPaquete * s.Paquete.PesoUnitario))),
                Salidas = s.Sum(x => x.NotaSalidaInsumos!.Sum(s2 => s2.Cantidad) + x.ProductoIntermedio!.Sum(s => s.Cantidad)),
                Ajustes = s.Sum(s => s.CompraInsumos!.Sum(s => s.AjusteInsumos!.Sum(s => s.Ajuste))),
                Baja = s.Sum(x => x.CompraInsumos!
            .Where(ci => ci.FechaVencimiento < DateTimeOffset.UtcNow)
            .Sum(ci => ci.StockDisponible)),
                Tipo = s.Select(x => x.Tipo).FirstOrDefault()
            }).ToListAsync()
            ;
        }
        private async Task<List<StockRes>> ObtenerMateriaEmpaque()
        {
            return await _context.Empaques
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = "Und",
                            Entradas = s.Sum(s => s.CompraEmpaques!.Sum(s => s.PaqueteEmpaques!.Sum(s => s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario))),
                            Salidas = s.Sum(x => x.DetalleNotaSalidaEmpaques!.Sum(s2 => s2.Cantidad) + x.EmpaqueProductoIntermedios.Count()),
                            Ajustes = s.Sum(s => s.CompraEmpaques.Sum(s => s.AjusteEmpaques.Sum(s => s.Ajuste))),
                            Baja = s.Sum(x => x.CompraEmpaques
    .Where(ce => ce.FechaVencimiento < DateTimeOffset.UtcNow)
    .Sum(ce => ce.StockDisponible))
                        }).ToListAsync()
                        ;
        }
        private async Task<List<StockRes>> ObtenerEconomato()
        {
            return await _context.Economatos
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = s.Select(s => s.UnidadMedida).FirstOrDefault() ?? "Und",
                            Entradas = s.Sum(s => s.CompraEconomatos.Sum(ce => ce.CantidadSolicitada)),
                            Salidas =s.Sum(x => x.NotaSalidaEconomatos.Sum(s2 => s2.Cantidad)),
                            Ajustes = s.Sum(s => s.CompraEconomatos.Sum(s => s.AjusteEconomatos.Sum(s => s.Ajuste))),
                            Baja = 0
                        }).ToListAsync();
        }
        private async Task<List<StockRes>> ObtenerProductoTerminado()
        {
            return await _context.Productos
            .GroupBy(g => new { g.Id })
            .Select(s => new StockRes()
            {
                Codigo = s.Key.Id + "",
                Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                Um = "UND",
                Entradas = s.Sum(s => s.CompraProductos.Sum(s => s.CantidadSolicitada)),
                Salidas = s.Sum(x => x.NotaSalidaProductos.Sum(s2 => s2.Cantidad)),
                Ajustes = s.Sum(s => s.CompraProductos.Sum(s => s.AjusteProductoTerminados.Sum(s => s.Ajuste))),
                Baja = 0
            }).ToListAsync()
            ;
        }
    }


}
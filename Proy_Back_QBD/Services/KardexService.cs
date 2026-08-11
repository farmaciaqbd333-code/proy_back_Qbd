using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
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

        public async Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int idInsumo, int idSede)
        {
            var resultado = new List<DetalleInsumoRes>();

            resultado = await _context.CompraInsumos
                .Include(w => w.Compra)
                .Include(w => w.StockInsumos)
                .Include(w => w.PaqueteInsumos!)
                .ThenInclude(p => p.Paquete)
                .Where(w => w.IdInsumo == idInsumo)
                .Select(s => new DetalleInsumoRes
                {
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = s.StockInsumos.Any(w => w.IdSede == idSede)
                        ? s.StockInsumos.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible)
                        : (s.StockInsumos.Any()
                            ? s.StockInsumos.Sum(s2 => s2.StockDisponible)
                            : s.PaqueteInsumos.Sum(p => p.Paquete != null ? p.Paquete.CantidadPaquete * p.Paquete.PesoUnitario : 0)),
                    FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.Observacion
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId, int idSede)
        {
            var resultado = new List<DetalleEmpaqueRes>();
            resultado = await _context.CompraEmpaques
                .Include(w => w.Compra)
                .Include(w => w.StockEmpaques)
                .Include(w => w.PaqueteEmpaques!)
                .ThenInclude(p => p.Paquete)
                .Where(w => w.IdEmpaque == empaqueId)
                .Select(s => new DetalleEmpaqueRes
                {
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = s.StockEmpaques.Any(w => w.IdSede == idSede)
                        ? s.StockEmpaques.Where(w => w.IdSede == idSede).Sum(s2 => s2.StockDisponible)
                        : (s.StockEmpaques.Any()
                            ? s.StockEmpaques.Sum(s2 => s2.StockDisponible)
                            : s.PaqueteEmpaques.Sum(p => p.Paquete != null ? p.Paquete.CantidadPaquete * p.Paquete.PesoUnitario : 0)),
                    FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.Observacion
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<List<StockRes>> StockListaPrincipal(string familia, int idSede)
        {
            List<StockRes> responseMP = familia switch
            {
                "MP" => await ObtenerMateriaPrima(idSede),
                "PI" => await ObtenerProductosIntermedios(idSede),
                "ME" => await ObtenerMateriaEmpaque(idSede),
                "ECO" => await ObtenerEconomato(idSede),
                "PT" => await ObtenerProductosIntermedios(idSede),
                _ => throw new BadRequestException("FAMILIA NO VALIDA")

            };

            return responseMP;
        }

        public async Task<List<ComprasVencidasRes>> ObtenerComprasVencidas(string familia, int idSede)
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
                    response = await _context.CompraEmpaques.Select(s => new ComprasVencidasRes()
                    {
                        Registro = Alfanumerico.ConvertToBase36(s.Id),
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
                return response;
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }

        }
        // Fusionar 2 listas para entradas compras y notas de salida
        private async Task<List<StockRes>> ObtenerMateriaPrima(int idSede)
        {
            return await _context.Insumos
            .Where(i => (i.Clasificacion == "MP" || i.Clasificacion == null) && i.CompraInsumos.FirstOrDefault(s => s.Compra.IdSede == idSede) != null)
            .GroupBy(g => new { g.Id })
            .Select(s => new StockRes()
            {
                Codigo = s.Key.Id + "",
                Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                Um = s.Select(x => x.UnidadMedida).FirstOrDefault() ?? string.Empty,
                Entradas =
                //Suma de cantidad recibida de compraInsumo
                s.Sum(x => x.CompraInsumos!.Where(w => w.Compra.IdSede == idSede).Sum(ci => ci.CantidadRecibida)) +
                //Suma de cantidad recibida de nota de salidas
                s.Sum(x => x.CompraInsumos.Sum(x2 => x2.NotaSalidaInsumos.Where(w => w.NotaSalida.IdSedeDestino == idSede).Sum(x3 => x3.CantidadRecibida))),
                Salidas =
                //Suma de Producto Intermedio
                s.Sum(s => s.InsumoProductoIntermedio.Where(w => w.ProductoIntermedio.IdSede == idSede).Sum(s3 => s3.CantidadLote)) +
                //Suma de Formulas Magistrales
                s.Sum(s => s.FormulasCC.Where(w => w.Formula.SedeId == idSede).Sum(s2 => s2.CantidadL)) +
                //Suma de Notas de Salida
                s.Sum(s => s.CompraInsumos.Sum(s2 => s2.NotaSalidaInsumos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.CantidadRecibida)))
                ,
                Ajustes = s.Sum(s => s.CompraInsumos!.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockInsumos.Sum(s => s.AjusteInsumos!.Sum(s => s.Ajuste)))),
                Baja = s.Sum(x => x.CompraInsumos!
            .Where(ci => ci.FechaVencimiento < DateTime.UtcNow)
            .Sum(ci => ci.StockInsumos.Where(w => w.IdSede == idSede).Sum(sm => sm.StockDisponible))),
                Tipo = s.Select(x => x.Tipo).FirstOrDefault(),
                CodigoUbicacion = s.Select(x => x.CodigoUbicacion).FirstOrDefault()
            }).ToListAsync()
            ;
        }
        private async Task<List<StockRes>> ObtenerProductosIntermedios(int idSede)
        {
            return await _context.Insumos
            .Where(i => i.Clasificacion == "PI" && i.ProductoIntermedio.FirstOrDefault(s => s.IdSede == idSede) != null)
            .GroupBy(g => new { g.Id })
            .Select(s => new StockRes()
            {
                Codigo = UtilFamilia.CodigoInsumo(s.Key.Id),
                Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                Um = s.Select(x => x.UnidadMedida).FirstOrDefault() ?? string.Empty,
                Entradas = s.Sum(s => s!.ProductoIntermedio!
                    .Where(w => w.IdSede == idSede)
                    .Sum(s2 => (s2.PesoUnidad.HasValue && s2.PesoUnidad.Value > 0) 
                        ? s2.PesoUnidad.Value 
                        : ((s2.LoteEstTotal.HasValue && s2.LoteEstTotal.Value > 0) 
                            ? s2.LoteEstTotal.Value 
                            : (s2.LoteEstandar ?? 0)))),
                Salidas = 0,
                Ajustes = 0,
                Baja = s.Sum(x => x.ProductoIntermedio!
                    .Where(ci => ci.FechaVencimiento < DateTime.UtcNow && ci.IdSede == idSede)
                    .Sum(s2 => (s2.PesoUnidad.HasValue && s2.PesoUnidad.Value > 0) 
                        ? s2.PesoUnidad.Value 
                        : ((s2.LoteEstTotal.HasValue && s2.LoteEstTotal.Value > 0) 
                            ? s2.LoteEstTotal.Value 
                            : (s2.LoteEstandar ?? 0)))),
                Tipo = s.Select(x => x.Tipo).FirstOrDefault(),
                CodigoUbicacion = s.Select(x => x.CodigoUbicacion).FirstOrDefault()
            }).ToListAsync();
        }
        private async Task<List<StockRes>> ObtenerMateriaEmpaque(int idSede)
        {
            return await _context.Empaques
                        .Where(i => i.CompraEmpaques.FirstOrDefault(s => s.Compra.IdSede == idSede) != null)
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = "UND",
                            Entradas =
                            //Suma de cantidad recibida de compraEmpaque
                            s.Sum(x => x.CompraEmpaques!.Where(w => w.Compra.IdSede == idSede).Sum(ci => ci.CantidadRecibida)) +
                            //Suma de cantidad recibida de nota de salidas
                            s.Sum(x => x.CompraEmpaques.Sum(x2 => x2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeDestino == idSede).Sum(x3 => x3.CantidadRecibida))),
                            // adjuntar en pi, fm y nota de salida
                            Salidas =
                            //Suma de Productos Intermedios
                            s.Sum(s => s.EmpaqueProductoIntermedios.Count(w => w.ProductoIntermedio.IdSede == idSede)) +
                            //Suma de Laboratorios
                            s.Sum(s => s.Laboratorios.Count(w => w.SedeId == idSede)) +
                            //Suma de Notas de Salida
                            s.Sum(s => s.CompraEmpaques.Sum(s2 => s2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.CantidadRecibida))),
                            Ajustes =
                            //Suma de Ajustes hechas a compra empaques
                             s.Sum(s => s.CompraEmpaques.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEmpaques.Sum(s => s.AjusteEmpaques.Sum(s => s.Ajuste)))),
                            //Suma de Empaques vencidos
                            Baja = s.Sum(x => x.CompraEmpaques
                                    .Where(ce => ce.FechaVencimiento < DateTimeOffset.UtcNow)
                                    .Sum(ce => ce.StockEmpaques.Where(w => w.IdSede == idSede).Sum(sm => sm.StockDisponible))),
                            Tipo = null,
                            CodigoUbicacion = s.Select(x => x.CodigoUbicacion).FirstOrDefault()
                        }).ToListAsync();
        }
        private async Task<List<StockRes>> ObtenerEconomato(int idSede)
        {
            return await _context.Economatos
                        .Where(i => i.CompraEconomatos.FirstOrDefault(s => s.Compra.IdSede == idSede) != null)
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = s.Select(s => s.UnidadMedida).FirstOrDefault() ?? "Und",
                            Entradas = s.Sum(s => s.CompraEconomatos.Sum(ce => ce.CantidadSolicitada)),
                            //nota de salida
                            Salidas = 0,
                            Ajustes = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEconomatos.Sum(s => s.AjusteEconomatos.Sum(s => s.Ajuste)))),
                            Baja = 0
                        }).ToListAsync();
        }
    }


}
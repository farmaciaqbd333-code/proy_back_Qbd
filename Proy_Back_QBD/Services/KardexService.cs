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
            var compras = await _context.CompraInsumos
                .Include(w => w.Compra)
                .Include(w => w.StockInsumos)
                    .ThenInclude(si => si.AjusteInsumos)
                .Include(w => w.NotaSalidaInsumos)
                    .ThenInclude(nsi => nsi.NotaSalida)
                .Where(w => w.IdInsumo == idInsumo)
                .ToListAsync();

            var salidasFM = await _context.FormulasCC
                .Where(f => f.InsumoId == idInsumo && f.Formula != null && f.Formula.SedeId == idSede)
                .SumAsync(f => (decimal?)f.CantidadL) ?? 0m;

            var salidasPI = await _context.InsumoProductoIntermedios
                .Where(ipi => ipi.IdInsumo == idInsumo && ipi.ProductoIntermedio != null && ipi.ProductoIntermedio.IdSede == idSede)
                .SumAsync(ipi => (decimal?)ipi.CantidadLote) ?? 0m;

            var resultado = new List<DetalleInsumoRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede)
                {
                    entradasLote += (s.CantidadRecibida ?? 0m);
                }
                entradasLote += s.NotaSalidaInsumos
                    .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede)
                    .Sum(nsi => nsi.CantidadRecibida ?? nsi.Cantidad);

                decimal salidasNS = s.NotaSalidaInsumos
                    .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nsi => nsi.CantidadRecibida ?? nsi.Cantidad);

                decimal salidasLocales = (idSede == 15) ? salidasPI : (salidasFM + salidasPI);

                decimal ajustesLote = s.StockInsumos
                    .Where(si => si.IdSede == idSede)
                    .Sum(si => si.AjusteInsumos.Sum(a => a.Ajuste));

                decimal bajasLote = (s.FechaVencimiento < DateTime.UtcNow)
                    ? s.StockInsumos.Where(si => si.IdSede == idSede).Sum(si => si.StockDisponible)
                    : 0m;

                decimal saldo = entradasLote - salidasNS - salidasLocales + ajustesLote - bajasLote;

                if (entradasLote == 0 && salidasNS == 0 && saldo == 0)
                {
                    continue;
                }

                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "MP" + Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.Observacion
                });
            }

            return resultado;
        }

        public async Task<List<DetalleInsumoRes>> ObtenerDetallePI(int idInsumo, int idSede)
        {
            var resultado = await _context.ProductosIntermedios
                .Include(pi => pi.StockInsumo)
                .Where(w => w.IdInsumo == idInsumo && w.IdSede == idSede)
                .Select(s => new DetalleInsumoRes
                {
                    Registro = "PI" + Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = s.StockInsumo != null ? s.StockInsumo.StockDisponible : (s.LoteEstTotal ?? s.LoteEstandar ?? 0),
                    FechaCompra = s.FechaCreacion,
                    FechaFabricacion = s.FechaCreacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = ""
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId, int idSede)
        {
            var compras = await _context.CompraEmpaques
                .Include(w => w.Compra)
                .Include(w => w.StockEmpaques)
                .Include(w => w.NotaSalidaEmpaques)
                    .ThenInclude(nse => nse.NotaSalida)
                .Where(w => w.IdEmpaque == empaqueId)
                .ToListAsync();

            var resultado = new List<DetalleEmpaqueRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede)
                {
                    entradasLote += (s.CantidadRecibida ?? 0m);
                }
                entradasLote += s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede)
                    .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad));

                decimal salidasNS = s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad));

                var stockSede = s.StockEmpaques.Where(w => w.IdSede == idSede).ToList();

                decimal saldo = 0m;
                if (stockSede.Any())
                {
                    saldo = stockSede.Sum(se => se.StockDisponible);
                }
                else if (entradasLote > 0 || salidasNS > 0)
                {
                    saldo = entradasLote - salidasNS;
                }
                else
                {
                    continue;
                }

                resultado.Add(new DetalleEmpaqueRes
                {
                    Registro = "ME" + Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.Observacion
                });
            }

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
                "PT" => await ObtenerProductosTerminados(idSede),
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
                    response = await _context.CompraEmpaques.Select(s => new ComprasVencidasRes()
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
                response = response.OrderBy(x => {
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
        // Fusionar 2 listas para entradas compras y notas de salida
        private async Task<List<StockRes>> ObtenerMateriaPrima(int idSede)
        {
            return await _context.Insumos.AsNoTracking()
            .Where(i => (i.Clasificacion == "MP" || i.Clasificacion == null))
            .GroupBy(g => new { g.Id })
            .Select(s => new StockRes()
            {
                Codigo = s.Key.Id + "",
                Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                Um = s.Select(x => x.UnidadMedida).FirstOrDefault() ?? string.Empty,
                Entradas =
                //Suma de cantidad recibida de compraInsumo en esta sede
                s.Sum(x => x.CompraInsumos!.Where(w => w.Compra.IdSede == idSede).Sum(ci => ci.CantidadRecibida)) +
                //Suma de cantidad recibida de nota de salidas recibidas en esta sede
                s.Sum(x => x.CompraInsumos.Sum(x2 => x2.NotaSalidaInsumos.Where(w => w.NotaSalida.IdSedeDestino == idSede).Sum(x3 => x3.CantidadRecibida ?? x3.Cantidad))),
                Salidas =
                //Suma de Producto Intermedio
                s.Sum(s => s.InsumoProductoIntermedio.Where(w => w.ProductoIntermedio.IdSede == idSede).Sum(s3 => s3.CantidadLote)) +
                //Suma de Formulas Magistrales
                s.Sum(s => s.FormulasCC.Where(w => w.Formula.SedeId == idSede).Sum(s2 => s2.CantidadL)) +
                //Suma de Notas de Salida despachadas por esta sede
                s.Sum(s => s.CompraInsumos.Sum(s2 => s2.NotaSalidaInsumos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.CantidadRecibida ?? s3.Cantidad)))
                ,
                Ajustes = s.Sum(s2 => s2.CompraInsumos.Sum(s3 => s3.StockInsumos.Where(w => w.IdSede == idSede).Sum(s4 => s4.AjusteInsumos.Sum(s5 => s5.Ajuste)))),
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
            return await _context.Insumos.AsNoTracking()
            .Where(i => i.Clasificacion == "PI")
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
                Salidas = 
                    s.Sum(s => s.InsumoProductoIntermedio.Where(w => w.ProductoIntermedio.IdSede == idSede).Sum(s3 => s3.CantidadLote)) +
                    s.Sum(s => s.FormulasCC.Where(w => w.Formula.SedeId == idSede).Sum(s2 => s2.CantidadL)),
                Ajustes = 0,
                Baja = s.Sum(x => x.ProductoIntermedio!
                    .Where(ci => ci.FechaVencimiento < DateTime.UtcNow && ci.IdSede == idSede)
                    .Sum(s2 => s2.StockInsumo != null ? s2.StockInsumo.StockDisponible : 0)),
                Tipo = s.Select(x => x.Tipo).FirstOrDefault(),
                CodigoUbicacion = s.Select(x => x.CodigoUbicacion).FirstOrDefault()
            }).ToListAsync();
        }
        private async Task<List<StockRes>> ObtenerMateriaEmpaque(int idSede)
        {
            return await _context.Empaques.AsNoTracking()
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
                            s.Sum(x => x.CompraEmpaques.Sum(x2 => x2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeDestino == idSede).Sum(x3 => x3.CantidadRecibida > 0 ? x3.CantidadRecibida : x3.Cantidad))),
                            // adjuntar en pi, fm y nota de salida
                            Salidas =
                            //Suma de Productos Intermedios
                            s.Sum(s => s.EmpaqueProductoIntermedios.Count(w => w.ProductoIntermedio.IdSede == idSede)) +
                            //Suma de Laboratorios
                            s.Sum(s => s.Laboratorios.Count(w => w.SedeId == idSede)) +
                            //Suma de Notas de Salida
                            s.Sum(s => s.CompraEmpaques.Sum(s2 => s2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.CantidadRecibida > 0 ? s3.CantidadRecibida : s3.Cantidad))),
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
            return await _context.Economatos.AsNoTracking()
                        .GroupBy(g => new { g.Id })
                        .Select(s => new StockRes()
                        {
                            Codigo = s.Key.Id + "",
                            Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                            Um = s.Select(s => s.UnidadMedida).FirstOrDefault() ?? "Und",
                            Entradas = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(ce => ce.CantidadSolicitada)) +
                                       s.Sum(x => x.CompraEconomatos.Sum(x2 => x2.NotaSalidaEconomatos.Where(w => w.NotaSalida.IdSedeDestino == idSede).Sum(x3 => x3.CantidadRecibida > 0 ? x3.CantidadRecibida : x3.Cantidad))),
                            Salidas = s.Sum(s => s.CompraEconomatos.Sum(s2 => s2.NotaSalidaEconomatos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.CantidadRecibida > 0 ? s3.CantidadRecibida : s3.Cantidad))),
                            Ajustes = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEconomatos.Sum(s => s.AjusteEconomatos.Sum(s => s.Ajuste)))),
                            Baja = 0
                        }).ToListAsync();
        }
    
                public async Task<List<DetalleInsumoRes>> ObtenerDetallePT(int idProducto, int idSede)
        {
            var compras = await _context.CompraProductos
                .Include(w => w.Compra)
                .Include(w => w.StockProductoTerminados)
                .Include(w => w.NotaSalidaProductos)
                    .ThenInclude(nsp => nsp.NotaSalida)
                .Where(w => w.IdProducto == idProducto)
                .ToListAsync();

            var resultado = new List<DetalleInsumoRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede)
                {
                    entradasLote += (s.CantidadRecibida ?? s.CantidadSolicitada);
                }
                entradasLote += s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeDestino == idSede)
                    .Sum(nsp => (nsp.CantidadRecibida > 0 ? nsp.CantidadRecibida : nsp.Cantidad));

                decimal salidasNS = s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nsp => (nsp.CantidadRecibida > 0 ? nsp.CantidadRecibida : nsp.Cantidad));

                var stockSede = s.StockProductoTerminados.Where(w => w.IdSede == idSede).ToList();

                decimal saldo = 0m;
                if (stockSede.Any())
                {
                    saldo = stockSede.Sum(sp => sp.StockDisponible);
                }
                else if (entradasLote > 0 || salidasNS > 0)
                {
                    saldo = entradasLote - salidasNS;
                }
                else
                {
                    continue;
                }

                resultado.Add(new DetalleInsumoRes
                {
                    Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaCompra = s.Compra != null ? s.Compra.FechaFactura : null,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.Observacion
                });
            }

            return resultado;
        }

        private async Task<List<StockRes>> ObtenerProductosTerminados(int idSede)
        {
            return await _context.Productos.AsNoTracking()
                .GroupBy(g => new { g.Id })
                .Select(s => new StockRes()
                {
                    Codigo = s.Key.Id + "",
                    Descripcion = s.Select(s => s.Descripcion).FirstOrDefault() ?? "",
                    Um = "UND",
                    Entradas =
                        s.Sum(x => x.CompraProductos!.Where(w => w.Compra.IdSede == idSede).Sum(ci => ci.CantidadRecibida ?? ci.CantidadSolicitada)) +
                        s.Sum(x => x.CompraProductos.Sum(x2 => x2.NotaSalidaProductos.Where(w => w.NotaSalida.IdSedeDestino == idSede).Sum(x3 => x3.CantidadRecibida > 0 ? x3.CantidadRecibida : x3.Cantidad))),
                    Salidas =
                        s.Sum(x => x.CompraProductos.Sum(s2 => s2.NotaSalidaProductos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.CantidadRecibida > 0 ? s3.CantidadRecibida : s3.Cantidad))),
                    Ajustes =
                        s.Sum(x => x.CompraProductos.Where(w => w.Compra.IdSede == idSede).Sum(cp => cp.StockProductoTerminados.Sum(sp => sp.AjusteProductos.Sum(a => a.Ajuste)))),
                    Baja = s.Sum(x => x.CompraProductos
                        .Where(cp => cp.FechaVencimiento < DateTime.UtcNow)
                        .Sum(cp => cp.StockProductoTerminados.Where(w => w.IdSede == idSede).Sum(sp => sp.StockDisponible))),
                    Tipo = null,
                    CodigoUbicacion = null
                }).ToListAsync();
        }

                        public async Task<List<SalidaInsumoRes>> ObtenerSalidasInsumo(int idInsumo, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();

            try
            {
                // 1. Salidas por Productos Intermedios con detalle de stock
                var consumosStockPI = await _context.StockInsumoProductoIntermedios
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                    .Include(x => x.StockInsumo)
                        .ThenInclude(si => si.CompraInsumo)
                    .Where(x => x.InsumoProductoIntermedio.IdInsumo == idInsumo && 
                                x.InsumoProductoIntermedio.ProductoIntermedio.IdSede == idSede)
                    .OrderByDescending(x => x.InsumoProductoIntermedio.ProductoIntermedio.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "ELABORACIÓN PI",
                        RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.InsumoProductoIntermedio.ProductoIntermedio.Id),
                        DescripcionDestino = s.InsumoProductoIntermedio.ProductoIntermedio.Insumo != null 
                            ? s.InsumoProductoIntermedio.ProductoIntermedio.Insumo.Descripcion 
                            : (s.InsumoProductoIntermedio.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                        LoteInsumo = s.StockInsumo != null && s.StockInsumo.CompraInsumo != null ? (s.StockInsumo.CompraInsumo.Lote ?? "") : "",
                        RegistroLoteInsumo = s.StockInsumo != null && s.StockInsumo.CompraInsumo != null ? "MP" + Alfanumerico.ConvertToBase36(s.StockInsumo.CompraInsumo.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = s.UnidadMedida ?? s.InsumoProductoIntermedio.UnidadMedida ?? "G",
                        Fecha = s.InsumoProductoIntermedio.ProductoIntermedio.FechaCreacion,
                        Usuario = s.InsumoProductoIntermedio.ProductoIntermedio.Elaborador != null 
                            ? s.InsumoProductoIntermedio.ProductoIntermedio.Elaborador.Codigo 
                            : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (consumosStockPI != null && consumosStockPI.Any())
                {
                    resultado.AddRange(consumosStockPI);
                }
                else
                {
                    var consumosDirectosPI = await _context.InsumoProductoIntermedios
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                        .Where(x => x.IdInsumo == idInsumo && x.ProductoIntermedio.IdSede == idSede)
                        .OrderByDescending(x => x.ProductoIntermedio.FechaCreacion)
                        .Select(s => new SalidaInsumoRes
                        {
                            TipoSalida = "ELABORACIÓN PI",
                            RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.ProductoIntermedio.Id),
                            DescripcionDestino = s.ProductoIntermedio.Insumo != null 
                                ? s.ProductoIntermedio.Insumo.Descripcion 
                                : (s.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                            LoteInsumo = "",
                            RegistroLoteInsumo = "",
                            Cantidad = s.CantidadLote,
                            Um = s.UnidadMedida ?? "G",
                            Fecha = s.ProductoIntermedio.FechaCreacion,
                            Usuario = s.ProductoIntermedio.Elaborador != null ? s.ProductoIntermedio.Elaborador.Codigo : "ADMIN"
                        })
                        .AsNoTracking()
                        .ToListAsync();

                    if (consumosDirectosPI != null)
                    {
                        resultado.AddRange(consumosDirectosPI);
                    }
                }

                // 2. Salidas por Notas de Salida
                var notasSalida = await _context.NotaSalidaInsumos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraInsumos)
                    .Where(x => x.CompraInsumos != null && x.CompraInsumos.IdInsumo == idInsumo && x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NS-" + Alfanumerico.ConvertToBase36(s.NotaSalida.Id)) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null 
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}" 
                            : "Nota de Salida",
                        LoteInsumo = s.CompraInsumos != null ? (s.CompraInsumos.Lote ?? "") : (s.Lote ?? ""),
                        RegistroLoteInsumo = s.CompraInsumos != null ? "MP" + Alfanumerico.ConvertToBase36(s.CompraInsumos.Id) : "",
                        Cantidad = s.CantidadRecibida ?? s.Cantidad,
                        Um = s.Um ?? (s.CompraInsumos != null ? s.CompraInsumos.Um : "G"),
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }

                // 3. Salidas por Fórmulas Magistrales
                var formulas = await _context.FormulasCC
                    .Include(x => x.Formula)
                        .ThenInclude(f => f.Creador)
                    .Include(x => x.Insumo)
                    .Where(x => x.InsumoId == idInsumo && (x.SedeId == idSede || (x.Formula != null && x.Formula.SedeId == idSede)))
                    .OrderByDescending(x => x.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "FÓRMULA MAGISTRAL",
                        RegistroDestino = s.Formula != null ? ("FM-" + s.Formula.Id) : "FM",
                        DescripcionDestino = s.Formula != null ? (s.Formula.FormulaMagistral ?? ("Fórmula #" + s.Formula.Id)) : "Fórmula",
                        LoteInsumo = s.Formula != null ? (s.Formula.Lote ?? "") : "",
                        RegistroLoteInsumo = "",
                        Cantidad = s.CantidadL,
                        Um = s.Insumo != null ? s.Insumo.UnidadMedida : "G",
                        Fecha = s.Formula != null ? s.Formula.FechaCreacion : s.FechaCreacion,
                        Usuario = s.Formula != null && s.Formula.Creador != null ? s.Formula.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (formulas != null && formulas.Any())
                {
                    resultado.AddRange(formulas);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasInsumo: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }
    }
}

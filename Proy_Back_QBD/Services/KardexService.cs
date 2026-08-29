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
        private readonly ISupplyRepository _repository;
        private readonly IUnitOfWork _unitWork;
        private readonly ILogger<KardexService> _logger;
        public KardexService(
            ApiContext context,
            ISupplyRepository repository,
            IUnitOfWork unitWork,
            ILogger<KardexService> logger)
        {
            _context = context;
            _repository = repository;
            _unitWork = unitWork;
            _logger = logger;
        }

        public async Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int idInsumo, int idSede)
        {
            var compraInsumos = await _context.CompraInsumos
                .Include(w => w.Compra)
                .Include(w => w.StockInsumos)
                    .ThenInclude(si => si.AjusteInsumos)
                .Include(w => w.NotaSalidaInsumos)
                    .ThenInclude(nsi => nsi.NotaSalida)
                .Where(w =>
    w.IdInsumo == idInsumo &&
    (
        (w.Compra != null && w.Compra.IdSede == idSede) ||
        w.NotaSalidaInsumos.Any(nsi =>
            nsi.NotaSalida != null &&
            nsi.NotaSalida.IdSedeDestino == idSede
        )
    )
)

                .ToListAsync();

            var salidasFM = await _context.FormulasCC
                .Where(f => f.InsumoId == idInsumo && f.Formula != null && f.Formula.SedeId == idSede)
                .SumAsync(f => (decimal?)f.CantidadL) ?? 0m;

            var salidasPI = await _context.InsumoProductoIntermedios
                .Where(ipi => ipi.IdInsumo == idInsumo && ipi.ProductoIntermedio != null && ipi.ProductoIntermedio.IdSede == idSede)
                .SumAsync(ipi => (decimal?)ipi.CantidadLote) ?? 0m;

            var resultado = new List<DetalleInsumoRes>();

            _logger.LogInformation(
                "INICIO cálculo de stock de insumos | IdSede: {IdSede} | CantidadCompraInsumos: {CantidadCompraInsumos} | SalidasFM: {SalidasFM} | SalidasPI: {SalidasPI}",
                idSede,
                compraInsumos?.Count() ?? 0,
                salidasFM,
                salidasPI
            );

            foreach (var compraInsumo in compraInsumos)
            {
                _logger.LogInformation(
                    "Procesando CompraInsumo | Id: {CompraInsumoId} | Lote: {Lote} | FechaFabricacion: {FechaFabricacion} | FechaVencimiento: {FechaVencimiento} | CantidadRecibida: {CantidadRecibida} | IdSedeCompra: {IdSedeCompra} | IdSedeConsulta: {IdSede}",
                    compraInsumo.Id,
                    compraInsumo.Lote,
                    compraInsumo.FechaFabricacion,
                    compraInsumo.FechaVencimiento,
                    compraInsumo.CantidadRecibida,
                    compraInsumo.Compra?.IdSede,
                    idSede
                );

                decimal entradasCompra = 0m;
                decimal entradasTraslado = 0m;

                // ============================================================
                // ENTRADA POR COMPRA
                // ============================================================

                if (compraInsumo.Compra != null &&
                    compraInsumo.Compra.IdSede == idSede)
                {
                    entradasCompra = compraInsumo.CantidadRecibida ?? 0m;
                }

                _logger.LogInformation(
                    "Entrada por compra | CompraInsumoId: {CompraInsumoId} | IdSedeCompra: {IdSedeCompra} | IdSedeConsulta: {IdSede} | CantidadRecibida: {CantidadRecibida} | EntradasCompra: {EntradasCompra}",
                    compraInsumo.Id,
                    compraInsumo.Compra?.IdSede,
                    idSede,
                    compraInsumo.CantidadRecibida,
                    entradasCompra
                );

                // ============================================================
                // ENTRADAS POR NOTAS DE SALIDA RECIBIDAS EN LA SEDE
                // ============================================================

                var notasSalidaDestino = compraInsumo.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida != null &&
                        nsi.NotaSalida.IdSedeDestino == idSede)
                    .ToList();

                foreach (var nsi in notasSalidaDestino)
                {
                    var cantidadEntrada = nsi.CantidadRecibida ?? nsi.Cantidad;

                    _logger.LogInformation(
                        "Entrada por traslado | CompraInsumoId: {CompraInsumoId} | NotaSalidaInsumoId: {NotaSalidaInsumoId} | NotaSalidaId: {NotaSalidaId} | SedeOrigen: {SedeOrigen} | SedeDestino: {SedeDestino} | Cantidad: {Cantidad} | CantidadRecibida: {CantidadRecibida} | EntradaConsiderada: {EntradaConsiderada}",
                        compraInsumo.Id,
                        nsi.Id,
                        nsi.NotaSalida?.Id,
                        nsi.NotaSalida?.IdSedeOrigen,
                        nsi.NotaSalida?.IdSedeDestino,
                        nsi.Cantidad,
                        nsi.CantidadRecibida,
                        cantidadEntrada
                    );
                }

                entradasTraslado = notasSalidaDestino
                    .Sum(nsi => nsi.CantidadRecibida ?? nsi.Cantidad);

                decimal entradas = entradasCompra + entradasTraslado;

                _logger.LogInformation(
                    "TOTAL ENTRADAS | CompraInsumoId: {CompraInsumoId} | EntradasCompra: {EntradasCompra} | EntradasTraslado: {EntradasTraslado} | EntradasTotal: {EntradasTotal}",
                    compraInsumo.Id,
                    entradasCompra,
                    entradasTraslado,
                    entradas
                );

                // ============================================================
                // SALIDAS POR NOTAS DE SALIDA
                // ============================================================

                var notasSalidaOrigen = compraInsumo.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida != null &&
                        nsi.NotaSalida.IdSedeOrigen == idSede)
                    .ToList();

                foreach (var nsi in notasSalidaOrigen)
                {
                    _logger.LogInformation(
                        "Salida por traslado | CompraInsumoId: {CompraInsumoId} | NotaSalidaInsumoId: {NotaSalidaInsumoId} | NotaSalidaId: {NotaSalidaId} | SedeOrigen: {SedeOrigen} | SedeDestino: {SedeDestino} | Cantidad: {Cantidad}",
                        compraInsumo.Id,
                        nsi.Id,
                        nsi.NotaSalida?.Id,
                        nsi.NotaSalida?.IdSedeOrigen,
                        nsi.NotaSalida?.IdSedeDestino,
                        nsi.Cantidad
                    );
                }

                decimal salidasNS = notasSalidaOrigen
                    .Sum(nsi => nsi.Cantidad);

                _logger.LogInformation(
                    "TOTAL SALIDAS NOTAS DE SALIDA | CompraInsumoId: {CompraInsumoId} | SalidasNS: {SalidasNS}",
                    compraInsumo.Id,
                    salidasNS
                );

                // ============================================================
                // SALIDAS LOCALES
                // ============================================================

                decimal salidasLocales = salidasFM + salidasPI;

                _logger.LogInformation(
                    "SALIDAS LOCALES | CompraInsumoId: {CompraInsumoId} | SalidasFM: {SalidasFM} | SalidasPI: {SalidasPI} | SalidasLocales: {SalidasLocales}",
                    compraInsumo.Id,
                    salidasFM,
                    salidasPI,
                    salidasLocales
                );

                // ============================================================
                // AJUSTES
                // ============================================================

                var stockInsumosSede = compraInsumo.StockInsumos
                    .Where(si => si.IdSede == idSede)
                    .ToList();

                decimal ajustes = 0m;

                foreach (var stockInsumo in stockInsumosSede)
                {
                    var ajustesStock = stockInsumo.AjusteInsumos?.ToList() ?? new List<AjusteInsumo>();

                    foreach (var ajuste in ajustesStock)
                    {
                        _logger.LogInformation(
                            "Ajuste de stock | CompraInsumoId: {CompraInsumoId} | StockInsumoId: {StockInsumoId} | AjusteInsumoId: {AjusteInsumoId} | IdSede: {IdSede} | Ajuste: {Ajuste}",
                            compraInsumo.Id,
                            stockInsumo.Id,
                            ajuste.Id,
                            stockInsumo.IdSede,
                            ajuste.Ajuste
                        );
                    }

                    var totalAjustesStock = ajustesStock.Sum(a => a.Ajuste);

                    _logger.LogInformation(
                        "Total ajustes por StockInsumo | CompraInsumoId: {CompraInsumoId} | StockInsumoId: {StockInsumoId} | IdSede: {IdSede} | TotalAjustes: {TotalAjustes}",
                        compraInsumo.Id,
                        stockInsumo.Id,
                        stockInsumo.IdSede,
                        totalAjustesStock
                    );

                    ajustes += totalAjustesStock;
                }

                _logger.LogInformation(
                    "TOTAL AJUSTES | CompraInsumoId: {CompraInsumoId} | IdSede: {IdSede} | Ajustes: {Ajustes}",
                    compraInsumo.Id,
                    idSede,
                    ajustes
                );

                // ============================================================
                // BAJAS POR VENCIMIENTO
                // ============================================================

                bool estaVencido = compraInsumo.FechaVencimiento < DateTime.UtcNow;

                decimal bajas = 0m;

                if (estaVencido)
                {
                    foreach (var stockInsumo in stockInsumosSede)
                    {
                        _logger.LogInformation(
                            "Baja por vencimiento | CompraInsumoId: {CompraInsumoId} | StockInsumoId: {StockInsumoId} | IdSede: {IdSede} | StockDisponible: {StockDisponible} | FechaVencimiento: {FechaVencimiento}",
                            compraInsumo.Id,
                            stockInsumo.Id,
                            stockInsumo.IdSede,
                            stockInsumo.StockDisponible,
                            compraInsumo.FechaVencimiento
                        );
                    }

                    bajas = stockInsumosSede
                        .Sum(si => si.StockDisponible);
                }

                _logger.LogInformation(
                    "BAJAS | CompraInsumoId: {CompraInsumoId} | EstaVencido: {EstaVencido} | FechaVencimiento: {FechaVencimiento} | Bajas: {Bajas}",
                    compraInsumo.Id,
                    estaVencido,
                    compraInsumo.FechaVencimiento,
                    bajas
                );

                // ============================================================
                // CÁLCULO FINAL
                // ============================================================

                decimal saldo =
                    entradas
                    - salidasNS
                    - salidasLocales
                    + ajustes
                    - bajas;

                _logger.LogInformation(
                    "CÁLCULO SALDO | CompraInsumoId: {CompraInsumoId} | " +
                    "Entradas: {Entradas} | " +
                    "SalidasNS: {SalidasNS} | " +
                    "SalidasLocales: {SalidasLocales} | " +
                    "Ajustes: {Ajustes} | " +
                    "Bajas: {Bajas} | " +
                    "Saldo: {Saldo}",
                    compraInsumo.Id,
                    entradas,
                    salidasNS,
                    salidasLocales,
                    ajustes,
                    bajas,
                    saldo
                );

                // ============================================================
                // VALIDACIÓN DE OMISIÓN
                // ============================================================

                if (entradas == 0 && salidasNS == 0 && saldo == 0)
                {
                    _logger.LogInformation(
                        "CompraInsumo OMITIDO | CompraInsumoId: {CompraInsumoId} | " +
                        "Motivo: Entradas = 0, SalidasNS = 0 y Saldo = 0 | " +
                        "Entradas: {Entradas} | SalidasNS: {SalidasNS} | Saldo: {Saldo}",
                        compraInsumo.Id,
                        entradas,
                        salidasNS,
                        saldo
                    );

                    continue;
                }

                // ============================================================
                // AGREGAR RESULTADO
                // ============================================================

                var registro = "MP" +
                               Alfanumerico.ConvertToBase36(compraInsumo.Id);

                var detalle = new DetalleInsumoRes
                {
                    Registro = registro,
                    Lote = compraInsumo.Lote ?? "",
                    Saldo = saldo,
                    FechaCompra = compraInsumo.Compra != null
                        ? compraInsumo.Compra.FechaFactura
                        : null,
                    FechaFabricacion = compraInsumo.FechaFabricacion,
                    FechaVencimiento = compraInsumo.FechaVencimiento,
                    Observacion = compraInsumo.Observacion
                };

                resultado.Add(detalle);

                _logger.LogInformation(
                    "RESULTADO AGREGADO | CompraInsumoId: {CompraInsumoId} | Registro: {Registro} | Lote: {Lote} | Saldo: {Saldo} | FechaCompra: {FechaCompra} | FechaFabricacion: {FechaFabricacion} | FechaVencimiento: {FechaVencimiento} | Observacion: {Observacion}",
                    compraInsumo.Id,
                    registro,
                    compraInsumo.Lote,
                    saldo,
                    detalle.FechaCompra,
                    detalle.FechaFabricacion,
                    detalle.FechaVencimiento,
                    detalle.Observacion
                );
            }

            _logger.LogInformation(
                "FIN cálculo de stock de insumos | IdSede: {IdSede} | CantidadResultados: {CantidadResultados}",
                idSede,
                resultado.Count
            );


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
                    .Sum(nse => nse.Cantidad);

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
                    .Sum(nsp => nsp.Cantidad);

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
        // Fusionar 2 listas para entradas compras y notas de salida
        private async Task<List<StockRes>> ObtenerMateriaPrima(int idSede)
        {
            _logger.LogInformation(
                "INICIO ObtenerMateriaPrima | IdSede: {IdSede}",
                idSede
            );

            // ============================================================
            // 1. OBTENER INSUMOS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 1: Obteniendo insumos de materia prima | IdSede: {IdSede}",
                idSede
            );

            var insumos = await _context.Insumos
                .AsNoTracking()
                .Where(i => i.Clasificacion == "MP" || i.Clasificacion == null)
                .Select(i => new
                {
                    i.Id,
                    i.Descripcion,
                    i.UnidadMedida,
                    i.Tipo
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 1 FINALIZADA | IdSede: {IdSede} | CantidadInsumos: {CantidadInsumos}",
                idSede,
                insumos.Count
            );

            if (!insumos.Any())
            {
                _logger.LogWarning(
                    "No se encontraron insumos de materia prima | IdSede: {IdSede}",
                    idSede
                );

                return new List<StockRes>();
            }

            var insumoIds = insumos
                .Select(i => i.Id)
                .ToList();

            // ============================================================
            // 2. OBTENER ENTRADAS POR COMPRAS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 2: Obteniendo entradas por compras | IdSede: {IdSede}",
                idSede
            );

            var entradasCompra = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.Compra.IdSede == idSede)
                .GroupBy(ci => ci.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(ci => ci.CantidadRecibida ?? 0m)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 2 FINALIZADA | IdSede: {IdSede} | InsumosConEntradasCompra: {Cantidad}",
                idSede,
                entradasCompra.Count
            );

            foreach (var item in entradasCompra)
            {
                _logger.LogDebug(
                    "Entrada compra | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 3. ENTRADAS POR NOTAS DE SALIDA RECIBIDAS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 3: Obteniendo entradas por notas de salida recibidas | IdSede: {IdSede}",
                idSede
            );

            var entradasNotaSalida = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.NotaSalidaInsumos.Any(nsi =>
                        nsi.NotaSalida.IdSedeDestino == idSede))
                .SelectMany(ci => ci.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida.IdSedeDestino == idSede)
                    .Select(nsi => new
                    {
                        ci.IdInsumo,
                        Cantidad = nsi.CantidadRecibida ?? nsi.Cantidad
                    }))
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.Cantidad)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 3 FINALIZADA | IdSede: {IdSede} | InsumosConEntradasNS: {Cantidad}",
                idSede,
                entradasNotaSalida.Count
            );

            foreach (var item in entradasNotaSalida)
            {
                _logger.LogDebug(
                    "Entrada nota salida | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 4. SALIDAS A PRODUCTO INTERMEDIO
            // ============================================================

            _logger.LogInformation(
                "Solicitud 4: Obteniendo salidas a Producto Intermedio | IdSede: {IdSede}",
                idSede
            );

            var salidasProductoIntermedio = await _context.InsumoProductoIntermedios
                .AsNoTracking()
                .Where(x =>
                    insumoIds.Contains(x.IdInsumo) &&
                    x.ProductoIntermedio.IdSede == idSede)
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.CantidadLote)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 4 FINALIZADA | IdSede: {IdSede} | InsumosConSalidasPI: {Cantidad}",
                idSede,
                salidasProductoIntermedio.Count
            );

            foreach (var item in salidasProductoIntermedio)
            {
                _logger.LogDebug(
                    "Salida Producto Intermedio | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 5. SALIDAS A FORMULAS MAGISTRALES
            // ============================================================

            _logger.LogInformation(
                "Solicitud 5: Obteniendo salidas a Fórmulas Magistrales | IdSede: {IdSede}",
                idSede
            );

            var salidasFormula = await _context.FormulasCC
                .AsNoTracking()
                .Where(x =>
                    insumoIds.Contains(x.InsumoId) &&
                    x.Formula.SedeId == idSede)
                .GroupBy(x => x.InsumoId)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.CantidadL)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 5 FINALIZADA | IdSede: {IdSede} | InsumosConSalidasFormula: {Cantidad}",
                idSede,
                salidasFormula.Count
            );

            foreach (var item in salidasFormula)
            {
                _logger.LogDebug(
                    "Salida Fórmula Magistral | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 6. SALIDAS POR NOTAS DE SALIDA DESPACHADAS
            // ============================================================

            _logger.LogInformation(
                "Solicitud 6: Obteniendo salidas por notas de salida | IdSede: {IdSede}",
                idSede
            );

            var salidasNotaSalida = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.NotaSalidaInsumos.Any(nsi =>
                        nsi.NotaSalida.IdSedeOrigen == idSede))
                .SelectMany(ci => ci.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida.IdSedeOrigen == idSede)
                    .Select(nsi => new
                    {
                        ci.IdInsumo,
                        nsi.Cantidad
                    }))
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.Cantidad)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 6 FINALIZADA | IdSede: {IdSede} | InsumosConSalidasNS: {Cantidad}",
                idSede,
                salidasNotaSalida.Count
            );

            foreach (var item in salidasNotaSalida)
            {
                _logger.LogDebug(
                    "Salida NotaSalida | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 7. AJUSTES
            // ============================================================

            _logger.LogInformation(
                "Solicitud 7: Obteniendo ajustes de stock | IdSede: {IdSede}",
                idSede
            );

            var ajustes = await _context.AjusteInsumos
                .AsNoTracking()
                .Where(a =>
                    a.StockInsumo.IdSede == idSede &&
                    insumoIds.Contains(a.StockInsumo.CompraInsumo.IdInsumo))
                .GroupBy(a => a.StockInsumo.CompraInsumo.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(a => a.Ajuste)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 7 FINALIZADA | IdSede: {IdSede} | InsumosConAjustes: {Cantidad}",
                idSede,
                ajustes.Count
            );

            foreach (var item in ajustes)
            {
                _logger.LogDebug(
                    "Ajuste | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 8. BAJAS POR VENCIMIENTO
            // ============================================================

            var ahora = DateTime.UtcNow;

            _logger.LogInformation(
                "Solicitud 8: Obteniendo bajas por vencimiento | IdSede: {IdSede} | FechaActualUTC: {FechaActualUTC}",
                idSede,
                ahora
            );

            var bajas = await _context.CompraInsumos
                .AsNoTracking()
                .Where(ci =>
                    insumoIds.Contains(ci.IdInsumo) &&
                    ci.FechaVencimiento < ahora &&
                    ci.StockInsumos.Any(si => si.IdSede == idSede))
                .SelectMany(ci => ci.StockInsumos
                    .Where(si => si.IdSede == idSede)
                    .Select(si => new
                    {
                        ci.IdInsumo,
                        si.StockDisponible
                    }))
                .GroupBy(x => x.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(x => x.StockDisponible)
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 8 FINALIZADA | IdSede: {IdSede} | InsumosConBajas: {Cantidad}",
                idSede,
                bajas.Count
            );

            foreach (var item in bajas)
            {
                _logger.LogDebug(
                    "Baja vencimiento | IdInsumo: {IdInsumo} | Cantidad: {Cantidad}",
                    item.IdInsumo,
                    item.Total
                );
            }

            // ============================================================
            // 9. UBICACIONES
            // ============================================================

            _logger.LogInformation(
                "Solicitud 9: Obteniendo ubicaciones | IdSede: {IdSede}",
                idSede
            );

            var ubicaciones = await _context.SiteSupplies
                .AsNoTracking()
                .Where(x =>
                    x.IdSite == idSede &&
                    insumoIds.Contains(x.IdSupply))
                .Select(x => new
                {
                    x.IdSupply,
                    x.Location
                })
                .ToListAsync();

            _logger.LogInformation(
                "Solicitud 9 FINALIZADA | IdSede: {IdSede} | Ubicaciones: {Cantidad}",
                idSede,
                ubicaciones.Count
            );

            // ============================================================
            // 10. ARMAR RESULTADO
            // ============================================================

            _logger.LogInformation(
                "Armando resultado final | IdSede: {IdSede} | Insumos: {Cantidad}",
                idSede,
                insumos.Count
            );

            var resultado = new List<StockRes>();

            foreach (var insumo in insumos)
            {
                var entradaCompra = entradasCompra
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var entradaNS = entradasNotaSalida
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var salidaPI = salidasProductoIntermedio
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var salidaFormula = salidasFormula
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var salidaNS = salidasNotaSalida
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var ajuste = ajustes
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var baja = bajas
                    .FirstOrDefault(x => x.IdInsumo == insumo.Id)?.Total ?? 0m;

                var entradas = entradaCompra + entradaNS;

                var salidas = salidaPI + salidaFormula + salidaNS;

                var saldo = entradas - salidas + ajuste - baja;

                var ubicacion = ubicaciones
                    .FirstOrDefault(x => x.IdSupply == insumo.Id)?.Location;

                _logger.LogInformation(
                    "Cálculo Insumo | IdSede: {IdSede} | IdInsumo: {IdInsumo} | Descripcion: {Descripcion} | " +
                    "EntradaCompra: {EntradaCompra} | EntradaNS: {EntradaNS} | EntradasTotal: {Entradas} | " +
                    "SalidaPI: {SalidaPI} | SalidaFormula: {SalidaFormula} | SalidaNS: {SalidaNS} | SalidasTotal: {Salidas} | " +
                    "Ajuste: {Ajuste} | Baja: {Baja} | Saldo: {Saldo} | Ubicacion: {Ubicacion}",
                    idSede,
                    insumo.Id,
                    insumo.Descripcion,
                    entradaCompra,
                    entradaNS,
                    entradas,
                    salidaPI,
                    salidaFormula,
                    salidaNS,
                    salidas,
                    ajuste,
                    baja,
                    saldo,
                    ubicacion
                );

                resultado.Add(new StockRes
                {
                    Codigo = insumo.Id.ToString(),
                    Descripcion = insumo.Descripcion ?? "",
                    Um = insumo.UnidadMedida ?? string.Empty,
                    Entradas = entradas,
                    Salidas = salidas,
                    Ajustes = ajuste,
                    Baja = baja,
                    Tipo = insumo.Tipo,
                    CodigoUbicacion = ubicacion
                });
            }

            _logger.LogInformation(
                "FIN ObtenerMateriaPrima | IdSede: {IdSede} | CantidadResultados: {CantidadResultados}",
                idSede,
                resultado.Count
            );

            return resultado;
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
                CodigoUbicacion = s.Select(s => s.SiteSupply.Where(w => w.IdSite == idSede).Select(s => s.Location).FirstOrDefault()).FirstOrDefault()
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
                            s.Sum(s => s.CompraEmpaques.Sum(s2 => s2.NotaSalidaEmpaques.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.Cantidad))),
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
                            Salidas = s.Sum(s => s.CompraEconomatos.Sum(s2 => s2.NotaSalidaEconomatos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.Cantidad))),
                            Ajustes = s.Sum(s => s.CompraEconomatos.Where(w => w.Compra.IdSede == idSede).Sum(s => s.StockEconomatos.Sum(s => s.AjusteEconomatos.Sum(s => s.Ajuste)))),
                            Baja = 0
                        }).ToListAsync();
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
                        s.Sum(x => x.CompraProductos.Sum(s2 => s2.NotaSalidaProductos.Where(w => w.NotaSalida.IdSedeOrigen == idSede).Sum(s3 => s3.Cantidad))),
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
                        Cantidad = s.Cantidad,
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
        public async Task<SiteSupply> AssignLocation(AssignLocationReq request)
        {
            SiteSupply? siteSupply = await _repository.GetSedeSupplyAsync(request.IdInsumo, request.IdSede);

            if (siteSupply == null)
            {
                siteSupply = new SiteSupply
                {
                    IdSite = request.IdSede,
                    IdSupply = request.IdInsumo,
                    Location = request.Ubicacion
                };

                siteSupply = await _repository.CreateLocationBySiteAsync(siteSupply);
            }
            else
            {
                siteSupply.Location = request.Ubicacion;
            }

            await _unitWork.SaveChangesAsync();

            return siteSupply;
        }

    }
}

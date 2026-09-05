using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Ajuste;
using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;

namespace Proy_back_QBD.Service.AjusteService
{
    public class AjusteService : IAjusteService
    {
        private readonly ApiContext _context;
        private static readonly List<string> FamiliasAptas = ["MP", "ME", "PT", "ECO"];
        public AjusteService(ApiContext context)
        {
            _context = context;
        }

        public async Task<List<TablaAjustesRes>> ListaAjustes(string familia, int idSede)
        {
            // Gestión de Inv / Ajuste debe mostrar única y exclusivamente el inventario de CENTRAL (no de otras sedes)
            var centralSede = await _context.Sedes.FirstOrDefaultAsync(s => s.Nombre.ToUpper().Contains("CENTRAL") || s.Id == 15);
            int idSedeCentral = centralSede?.Id ?? 15;

            List<TablaAjustesRes> Response = familia switch
            {
                "MP" => await ObtenerMateriaPrima(idSedeCentral),
                "ME" => await ObtenerMateriaEmpaques(idSedeCentral),
                "PT" => await ObtenerProductosTerminados(idSedeCentral),
                "ECO" => await ObtenerEconomatos(idSedeCentral),
                _ => throw new BadRequestException("Familia no Apta")
            };

            return Response;
        }

        public async Task RegistrarAjuste(CrearAjusteReq request)
        {
            string familia = request.Familia;
            int idCreador = request.IdCreador;
            if (FamiliasAptas.Contains(familia))
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    List<CrearAjustes> listaAjustes = request.ListaAjustes;

                    switch (familia)
                    {
                        case "MP":
                            await StrategyCrearAjusteInsumo(listaAjustes, idCreador); break;
                        case "ME":
                            await StrategyCrearAjusteEmpaque(listaAjustes, idCreador); break;
                        case "ECO":
                            await StrategyCrearAjusteEconomato(listaAjustes, idCreador); break;
                        case "PT":
                            await StrategyCrearAjusteProductoTerminado(listaAjustes, idCreador); break;
                        default: throw new BadRequestException("Familia no apta");
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw new ServerException("Ocurrió un error al crear el ajuste.", e);
                }
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }
        }

        public async Task<List<DetalleAjusteRes>> DetalleAjuste(int registroId, string familia)
        {
            if (FamiliasAptas.Contains(familia))
            {
                List<DetalleAjusteRes> response = new();
                if (familia == "MP")
                {
                    response = await _context.AjusteInsumos
                    .AsNoTracking()
                    .Where(w => w.StockInsumo != null && w.StockInsumo.IdCompraInsumo == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion
                    }).ToListAsync();
                }
                if (familia == "ME")
                {
                    response = await _context.AjusteEmpaques
                    .AsNoTracking()
                    .Where(w => w.StockEmpaque != null && w.StockEmpaque.IdCompraEmpaque == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion ?? ""
                    }).ToListAsync();
                }
                if (familia == "PT")
                {
                    response = await _context.AjusteProductoTerminados
                    .AsNoTracking()
                    .Where(w => w.StockProducto != null && w.StockProducto.IdCompraProducto == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion ?? ""
                    }).ToListAsync();
                }
                if (familia == "ECO")
                {
                    response = await _context.AjusteEconomatos
                    .AsNoTracking()
                    .Where(w => w.StockEconomato != null && w.StockEconomato.IdCompraEconomato == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion ?? ""
                    }).ToListAsync();
                }
                return response;
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }
        }

        // LISTA AJUSTE PRINCIPAL - INCLUYE TODAS LAS COMPRAS Y ENTRADAS POR NOTA DE SALIDA
        private async Task<List<TablaAjustesRes>> ObtenerMateriaPrima(int idSede)
        {
            var compras = await _context.CompraInsumos
                .Include(ci => ci.Insumo)
                .Include(ci => ci.Compra)
                .Include(ci => ci.StockInsumos)
                    .ThenInclude(si => si.AjusteInsumos)
                .Include(ci => ci.NotaSalidaInsumos)
                    .ThenInclude(nsi => nsi.NotaSalida)
                .Where(w =>
                    (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                    w.NotaSalidaInsumos.Any(nsi =>
                        nsi.NotaSalida != null &&
                        nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0))
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var salidasFM = await _context.FormulasCC
                .Where(f => f.Formula != null && f.Formula.SedeId == idSede)
                .GroupBy(f => f.InsumoId)
                .Select(g => new { IdInsumo = g.Key, Total = g.Sum(f => f.CantidadL) })
                .ToDictionaryAsync(x => x.IdInsumo, x => x.Total);

            var salidasPI = await _context.InsumoProductoIntermedios
                .Where(ipi => ipi.ProductoIntermedio != null && ipi.ProductoIntermedio.IdSede == idSede)
                .GroupBy(ipi => ipi.IdInsumo)
                .Select(g => new { IdInsumo = g.Key, Total = g.Sum(ipi => ipi.CantidadLote) })
                .ToDictionaryAsync(x => x.IdInsumo, x => x.Total);

            var insumoGrupos = compras.GroupBy(c => c.IdInsumo);
            var resultado = new List<TablaAjustesRes>();

            foreach (var grupo in insumoGrupos)
            {
                int idInsumo = grupo.Key;
                decimal salidasPendientes = (salidasFM.ContainsKey(idInsumo) ? salidasFM[idInsumo] : 0m) +
                                            (salidasPI.ContainsKey(idInsumo) ? salidasPI[idInsumo] : 0m);

                foreach (var compraInsumo in grupo)
                {
                    decimal entradasCompra = 0m;
                    if (compraInsumo.Compra != null && compraInsumo.Compra.IdSede == idSede && (idSede == 15 || compraInsumo.Compra.FechaLab != null))
                    {
                        entradasCompra = (compraInsumo.CantidadRecibida.HasValue && compraInsumo.CantidadRecibida.Value > 0) ? compraInsumo.CantidadRecibida.Value : compraInsumo.CantidadSolicitada;
                    }

                    var notasSalidaDestino = compraInsumo.NotaSalidaInsumos
                        .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede &&
                            (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                        .ToList();

                    decimal entradasTraslado = notasSalidaDestino
                        .Sum(nsi => ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad));

                    decimal entradas = entradasCompra + entradasTraslado;

                    var notasSalidaOrigen = compraInsumo.NotaSalidaInsumos
                        .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeOrigen == idSede)
                        .ToList();

                    decimal salidasNS = notasSalidaOrigen
                        .Sum(nsi => ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * nsi.Cantidad);

                    var stockInsumosSede = compraInsumo.StockInsumos
                        .Where(si => si.IdSede == idSede)
                        .ToList();

                    decimal ajustes = 0m;
                    string observacionAjuste = "";
                    foreach (var stockInsumo in stockInsumosSede)
                    {
                        var ajustesStock = stockInsumo.AjusteInsumos?.OrderByDescending(a => a.FechaCreacion).ToList() ?? new List<AjusteInsumo>();
                        ajustes += ajustesStock.Sum(a => a.Ajuste);
                        if (string.IsNullOrEmpty(observacionAjuste) && ajustesStock.Any())
                        {
                            observacionAjuste = ajustesStock.First().Observacion ?? "";
                        }
                    }

                    bool estaVencido = compraInsumo.FechaVencimiento < DateTime.UtcNow;
                    decimal bajas = 0m;
                    if (estaVencido)
                    {
                        bajas = stockInsumosSede.Sum(si => si.StockDisponible);
                    }

                    decimal saldoBruto = entradas - salidasNS + ajustes - bajas;
                    decimal descuentoLocal = 0m;
                    if (salidasPendientes > 0 && saldoBruto > 0)
                    {
                        if (saldoBruto >= salidasPendientes)
                        {
                            descuentoLocal = salidasPendientes;
                            salidasPendientes = 0;
                        }
                        else
                        {
                            descuentoLocal = saldoBruto;
                            salidasPendientes -= saldoBruto;
                        }
                    }

                    decimal saldo = entradas - salidasNS - descuentoLocal + ajustes - bajas;
                    if (saldo < 0) saldo = 0;

                    resultado.Add(new TablaAjustesRes
                    {
                        Codigo = UtilFamilia.CodigoInsumo(compraInsumo.IdInsumo),
                        Registro = "MP" + Alfanumerico.ConvertToBase36(compraInsumo.Id),
                        Descripcion = compraInsumo.Insumo?.Descripcion ?? "",
                        Lote = compraInsumo.Lote ?? "",
                        Saldo = saldo,
                        FechaFabricacion = compraInsumo.FechaFabricacion,
                        FechaVencimiento = compraInsumo.FechaVencimiento,
                        Clasificacion = compraInsumo.Insumo?.Clasificacion ?? "MP",
                        Observacion = observacionAjuste
                    });
                }
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        private async Task<List<TablaAjustesRes>> ObtenerMateriaEmpaques(int idSede)
        {
            var compras = await _context.CompraEmpaques
                .Include(ce => ce.Empaque)
                .Include(ce => ce.Compra)
                .Include(ce => ce.StockEmpaques)
                    .ThenInclude(se => se.AjusteEmpaques)
                .Include(ce => ce.NotaSalidaEmpaques)
                    .ThenInclude(nse => nse.NotaSalida)
                .Where(w =>
                    (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                    w.NotaSalidaEmpaques.Any(nse =>
                        nse.NotaSalida != null &&
                        nse.NotaSalida.IdSedeDestino == idSede &&
                        (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0))
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var resultado = new List<TablaAjustesRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede && (idSede == 15 || s.Compra.FechaLab != null))
                {
                    entradasLote += (s.CantidadRecibida.HasValue && s.CantidadRecibida.Value > 0 ? s.CantidadRecibida.Value : s.CantidadSolicitada);
                }
                entradasLote += s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede && (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0)))
                    .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad));

                decimal salidasNS = s.NotaSalidaEmpaques
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nse => nse.Cantidad);

                var stockSede = s.StockEmpaques.Where(w => w.IdSede == idSede).ToList();
                decimal ajustes = 0m;
                string observacionAjuste = "";
                foreach (var se in stockSede)
                {
                    var ajList = se.AjusteEmpaques?.OrderByDescending(a => a.FechaCreacion).ToList() ?? new List<AjusteEmpaque>();
                    ajustes += ajList.Sum(a => a.Ajuste);
                    if (string.IsNullOrEmpty(observacionAjuste) && ajList.Any())
                    {
                        observacionAjuste = ajList.First().Observacion ?? "";
                    }
                }

                decimal saldo = 0m;
                if (stockSede.Any())
                {
                    saldo = stockSede.Sum(se => se.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS + ajustes;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoEmpaque(s.IdEmpaque),
                    Registro = "ME" + Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Empaque?.Descripcion ?? "",
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Clasificacion = "ME",
                    Observacion = observacionAjuste
                });
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        private async Task<List<TablaAjustesRes>> ObtenerEconomatos(int idSede)
        {
            var compras = await _context.CompraEconomatos
                .Include(ce => ce.Economato)
                .Include(ce => ce.Compra)
                .Include(ce => ce.StockEconomatos)
                    .ThenInclude(se => se.AjusteEconomatos)
                .Include(ce => ce.NotaSalidaEconomatos)
                    .ThenInclude(nse => nse.NotaSalida)
                .Where(w =>
                    (w.Compra != null && w.Compra.IdSede == idSede) ||
                    w.NotaSalidaEconomatos.Any(nse =>
                        nse.NotaSalida != null &&
                        nse.NotaSalida.IdSedeDestino == idSede &&
                        (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0))
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var resultado = new List<TablaAjustesRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede)
                {
                    entradasLote += s.CantidadSolicitada;
                }
                entradasLote += s.NotaSalidaEconomatos
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede && (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || (nse.CantidadRecibida > 0)))
                    .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad));

                decimal salidasNS = s.NotaSalidaEconomatos
                    .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nse => nse.Cantidad);

                var stockSede = s.StockEconomatos.Where(w => w.IdSede == idSede).ToList();
                decimal ajustes = 0m;
                string observacionAjuste = "";
                foreach (var se in stockSede)
                {
                    var ajList = se.AjusteEconomatos?.OrderByDescending(a => a.FechaCreacion).ToList() ?? new List<AjusteEconomato>();
                    ajustes += ajList.Sum(a => a.Ajuste);
                    if (string.IsNullOrEmpty(observacionAjuste) && ajList.Any())
                    {
                        observacionAjuste = ajList.First().Observacion ?? "";
                    }
                }

                decimal saldo = 0m;
                if (stockSede.Any())
                {
                    saldo = stockSede.Sum(se => se.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS + ajustes;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdEconomato),
                    Registro = "ECO" + Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Economato?.Descripcion ?? "",
                    Lote = "",
                    Saldo = saldo,
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Clasificacion = "ECO",
                    Observacion = observacionAjuste
                });
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        private async Task<List<TablaAjustesRes>> ObtenerProductosTerminados(int idSede)
        {
            var compras = await _context.CompraProductos
                .Include(cp => cp.Producto)
                .Include(cp => cp.Compra)
                .Include(cp => cp.StockProductoTerminados)
                    .ThenInclude(sp => sp.AjusteProductos)
                .Include(cp => cp.NotaSalidaProductos)
                    .ThenInclude(nsp => nsp.NotaSalida)
                .Where(w =>
                    (w.Compra != null && w.Compra.IdSede == idSede) ||
                    w.NotaSalidaProductos.Any(nsp =>
                        nsp.NotaSalida != null &&
                        nsp.NotaSalida.IdSedeDestino == idSede &&
                        (nsp.NotaSalida.Estado == "RECIBIDO" || nsp.NotaSalida.Estado == "RECEPCIONADO" || nsp.NotaSalida.FechaRecepcion != null || (nsp.CantidadRecibida > 0))
                    )
                )
                .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                .ToListAsync();

            var resultado = new List<TablaAjustesRes>();

            foreach (var s in compras)
            {
                decimal entradasLote = 0m;
                if (s.Compra != null && s.Compra.IdSede == idSede)
                {
                    entradasLote += (s.CantidadRecibida.HasValue && s.CantidadRecibida.Value > 0 ? s.CantidadRecibida.Value : s.CantidadSolicitada);
                }
                entradasLote += s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeDestino == idSede && (nsp.NotaSalida.Estado == "RECIBIDO" || nsp.NotaSalida.Estado == "RECEPCIONADO" || nsp.NotaSalida.FechaRecepcion != null || (nsp.CantidadRecibida > 0)))
                    .Sum(nsp => (nsp.CantidadRecibida > 0 ? nsp.CantidadRecibida : nsp.Cantidad));

                decimal salidasNS = s.NotaSalidaProductos
                    .Where(nsp => nsp.NotaSalida != null && nsp.NotaSalida.IdSedeOrigen == idSede)
                    .Sum(nsp => nsp.Cantidad);

                var stockSede = s.StockProductoTerminados.Where(w => w.IdSede == idSede).ToList();
                decimal ajustes = 0m;
                string observacionAjuste = "";
                foreach (var sp in stockSede)
                {
                    var ajList = sp.AjusteProductos?.OrderByDescending(a => a.FechaCreacion).ToList() ?? new List<AjusteProducto>();
                    ajustes += ajList.Sum(a => a.Ajuste);
                    if (string.IsNullOrEmpty(observacionAjuste) && ajList.Any())
                    {
                        observacionAjuste = ajList.First().Observacion ?? "";
                    }
                }

                decimal saldo = 0m;
                if (stockSede.Any())
                {
                    saldo = stockSede.Sum(sp => sp.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS + ajustes;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdProducto),
                    Registro = "PT" + Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Producto?.Descripcion ?? "",
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Clasificacion = "PT",
                    Observacion = observacionAjuste
                });
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        // REGISTRAR AJUSTES
        private async Task StrategyCrearAjusteInsumo(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteInsumo> ajusteInsumos = new AjusteMapper().CrearAjusteInsumoList(listaAjustes, idCreador);
            foreach (var item in ajusteInsumos)
            {
                AjusteInsumo ajusteInsumo = item;
                StockInsumo? stockInsumo = await _context.StockInsumos
                    .Where(w => w.IdCompraInsumo == ajusteInsumo.IdStockInsumo)
                    .FirstOrDefaultAsync();

                if (stockInsumo == null)
                {
                    var ci = await _context.CompraInsumos.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteInsumo.IdStockInsumo);
                    int idSede = ci?.Compra?.IdSede ?? 15;
                    stockInsumo = new StockInsumo
                    {
                        IdCompraInsumo = ajusteInsumo.IdStockInsumo,
                        IdSede = idSede,
                        Tipo = "MP",
                        StockDisponible = ajusteInsumo.StockNuevo
                    };
                    _context.StockInsumos.Add(stockInsumo);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockInsumo.StockDisponible += ajusteInsumo.Ajuste;
                }
                ajusteInsumo.IdStockInsumo = stockInsumo.Id;
                _context.AjusteInsumos.Add(ajusteInsumo);
            }
        }

        private async Task StrategyCrearAjusteEmpaque(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteEmpaque> ajusteEmpaques = new AjusteMapper().CrearAjusteEmpaqueList(listaAjustes, idCreador);
            foreach (var item in ajusteEmpaques)
            {
                AjusteEmpaque ajusteEmpaque = item;
                StockEmpaque? stockEmpaque = await _context.StockEmpaques
                    .Where(w => w.IdCompraEmpaque == ajusteEmpaque.IdStockEmpaque)
                    .FirstOrDefaultAsync();

                if (stockEmpaque == null)
                {
                    var ce = await _context.CompraEmpaques.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteEmpaque.IdStockEmpaque);
                    int idSede = ce?.Compra?.IdSede ?? 15;
                    stockEmpaque = new StockEmpaque
                    {
                        IdCompraEmpaque = ajusteEmpaque.IdStockEmpaque,
                        IdSede = idSede,
                        StockDisponible = ajusteEmpaque.StockNuevo
                    };
                    _context.StockEmpaques.Add(stockEmpaque);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockEmpaque.StockDisponible += ajusteEmpaque.Ajuste;
                }
                ajusteEmpaque.IdStockEmpaque = stockEmpaque.Id;
                _context.AjusteEmpaques.Add(ajusteEmpaque);
            }
        }

        private async Task StrategyCrearAjusteEconomato(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteEconomato> ajusteEconomatos = new AjusteMapper().CrearAjusteEconomatoList(listaAjustes, idCreador);
            foreach (var item in ajusteEconomatos)
            {
                AjusteEconomato ajusteEconomato = item;
                StockEconomato? stockEconomato = await _context.StockEconomatos
                    .Where(w => w.IdCompraEconomato == ajusteEconomato.IdStockEconomato)
                    .FirstOrDefaultAsync();

                if (stockEconomato == null)
                {
                    var ce = await _context.CompraEconomatos.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteEconomato.IdStockEconomato);
                    int idSede = ce?.Compra?.IdSede ?? 15;
                    stockEconomato = new StockEconomato
                    {
                        IdCompraEconomato = ajusteEconomato.IdStockEconomato,
                        IdSede = idSede,
                        StockDisponible = ajusteEconomato.StockNuevo
                    };
                    _context.StockEconomatos.Add(stockEconomato);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockEconomato.StockDisponible += ajusteEconomato.Ajuste;
                }
                ajusteEconomato.IdStockEconomato = stockEconomato.Id;
                _context.AjusteEconomatos.Add(ajusteEconomato);
            }
        }

        private async Task StrategyCrearAjusteProductoTerminado(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteProducto> ajusteProductoTerminados = new AjusteMapper().CrearAjusteProductoTerminadoList(listaAjustes, idCreador);
            foreach (var item in ajusteProductoTerminados)
            {
                AjusteProducto ajusteProductoTerminado = item;
                StockProducto? stockProducto = await _context.StockProductos
                    .Where(w => w.IdCompraProducto == ajusteProductoTerminado.IdStockProducto)
                    .FirstOrDefaultAsync();

                if (stockProducto == null)
                {
                    var cp = await _context.CompraProductos.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteProductoTerminado.IdStockProducto);
                    int idSede = cp?.Compra?.IdSede ?? 15;
                    stockProducto = new StockProducto
                    {
                        IdCompraProducto = ajusteProductoTerminado.IdStockProducto,
                        IdSede = idSede,
                        StockDisponible = ajusteProductoTerminado.StockNuevo
                    };
                    _context.StockProductos.Add(stockProducto);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockProducto.StockDisponible += ajusteProductoTerminado.Ajuste;
                }
                ajusteProductoTerminado.IdStockProducto = stockProducto.Id;
                _context.AjusteProductoTerminados.Add(ajusteProductoTerminado);
            }
        }
    }
}
using proy_back_Qbd.Models.ProductoIntermedio;
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

namespace Proy_back_QBD.Service.AjusteService.Familias
{
    /// <summary>Ajustes de inventario de la familia MP (materia prima).</summary>
    public class MateriaPrimaAjusteStrategy : IAjusteFamiliaStrategy
    {
        private readonly ApiContext _context;

        public MateriaPrimaAjusteStrategy(ApiContext context)
        {
            _context = context;
        }

        public string Familia => "MP";

        public async Task<List<TablaAjustesRes>> Listar(int idSede)
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

            var compraIds = compras.Select(c => c.Id).ToList();

            var ajustesInsumoQuery = await _context.AjusteInsumos
                .Include(a => a.StockInsumo)
                .Where(a =>
                    (a.StockInsumo != null && a.StockInsumo.IdCompraInsumo.HasValue && compraIds.Contains(a.StockInsumo.IdCompraInsumo.Value) && (idSede == 0 || a.StockInsumo.IdSede == idSede)) ||
                    compraIds.Contains(a.IdStockInsumo)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompra = ajustesInsumoQuery
                .GroupBy(a => (a.StockInsumo != null && a.StockInsumo.IdCompraInsumo.HasValue && a.StockInsumo.IdCompraInsumo.Value > 0) ? a.StockInsumo.IdCompraInsumo.Value : a.IdStockInsumo)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Salidas por fórmula magistral:
            //  - las que guardaron su reparto por registro se descuentan de ESE registro
            //  - las antiguas (sin reparto) quedan pendientes y se reparten FIFO como antes
            var formulasSede = await _context.FormulasCC
                .Where(f => f.Formula != null && f.Formula.SedeId == idSede)
                .Select(f => new { f.InsumoId, f.CantidadL, f.RegistrosLote })
                .ToListAsync();

            var salidasFM = new Dictionary<int, decimal>();
            var salidasFMPorRegistro = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in formulasSede)
            {
                var reparto = ParsearRegistrosLote(f.RegistrosLote);
                if (reparto.Count == 0)
                {
                    salidasFM[f.InsumoId] = (salidasFM.TryGetValue(f.InsumoId, out var acum) ? acum : 0m) + f.CantidadL;
                    continue;
                }
                foreach (var (registro, cantidad) in reparto)
                {
                    salidasFMPorRegistro[registro] = (salidasFMPorRegistro.TryGetValue(registro, out var acumReg) ? acumReg : 0m) + cantidad;
                }
            }

            var salidasPI = await _context.InsumoProductoIntermedios
                .Where(ipi => ipi.ProductoIntermedio != null && ipi.ProductoIntermedio.IdSede == idSede)
                .GroupBy(ipi => ipi.IdInsumo)
                .Select(g => new { IdInsumo = g.Key, Total = g.Sum(ipi => (ipi.Practica.HasValue && ipi.Practica.Value > 0) ? ipi.Practica.Value : ipi.CantidadLote) })
                .ToDictionaryAsync(x => x.IdInsumo, x => x.Total);

            // Consumos directos por lote desde StockInsumoProductoIntermedio
            var consumosPIDirectos = await _context.StockInsumoProductoIntermedios
                .Include(x => x.InsumoProductoIntermedio)
                    .ThenInclude(ipi => ipi.ProductoIntermedio)
                .Include(x => x.StockInsumo)
                .Where(x => x.InsumoProductoIntermedio != null &&
                            x.InsumoProductoIntermedio.ProductoIntermedio != null &&
                            x.InsumoProductoIntermedio.ProductoIntermedio.IdSede == idSede &&
                            x.StockInsumo != null &&
                            x.StockInsumo.IdCompraInsumo.HasValue &&
                            compraIds.Contains(x.StockInsumo.IdCompraInsumo.Value))
                .GroupBy(x => x.StockInsumo!.IdCompraInsumo!.Value)
                .Select(g => new { IdCompra = g.Key, Total = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.IdCompra, x => x.Total);

            var totalPIDirectoPorInsumo = await _context.StockInsumoProductoIntermedios
                .Include(x => x.InsumoProductoIntermedio)
                    .ThenInclude(ipi => ipi.ProductoIntermedio)
                .Where(x => x.InsumoProductoIntermedio != null &&
                            x.InsumoProductoIntermedio.ProductoIntermedio != null &&
                            x.InsumoProductoIntermedio.ProductoIntermedio.IdSede == idSede)
                .GroupBy(x => x.InsumoProductoIntermedio!.IdInsumo)
                .Select(g => new { IdInsumo = g.Key, Total = g.Sum(x => x.Cantidad) })
                .ToDictionaryAsync(x => x.IdInsumo, x => x.Total);

            var insumoGrupos = compras.GroupBy(c => c.IdInsumo);
            var resultado = new List<TablaAjustesRes>();

            foreach (var grupo in insumoGrupos)
            {
                int idInsumo = grupo.Key;
                decimal piDirecto = totalPIDirectoPorInsumo.ContainsKey(idInsumo) ? totalPIDirectoPorInsumo[idInsumo] : 0m;
                decimal totalPI = salidasPI.ContainsKey(idInsumo) ? salidasPI[idInsumo] : 0m;
                decimal salidasPIPendientes = Math.Max(0m, totalPI - piDirecto);

                decimal salidasPendientes = (salidasFM.ContainsKey(idInsumo) ? salidasFM[idInsumo] : 0m) + salidasPIPendientes;

                foreach (var compraInsumo in grupo)
                {
                    bool esKg = compraInsumo.Um != null && (
                        string.Equals(compraInsumo.Um.Trim(), "KG", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(compraInsumo.Um.Trim(), "KILOGRAMO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(compraInsumo.Um.Trim(), "KILOGRAMOS", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(compraInsumo.Um.Trim(), "KGS", StringComparison.OrdinalIgnoreCase)
                    );

                    decimal entradasCompra = 0m;
                    if (compraInsumo.Compra != null && compraInsumo.Compra.IdSede == idSede && (idSede == 15 || compraInsumo.Compra.FechaLab != null))
                    {
                        if (esKg)
                        {
                            if (compraInsumo.CantidadRecibida.HasValue && compraInsumo.CantidadRecibida.Value > 0)
                            {
                                if (compraInsumo.CantidadRecibida.Value <= compraInsumo.CantidadSolicitada * 2 && compraInsumo.CantidadRecibida.Value < 500)
                                {
                                    entradasCompra = compraInsumo.CantidadRecibida.Value * 1000m;
                                }
                                else
                                {
                                    entradasCompra = compraInsumo.CantidadRecibida.Value;
                                }
                            }
                            else
                            {
                                entradasCompra = compraInsumo.CantidadSolicitada * 1000m;
                            }
                        }
                        else
                        {
                            entradasCompra = (compraInsumo.CantidadRecibida.HasValue && compraInsumo.CantidadRecibida.Value > 0) ? compraInsumo.CantidadRecibida.Value : compraInsumo.CantidadSolicitada;
                        }
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

                    decimal salidasPIDirectas = consumosPIDirectos.ContainsKey(compraInsumo.Id) ? consumosPIDirectos[compraInsumo.Id] : 0m;
                    string registroCompra = "MP" + Alfanumerico.ConvertToBase36(compraInsumo.Id);
                    decimal salidasFMRegistro = salidasFMPorRegistro.TryGetValue(registroCompra, out var fmReg) ? fmReg : 0m;
                    decimal salidasLote = salidasNS + salidasPIDirectas + salidasFMRegistro;

                    var stockInsumosSede = compraInsumo.StockInsumos
                        .Where(si => si.IdSede == idSede)
                        .ToList();

                    var ajustesDeCompra = ajustesPorCompra.ContainsKey(compraInsumo.Id) ? ajustesPorCompra[compraInsumo.Id] : new List<AjusteInsumo>();
                    var ultimoAjuste = ajustesDeCompra.FirstOrDefault();

                    decimal saldo = 0m;
                    string observacionAjuste = "";

                    if (ultimoAjuste != null)
                    {
                        bool estaVencido = compraInsumo.FechaVencimiento < DateTime.UtcNow;
                        if (estaVencido)
                        {
                            saldo = 0m;
                        }
                        else
                        {
                            var stockPrincipal = stockInsumosSede.FirstOrDefault(w => w.IdNotaSalidaInsumo == null || w.IdNotaSalidaInsumo == 0);
                            saldo = stockPrincipal != null ? stockPrincipal.StockDisponible : (stockInsumosSede.Any() ? stockInsumosSede.Sum(si => si.StockDisponible) : ultimoAjuste.StockNuevo);
                        }
                        observacionAjuste = ultimoAjuste.Observacion ?? "";
                    }
                    else
                    {
                        bool estaVencido = compraInsumo.FechaVencimiento < DateTime.UtcNow;
                        decimal bajas = 0m;
                        if (estaVencido)
                        {
                            bajas = stockInsumosSede.Sum(si => si.StockDisponible);
                        }

                        decimal saldoBruto = entradas - salidasLote - bajas;
                        if (saldoBruto < 0) saldoBruto = 0;
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

                        saldo = saldoBruto - descuentoLocal;
                    }
                    if (saldo < 0) saldo = 0;

                    resultado.Add(new TablaAjustesRes
                    {
                        Codigo = UtilFamilia.CodigoInsumo(compraInsumo.IdInsumo),
                        IdCompraFamilia = compraInsumo.Id,
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

        public async Task Registrar(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
        {
            List<AjusteInsumo> ajusteInsumos = new AjusteMapper().CrearAjusteInsumoList(listaAjustes, idCreador);
            foreach (var item in ajusteInsumos)
            {
                AjusteInsumo ajusteInsumo = item;
                StockInsumo? stockInsumo = await _context.StockInsumos
                    .Where(w => (idSede <= 0 || w.IdSede == idSede) &&
                                (w.IdCompraInsumo == ajusteInsumo.IdStockInsumo ||
                                 w.IdProductoIntermedio == ajusteInsumo.IdStockInsumo ||
                                 w.Id == ajusteInsumo.IdStockInsumo))
                    .FirstOrDefaultAsync();

                if (stockInsumo == null)
                {
                    var pi = await _context.ProductosIntermedios.FirstOrDefaultAsync(p => p.Id == ajusteInsumo.IdStockInsumo);
                    if (pi != null)
                    {
                        int finalSede = idSede > 0 ? idSede : (pi.IdSede > 0 ? pi.IdSede : 15);
                        stockInsumo = new StockInsumo
                        {
                            IdProductoIntermedio = pi.Id,
                            IdSede = finalSede,
                            Tipo = "PI",
                            StockDisponible = ajusteInsumo.StockNuevo,
                            UnidadMedida = string.IsNullOrWhiteSpace(pi.Um) ? "G" : pi.Um
                        };
                        _context.StockInsumos.Add(stockInsumo);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        var ci = await _context.CompraInsumos.Include(c => c.Compra).Include(c => c.Insumo).FirstOrDefaultAsync(c => c.Id == ajusteInsumo.IdStockInsumo);
                        if (ci == null) continue;
                        int finalSede = idSede > 0 ? idSede : (ci?.Compra?.IdSede ?? 15);
                        stockInsumo = new StockInsumo
                        {
                            IdCompraInsumo = ajusteInsumo.IdStockInsumo,
                            IdSede = finalSede,
                            Tipo = ci?.Insumo?.Clasificacion == "PI" ? "PI" : "MP",
                            StockDisponible = ajusteInsumo.StockNuevo,
                            UnidadMedida = string.IsNullOrWhiteSpace(ci?.Um) ? "G" : ci.Um
                        };
                        _context.StockInsumos.Add(stockInsumo);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    stockInsumo.StockDisponible = ajusteInsumo.StockNuevo;
                }
                if (stockInsumo != null) {
                    ajusteInsumo.IdStockInsumo = stockInsumo.Id;
                    _context.AjusteInsumos.Add(ajusteInsumo);
                }
            }
        }

        public async Task<List<DetalleAjusteRes>> Detalle(int registroId, int? idInsumo, int? idSede)
        {
            List<DetalleAjusteRes> response = new();
            var query = _context.AjusteInsumos
                .Include(a => a.StockInsumo)
                    .ThenInclude(s => s.CompraInsumo)
                .Include(a => a.StockInsumo)
                    .ThenInclude(s => s.ProductoIntermedio)
                .Include(a => a.Creador)
                    .ThenInclude(c => c.Persona)
                .AsNoTracking();

            if (registroId > 0)
            {
                query = query.Where(w => (w.StockInsumo != null && (w.StockInsumo.IdCompraInsumo == registroId || w.StockInsumo.IdProductoIntermedio == registroId)) || w.IdStockInsumo == registroId);
            }
            else if (idInsumo.HasValue && idInsumo.Value > 0)
            {
                query = query.Where(w =>
                    ((w.StockInsumo != null && w.StockInsumo.CompraInsumo != null && w.StockInsumo.CompraInsumo.IdInsumo == idInsumo.Value) ||
                     (w.StockInsumo != null && w.StockInsumo.ProductoIntermedio != null && w.StockInsumo.ProductoIntermedio.IdInsumo == idInsumo.Value) ||
                     (_context.CompraInsumos.Any(ci => ci.Id == w.IdStockInsumo && ci.IdInsumo == idInsumo.Value)) ||
                     (_context.ProductosIntermedios.Any(pi => pi.Id == w.IdStockInsumo && pi.IdInsumo == idInsumo.Value))) &&
                    (!idSede.HasValue || idSede.Value == 0 || (w.StockInsumo != null && w.StockInsumo.IdSede == idSede.Value)));
            }
            else
            {
                return response;
            }

            var rawList = await query
                .OrderByDescending(odb => odb.FechaCreacion)
                .ToListAsync();

            var idsCompraInsumo = rawList
                .Select(s => (s.StockInsumo != null && s.StockInsumo.IdCompraInsumo.HasValue && s.StockInsumo.IdCompraInsumo.Value > 0) ? s.StockInsumo.IdCompraInsumo.Value : s.IdStockInsumo)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var comprasDict = await _context.CompraInsumos
                .Where(ci => idsCompraInsumo.Contains(ci.Id))
                .ToDictionaryAsync(ci => ci.Id);

            var idsPI = rawList
                .Select(s => (s.StockInsumo != null && s.StockInsumo.IdProductoIntermedio.HasValue && s.StockInsumo.IdProductoIntermedio.Value > 0) ? s.StockInsumo.IdProductoIntermedio.Value : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var piDict = await _context.ProductosIntermedios
                .Where(pi => idsPI.Contains(pi.Id))
                .ToDictionaryAsync(pi => pi.Id);

            response = rawList.Select(s =>
            {
                string reg = "-";
                string lote = "-";

                if (s.StockInsumo?.ProductoIntermedio != null || (s.StockInsumo?.IdProductoIntermedio.HasValue == true && piDict.ContainsKey(s.StockInsumo.IdProductoIntermedio.Value)))
                {
                    var pi = s.StockInsumo?.ProductoIntermedio ?? (s.StockInsumo?.IdProductoIntermedio.HasValue == true && piDict.ContainsKey(s.StockInsumo.IdProductoIntermedio.Value) ? piDict[s.StockInsumo.IdProductoIntermedio.Value] : null);
                    if (pi != null)
                    {
                        reg = "PI" + Alfanumerico.ConvertToBase36(pi.Id);
                        lote = pi.Lote ?? "-";
                    }
                }
                else
                {
                    int idCompra = (s.StockInsumo != null && s.StockInsumo.IdCompraInsumo.HasValue && s.StockInsumo.IdCompraInsumo.Value > 0) ? s.StockInsumo.IdCompraInsumo.Value : s.IdStockInsumo;
                    comprasDict.TryGetValue(idCompra, out var ci);
                    reg = ci != null ? ("MP" + Alfanumerico.ConvertToBase36(ci.Id)) : (idCompra > 0 ? ("MP" + Alfanumerico.ConvertToBase36(idCompra)) : "-");
                    lote = ci?.Lote ?? s.StockInsumo?.CompraInsumo?.Lote ?? "-";
                }

                return new DetalleAjusteRes
                {
                    Registro = reg,
                    Lote = lote,
                    FechaCreacion = s.FechaCreacion,
                    Usuario = s.Creador != null && s.Creador.Persona != null ? (s.Creador.Persona.NombreCompleto ?? "") : "No registrado",
                    Stock = s.StockAnterior,
                    Diferencia = s.Ajuste,
                    StockFinal = s.StockNuevo,
                    Observacion = s.Observacion ?? ""
                };
            }).ToList();
            return response;
        }


        /// <summary>
        /// Convierte "MPNX:100;MPPH:185" en [(MPNX,100),(MPPH,185)].
        /// </summary>
        private static List<(string Registro, decimal Cantidad)> ParsearRegistrosLote(string? valor)
        {
            var lista = new List<(string Registro, decimal Cantidad)>();
            if (string.IsNullOrWhiteSpace(valor)) return lista;
            foreach (var parte in valor.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var kv = parte.Split(':', 2, StringSplitOptions.TrimEntries);
                if (kv.Length != 2 || string.IsNullOrWhiteSpace(kv[0])) continue;
                if (decimal.TryParse(kv[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cant) && cant > 0)
                {
                    lista.Add((kv[0].Trim().ToUpper(), cant));
                }
            }
            return lista;
        }
    }
}

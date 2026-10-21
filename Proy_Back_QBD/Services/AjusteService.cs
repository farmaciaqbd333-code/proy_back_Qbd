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

namespace Proy_back_QBD.Service.AjusteService
{
    public class AjusteService : IAjusteService
    {
        private readonly ApiContext _context;
        private static readonly List<string> FamiliasAptas = ["MP", "ME", "PT", "ECO", "PI"];
        public AjusteService(ApiContext context)
        {
            _context = context;
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

        public async Task<List<TablaAjustesRes>> ListaAjustes(string familia, int idSede)
        {
            if (idSede <= 0)
            {
                var centralSede = await _context.Sedes.FirstOrDefaultAsync(s => s.Nombre.ToUpper().Contains("CENTRAL") || s.Id == 15);
                idSede = centralSede?.Id ?? 15;
            }

            List<TablaAjustesRes> Response = familia switch
            {
                "MP" => await ObtenerMateriaPrima(idSede),
                "PI" => await ObtenerProductosIntermediosAjuste(idSede),
                "ME" => await ObtenerMateriaEmpaques(idSede),
                "PT" => await ObtenerProductosTerminados(idSede),
                "ECO" => await ObtenerEconomatos(idSede),
                _ => throw new BadRequestException("Familia no Apta")
            };

            return Response;
        }

        public async Task RegistrarAjuste(CrearAjusteReq request)
        {
            string familia = request.Familia;
            if (familia == "PI") familia = "MP";
            int idCreador = request.IdCreador;
            int idSede = request.IdSede.HasValue && request.IdSede.Value > 0 ? request.IdSede.Value : 15;
            if (FamiliasAptas.Contains(familia))
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    List<CrearAjustes> listaAjustes = request.ListaAjustes;

                    int targetSede = request.IdSede.HasValue && request.IdSede.Value > 0 ? request.IdSede.Value : 15;
                    switch (familia)
                    {
                        case "PI":
                        case "MP":
                            await StrategyCrearAjusteInsumo(listaAjustes, idCreador, targetSede); break;
                        case "ME":
                            await StrategyCrearAjusteEmpaque(listaAjustes, idCreador, targetSede); break;
                        case "ECO":
                            await StrategyCrearAjusteEconomato(listaAjustes, idCreador, targetSede); break;
                        case "PT":
                            await StrategyCrearAjusteProductoTerminado(listaAjustes, idCreador, targetSede); break;
                        default: throw new BadRequestException("Familia no apta");
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw new ServerException($"OcurriÃ³ un error al crear el ajuste: {e.Message}", e);
                }
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }
        }

        public async Task<List<DetalleAjusteRes>> DetalleAjuste(int registroId, string familia, int? idInsumo = null, int? idSede = null)
        {
            if (familia == "PI") familia = "MP";

            if (FamiliasAptas.Contains(familia))
            {
                List<DetalleAjusteRes> response = new();
                if (familia == "MP")
                {
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
                }
                if (familia == "ME")
                {
                    var query = _context.AjusteEmpaques
                        .Include(a => a.StockEmpaque)
                            .ThenInclude(s => s.CompraEmpaque)
                        .Include(a => a.Creador)
                            .ThenInclude(c => c.Persona)
                        .AsNoTracking();

                    if (registroId > 0)
                    {
                        query = query.Where(w => (w.StockEmpaque != null && w.StockEmpaque.IdCompraEmpaque == registroId) || w.IdStockEmpaque == registroId);
                    }
                    else if (idInsumo.HasValue && idInsumo.Value > 0)
                    {
                        query = query.Where(w =>
                            ((w.StockEmpaque != null && w.StockEmpaque.CompraEmpaque != null && w.StockEmpaque.CompraEmpaque.IdEmpaque == idInsumo.Value) ||
                             (_context.CompraEmpaques.Any(ce => ce.Id == w.IdStockEmpaque && ce.IdEmpaque == idInsumo.Value))) &&
                            (!idSede.HasValue || idSede.Value == 0 || (w.StockEmpaque != null && w.StockEmpaque.IdSede == idSede.Value)));
                    }
                    else
                    {
                        return response;
                    }

                    var rawList = await query
                        .OrderByDescending(odb => odb.FechaCreacion)
                        .ToListAsync();

                    var idsCompra = rawList
                        .Select(s => (s.StockEmpaque != null && s.StockEmpaque.IdCompraEmpaque > 0) ? s.StockEmpaque.IdCompraEmpaque : s.IdStockEmpaque)
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    var comprasDict = await _context.CompraEmpaques
                        .Where(ce => idsCompra.Contains(ce.Id))
                        .ToDictionaryAsync(ce => ce.Id);

                    response = rawList.Select(s =>
                    {
                        int idCompra = (s.StockEmpaque != null && s.StockEmpaque.IdCompraEmpaque > 0) ? s.StockEmpaque.IdCompraEmpaque : s.IdStockEmpaque;
                        comprasDict.TryGetValue(idCompra, out var ce);
                        string reg = ce != null ? ("ME" + Alfanumerico.ConvertToBase36(ce.Id)) : (idCompra > 0 ? ("ME" + Alfanumerico.ConvertToBase36(idCompra)) : "-");
                        string lote = ce?.Lote ?? s.StockEmpaque?.CompraEmpaque?.Lote ?? "-";

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
                }
                if (familia == "PT")
                {
                    var query = _context.AjusteProductoTerminados
                        .Include(a => a.StockProducto)
                            .ThenInclude(s => s.CompraProducto)
                        .Include(a => a.Creador)
                            .ThenInclude(c => c.Persona)
                        .AsNoTracking();

                    if (registroId > 0)
                    {
                        query = query.Where(w => (w.StockProducto != null && w.StockProducto.IdCompraProducto == registroId) || w.IdStockProducto == registroId);
                    }
                    else if (idInsumo.HasValue && idInsumo.Value > 0)
                    {
                        query = query.Where(w =>
                            ((w.StockProducto != null && w.StockProducto.CompraProducto != null && w.StockProducto.CompraProducto.IdProducto == idInsumo.Value) ||
                             (_context.CompraProductos.Any(cp => cp.Id == w.IdStockProducto && cp.IdProducto == idInsumo.Value))) &&
                            (!idSede.HasValue || idSede.Value == 0 || (w.StockProducto != null && w.StockProducto.IdSede == idSede.Value)));
                    }
                    else
                    {
                        return response;
                    }

                    var rawList = await query
                        .OrderByDescending(odb => odb.FechaCreacion)
                        .ToListAsync();

                    var idsCompra = rawList
                        .Select(s => (s.StockProducto != null && s.StockProducto.IdCompraProducto > 0) ? s.StockProducto.IdCompraProducto : s.IdStockProducto)
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    var comprasDict = await _context.CompraProductos
                        .Where(cp => idsCompra.Contains(cp.Id))
                        .ToDictionaryAsync(cp => cp.Id);

                    response = rawList.Select(s =>
                    {
                        int idCompra = (s.StockProducto != null && s.StockProducto.IdCompraProducto > 0) ? s.StockProducto.IdCompraProducto : s.IdStockProducto;
                        comprasDict.TryGetValue(idCompra, out var cp);
                        string reg = cp != null ? ("PT" + Alfanumerico.ConvertToBase36(cp.Id)) : (idCompra > 0 ? ("PT" + Alfanumerico.ConvertToBase36(idCompra)) : "-");
                        string lote = cp?.Lote ?? s.StockProducto?.CompraProducto?.Lote ?? "-";

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
                }
                if (familia == "ECO")
                {
                    var query = _context.AjusteEconomatos
                        .Include(a => a.StockEconomato)
                            .ThenInclude(s => s.CompraEconomato)
                        .Include(a => a.Creador)
                            .ThenInclude(c => c.Persona)
                        .AsNoTracking();

                    if (registroId > 0)
                    {
                        query = query.Where(w => (w.StockEconomato != null && w.StockEconomato.IdCompraEconomato == registroId) || w.IdStockEconomato == registroId);
                    }
                    else if (idInsumo.HasValue && idInsumo.Value > 0)
                    {
                        query = query.Where(w =>
                            ((w.StockEconomato != null && w.StockEconomato.CompraEconomato != null && w.StockEconomato.CompraEconomato.IdEconomato == idInsumo.Value) ||
                             (_context.CompraEconomatos.Any(ce => ce.Id == w.IdStockEconomato && ce.IdEconomato == idInsumo.Value))) &&
                            (!idSede.HasValue || idSede.Value == 0 || (w.StockEconomato != null && w.StockEconomato.IdSede == idSede.Value)));
                    }
                    else
                    {
                        return response;
                    }

                    var rawList = await query
                        .OrderByDescending(odb => odb.FechaCreacion)
                        .ToListAsync();

                    var idsCompra = rawList
                        .Select(s => (s.StockEconomato != null && s.StockEconomato.IdCompraEconomato > 0) ? s.StockEconomato.IdCompraEconomato : s.IdStockEconomato)
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    var comprasDict = await _context.CompraEconomatos
                        .Where(ce => idsCompra.Contains(ce.Id))
                        .ToDictionaryAsync(ce => ce.Id);

                    response = rawList.Select(s =>
                    {
                        int idCompra = (s.StockEconomato != null && s.StockEconomato.IdCompraEconomato > 0) ? s.StockEconomato.IdCompraEconomato : s.IdStockEconomato;
                        comprasDict.TryGetValue(idCompra, out var ce);
                        string reg = ce != null ? ("ECO" + Alfanumerico.ConvertToBase36(ce.Id)) : (idCompra > 0 ? ("ECO" + Alfanumerico.ConvertToBase36(idCompra)) : "-");

                        return new DetalleAjusteRes
                        {
                            Registro = reg,
                            Lote = "-",
                            FechaCreacion = s.FechaCreacion,
                            Usuario = s.Creador != null && s.Creador.Persona != null ? (s.Creador.Persona.NombreCompleto ?? "") : "No registrado",
                            Stock = s.StockAnterior,
                            Diferencia = s.Ajuste,
                            StockFinal = s.StockNuevo,
                            Observacion = s.Observacion ?? ""
                        };
                    }).ToList();
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

            var compraEmpaqueIds = compras.Select(c => c.Id).ToList();

            var ajustesEmpaqueQuery = await _context.AjusteEmpaques
                .Include(a => a.StockEmpaque)
                .Where(a =>
                    (a.StockEmpaque != null && compraEmpaqueIds.Contains(a.StockEmpaque.IdCompraEmpaque) && (idSede == 0 || a.StockEmpaque.IdSede == idSede)) ||
                    compraEmpaqueIds.Contains(a.IdStockEmpaque)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompraEmpaque = ajustesEmpaqueQuery
                .GroupBy(a => (a.StockEmpaque != null && a.StockEmpaque.IdCompraEmpaque > 0) ? a.StockEmpaque.IdCompraEmpaque : a.IdStockEmpaque)
                .ToDictionary(g => g.Key, g => g.ToList());

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
                var ajustesDeCompra = ajustesPorCompraEmpaque.ContainsKey(s.Id) ? ajustesPorCompraEmpaque[s.Id] : new List<AjusteEmpaque>();
                var ultimoAjuste = ajustesDeCompra.FirstOrDefault();

                decimal saldo = 0m;
                string observacionAjuste = "";

                if (ultimoAjuste != null)
                {
                    saldo = ultimoAjuste.StockNuevo;
                    observacionAjuste = ultimoAjuste.Observacion ?? "";
                }
                else if (stockSede.Any())
                {
                    saldo = stockSede.Sum(se => se.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoEmpaque(s.IdEmpaque),
                    IdCompraFamilia = s.Id,
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

            var compraEcoIds = compras.Select(c => c.Id).ToList();

            var ajustesEcoQuery = await _context.AjusteEconomatos
                .Include(a => a.StockEconomato)
                .Where(a =>
                    (a.StockEconomato != null && compraEcoIds.Contains(a.StockEconomato.IdCompraEconomato) && (idSede == 0 || a.StockEconomato.IdSede == idSede)) ||
                    compraEcoIds.Contains(a.IdStockEconomato)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompraEco = ajustesEcoQuery
                .GroupBy(a => (a.StockEconomato != null && a.StockEconomato.IdCompraEconomato > 0) ? a.StockEconomato.IdCompraEconomato : a.IdStockEconomato)
                .ToDictionary(g => g.Key, g => g.ToList());

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
                var ajustesDeCompra = ajustesPorCompraEco.ContainsKey(s.Id) ? ajustesPorCompraEco[s.Id] : new List<AjusteEconomato>();
                var ultimoAjuste = ajustesDeCompra.FirstOrDefault();

                decimal saldo = 0m;
                string observacionAjuste = "";

                if (ultimoAjuste != null)
                {
                    saldo = ultimoAjuste.StockNuevo;
                    observacionAjuste = ultimoAjuste.Observacion ?? "";
                }
                else if (stockSede.Any())
                {
                    saldo = stockSede.Sum(se => se.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdEconomato),
                    IdCompraFamilia = s.Id,
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

            var compraPtIds = compras.Select(c => c.Id).ToList();

            var ajustesPtQuery = await _context.AjusteProductoTerminados
                .Include(a => a.StockProducto)
                .Where(a =>
                    (a.StockProducto != null && compraPtIds.Contains(a.StockProducto.IdCompraProducto) && (idSede == 0 || a.StockProducto.IdSede == idSede)) ||
                    compraPtIds.Contains(a.IdStockProducto)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorCompraPt = ajustesPtQuery
                .GroupBy(a => (a.StockProducto != null && a.StockProducto.IdCompraProducto > 0) ? a.StockProducto.IdCompraProducto : a.IdStockProducto)
                .ToDictionary(g => g.Key, g => g.ToList());

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
                var ajustesDeCompra = ajustesPorCompraPt.ContainsKey(s.Id) ? ajustesPorCompraPt[s.Id] : new List<AjusteProducto>();
                var ultimoAjuste = ajustesDeCompra.FirstOrDefault();

                decimal saldo = 0m;
                string observacionAjuste = "";

                if (ultimoAjuste != null)
                {
                    saldo = ultimoAjuste.StockNuevo;
                    observacionAjuste = ultimoAjuste.Observacion ?? "";
                }
                else if (stockSede.Any())
                {
                    saldo = stockSede.Sum(sp => sp.StockDisponible);
                }
                else
                {
                    saldo = entradasLote - salidasNS;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdProducto),
                    IdCompraFamilia = s.Id,
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

        private async Task<List<TablaAjustesRes>> ObtenerProductosIntermediosAjuste(int idSede)
        {
            var resultado = new List<TablaAjustesRes>();

            // 1. Elaboraciones directas en esta sede
            var piList = await _context.ProductosIntermedios
                .Include(pi => pi.Insumo)
                .Include(pi => pi.StockInsumo)
                    .ThenInclude(si => si.AjusteInsumos)
                .Where(w => w.IdSede == idSede)
                .OrderBy(o => o.FechaCreacion)
                .ToListAsync();

            var piIds = piList.Select(p => p.Id).ToList();

            var ajustesPIQuery = await _context.AjusteInsumos
                .Include(a => a.StockInsumo)
                .Where(a =>
                    (a.StockInsumo != null && a.StockInsumo.IdProductoIntermedio.HasValue && piIds.Contains(a.StockInsumo.IdProductoIntermedio.Value) && (idSede == 0 || a.StockInsumo.IdSede == idSede)) ||
                    piIds.Contains(a.IdStockInsumo)
                )
                .OrderByDescending(a => a.FechaCreacion)
                .ToListAsync();

            var ajustesPorPI = ajustesPIQuery
                .GroupBy(a => (a.StockInsumo != null && a.StockInsumo.IdProductoIntermedio.HasValue && a.StockInsumo.IdProductoIntermedio.Value > 0) ? a.StockInsumo.IdProductoIntermedio.Value : a.IdStockInsumo)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var s in piList)
            {
                decimal cantIngresada = (s.TipoUso == "PI-FMG" || s.Um == "UND")
                    ? (s.LoteEstandar ?? 0)
                    : (s.LoteEstTotal ?? s.LoteEstandar ?? 0);

                var ajustesDePI = ajustesPorPI.ContainsKey(s.Id) ? ajustesPorPI[s.Id] : (s.StockInsumo?.AjusteInsumos?.ToList() ?? new List<AjusteInsumo>());
                var ultimoAjuste = ajustesDePI.FirstOrDefault();

                decimal saldo = 0m;
                string observacionAjuste = "";

                if (ultimoAjuste != null)
                {
                    saldo = s.StockInsumo != null ? s.StockInsumo.StockDisponible : ultimoAjuste.StockNuevo;
                    observacionAjuste = ultimoAjuste.Observacion ?? "";
                }
                else
                {
                    saldo = s.StockInsumo != null ? s.StockInsumo.StockDisponible : cantIngresada;
                }

                if (saldo < 0) saldo = 0;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoProductoIntermedio(s.IdInsumo),
                    IdCompraFamilia = s.Id,
                    Registro = "PI" + Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Insumo?.Descripcion ?? "",
                    Lote = s.Lote ?? "",
                    Saldo = saldo,
                    FechaFabricacion = s.FechaEmision ?? s.FechaCreacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Clasificacion = "PI",
                    Observacion = observacionAjuste
                });
            }

            // 2. Transferencias recibidas por Nota de Salida en esta sede
            var allPIs = await _context.ProductosIntermedios
                .Include(p => p.Insumo)
                .AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Lote))
                .ToListAsync();

            var piByLote = allPIs
                .GroupBy(p => p.Lote!.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.First());

            var notasSalidaDestinoPI = await _context.NotaSalidaInsumos.AsNoTracking()
                .Include(nsi => nsi.NotaSalida)
                .Where(nsi => !nsi.IdCompraInsumo.HasValue &&
                              nsi.NotaSalida != null &&
                              nsi.NotaSalida.IdSedeDestino == idSede &&
                              (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                .OrderBy(nsi => nsi.NotaSalida!.FechaRecepcion ?? nsi.NotaSalida!.FechaSalida)
                .ToListAsync();

            var nsiIds = notasSalidaDestinoPI.Select(n => n.Id).ToList();

            var stockNSList = await _context.StockInsumos
                .Include(s => s.AjusteInsumos)
                .Where(s => s.IdSede == idSede && s.IdNotaSalidaInsumo.HasValue && nsiIds.Contains(s.IdNotaSalidaInsumo.Value))
                .ToListAsync();

            var stockNSDict = stockNSList.ToDictionary(s => s.IdNotaSalidaInsumo!.Value, s => s);

            foreach (var nsi in notasSalidaDestinoPI)
            {
                if (string.IsNullOrEmpty(nsi.Lote)) continue;
                var key = nsi.Lote.Trim().ToUpper();
                if (!piByLote.TryGetValue(key, out var piRef)) continue;

                stockNSDict.TryGetValue(nsi.Id, out var stockNS);

                decimal cantRecibida = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) *
                    ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad);

                var ajustesNS = stockNS?.AjusteInsumos?.OrderByDescending(a => a.FechaCreacion).ToList() ?? new List<AjusteInsumo>();
                var ultimoAjusteNS = ajustesNS.FirstOrDefault();

                decimal saldo = 0m;
                string observacionAjuste = "";

                if (ultimoAjusteNS != null)
                {
                    saldo = stockNS != null ? stockNS.StockDisponible : ultimoAjusteNS.StockNuevo;
                    observacionAjuste = ultimoAjusteNS.Observacion ?? "";
                }
                else
                {
                    saldo = stockNS != null ? stockNS.StockDisponible : cantRecibida;
                }

                if (saldo < 0) saldo = 0;

                DateTime? fechaFab = piRef.FechaEmision ?? piRef.FechaCreacion;
                DateTime? fechaVcto = piRef.FechaVencimiento;

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoProductoIntermedio(piRef.IdInsumo),
                    IdCompraFamilia = piRef.Id,
                    Registro = "PI" + Alfanumerico.ConvertToBase36(piRef.Id),
                    Descripcion = piRef.Insumo?.Descripcion ?? "",
                    Lote = nsi.Lote ?? piRef.Lote ?? "",
                    Saldo = saldo,
                    FechaFabricacion = fechaFab,
                    FechaVencimiento = fechaVcto,
                    Clasificacion = "PI",
                    Observacion = observacionAjuste
                });
            }

            // 3. Tambien incluir cualquier CompraInsumos que haya sido clasificado como "PI"
            var comprasPI = await _context.CompraInsumos
                .Include(ci => ci.Insumo)
                .Include(ci => ci.Compra)
                .Include(ci => ci.StockInsumos)
                    .ThenInclude(si => si.AjusteInsumos)
                .Include(ci => ci.NotaSalidaInsumos)
                    .ThenInclude(nsi => nsi.NotaSalida)
                .Where(ci => ci.Insumo != null && ci.Insumo.Clasificacion == "PI" &&
                    ((ci.Compra != null && ci.Compra.IdSede == idSede && (idSede == 15 || ci.Compra.FechaLab != null)) ||
                     ci.NotaSalidaInsumos.Any(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))))
                .ToListAsync();

            foreach (var ci in comprasPI)
            {
                var stockSede = ci.StockInsumos.Where(si => si.IdSede == idSede).ToList();
                decimal saldo = stockSede.Any() ? stockSede.Sum(si => si.StockDisponible) : (ci.CantidadRecibida ?? ci.CantidadSolicitada);
                var ultimoAjuste = stockSede.SelectMany(s => s.AjusteInsumos).OrderByDescending(a => a.FechaCreacion).FirstOrDefault();

                resultado.Add(new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoProductoIntermedio(ci.IdInsumo),
                    IdCompraFamilia = ci.Id,
                    Registro = "PI" + Alfanumerico.ConvertToBase36(ci.Id),
                    Descripcion = ci.Insumo?.Descripcion ?? "",
                    Lote = ci.Lote ?? "",
                    Saldo = saldo,
                    FechaFabricacion = ci.FechaFabricacion,
                    FechaVencimiento = ci.FechaVencimiento,
                    Clasificacion = "PI",
                    Observacion = ultimoAjuste?.Observacion ?? ""
                });
            }

            return resultado.OrderBy(r => r.Codigo).ThenBy(r => r.Registro).ToList();
        }

        // REGISTRAR AJUSTES
        private async Task StrategyCrearAjusteInsumo(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
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

        private async Task StrategyCrearAjusteEmpaque(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
        {
            List<AjusteEmpaque> ajusteEmpaques = new AjusteMapper().CrearAjusteEmpaqueList(listaAjustes, idCreador);
            foreach (var item in ajusteEmpaques)
            {
                AjusteEmpaque ajusteEmpaque = item;
                StockEmpaque? stockEmpaque = await _context.StockEmpaques
                    .Where(w => w.IdCompraEmpaque == ajusteEmpaque.IdStockEmpaque && (idSede <= 0 || w.IdSede == idSede))
                    .FirstOrDefaultAsync();

                if (stockEmpaque == null)
                {
                    var ce = await _context.CompraEmpaques.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteEmpaque.IdStockEmpaque);
                    if (ce == null) continue;
                    int finalSede = idSede > 0 ? idSede : (ce?.Compra?.IdSede ?? 15);
                    stockEmpaque = new StockEmpaque
                    {
                        IdCompraEmpaque = ajusteEmpaque.IdStockEmpaque,
                        IdSede = finalSede,
                        StockDisponible = ajusteEmpaque.StockNuevo
                    };
                    _context.StockEmpaques.Add(stockEmpaque);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockEmpaque.StockDisponible = ajusteEmpaque.StockNuevo;
                }
                if (stockEmpaque != null) {
                    ajusteEmpaque.IdStockEmpaque = stockEmpaque.Id;
                    _context.AjusteEmpaques.Add(ajusteEmpaque);
                }
            }
        }

        private async Task StrategyCrearAjusteEconomato(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
        {
            List<AjusteEconomato> ajusteEconomatos = new AjusteMapper().CrearAjusteEconomatoList(listaAjustes, idCreador);
            foreach (var item in ajusteEconomatos)
            {
                AjusteEconomato ajusteEconomato = item;
                StockEconomato? stockEconomato = await _context.StockEconomatos
                    .Where(w => w.IdCompraEconomato == ajusteEconomato.IdStockEconomato && (idSede <= 0 || w.IdSede == idSede))
                    .FirstOrDefaultAsync();

                if (stockEconomato == null)
                {
                    var ce = await _context.CompraEconomatos.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteEconomato.IdStockEconomato);
                    if (ce == null) continue;
                    int finalSede = idSede > 0 ? idSede : (ce?.Compra?.IdSede ?? 15);
                    stockEconomato = new StockEconomato
                    {
                        IdCompraEconomato = ajusteEconomato.IdStockEconomato,
                        IdSede = finalSede,
                        StockDisponible = ajusteEconomato.StockNuevo
                    };
                    _context.StockEconomatos.Add(stockEconomato);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockEconomato.StockDisponible = ajusteEconomato.StockNuevo;
                }
                if (stockEconomato != null) {
                    ajusteEconomato.IdStockEconomato = stockEconomato.Id;
                    _context.AjusteEconomatos.Add(ajusteEconomato);
                }
            }
        }

        private async Task StrategyCrearAjusteProductoTerminado(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
        {
            List<AjusteProducto> ajusteProductoTerminados = new AjusteMapper().CrearAjusteProductoTerminadoList(listaAjustes, idCreador);
            foreach (var item in ajusteProductoTerminados)
            {
                AjusteProducto ajusteProductoTerminado = item;
                StockProducto? stockProducto = await _context.StockProductos
                    .Where(w => w.IdCompraProducto == ajusteProductoTerminado.IdStockProducto && (idSede <= 0 || w.IdSede == idSede))
                    .FirstOrDefaultAsync();

                if (stockProducto == null)
                {
                    var cp = await _context.CompraProductos.Include(c => c.Compra).FirstOrDefaultAsync(c => c.Id == ajusteProductoTerminado.IdStockProducto);
                    if (cp == null) continue;
                    int finalSede = idSede > 0 ? idSede : (cp?.Compra?.IdSede ?? 15);
                    stockProducto = new StockProducto
                    {
                        IdCompraProducto = ajusteProductoTerminado.IdStockProducto,
                        IdSede = finalSede,
                        StockDisponible = ajusteProductoTerminado.StockNuevo
                    };
                    _context.StockProductos.Add(stockProducto);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    stockProducto.StockDisponible = ajusteProductoTerminado.StockNuevo;
                }
                ajusteProductoTerminado.IdStockProducto = stockProducto.Id;
                _context.AjusteProductoTerminados.Add(ajusteProductoTerminado);
            }
        }
    }
}

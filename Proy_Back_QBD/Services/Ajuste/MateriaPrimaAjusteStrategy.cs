using proy_back_Qbd.Models.ProductoIntermedio;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Ajuste;
using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;
using proy_back_Qbd.Util;
using proy_back_Qbd.Services.Kardex.Ledger;
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

            // Saldo por registro = libro mayor por lote (misma regla que la lista de stock y el kardex)
            var mayor = await new KardexLedgerMpService(_context).CalcularAsync(idSede, null);

            var insumoGrupos = compras.GroupBy(c => c.IdInsumo);
            var resultado = new List<TablaAjustesRes>();

            foreach (var grupo in insumoGrupos)
            {
                int idInsumo = grupo.Key;
                var partesInsumo = mayor.TryGetValue(idInsumo, out var m) ? m.Partes : new List<LedgerParte>();

                foreach (var compraInsumo in grupo)
                {
                    var ajustesDeCompra = ajustesPorCompra.ContainsKey(compraInsumo.Id) ? ajustesPorCompra[compraInsumo.Id] : new List<AjusteInsumo>();
                    var ultimoAjuste = ajustesDeCompra.FirstOrDefault();

                    decimal saldo = partesInsumo.Where(pt => pt.IdCompraInsumo == compraInsumo.Id).Sum(pt => pt.Saldo);
                    decimal baja = partesInsumo.Where(pt => pt.IdCompraInsumo == compraInsumo.Id).Sum(pt => pt.Baja);

                    string observacionAjuste = ultimoAjuste?.Observacion ?? "";

                    resultado.Add(new TablaAjustesRes
                    {
                        Codigo = UtilFamilia.CodigoInsumo(compraInsumo.IdInsumo),
                        IdCompraFamilia = compraInsumo.Id,
                        Registro = "MP" + Alfanumerico.ConvertToBase36(compraInsumo.Id),
                        Descripcion = compraInsumo.Insumo?.Descripcion ?? "",
                        Lote = compraInsumo.Lote ?? "",
                        Saldo = saldo,
                        Baja = baja,

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


            // Materia prima: el "stock" y la "diferencia" de cada ajuste se toman del libro mayor

            // (saldo según movimientos en la fecha del conteo), no de lo que mostraba la pantalla al registrarlo.

            var ledgerSvc = new KardexLedgerMpService(_context);

            var grupos = rawList

                .Select((a, i) => new { a, i, IdCompra = (a.StockInsumo != null && a.StockInsumo.IdCompraInsumo.HasValue) ? a.StockInsumo.IdCompraInsumo.Value : 0 })

                .Where(x => x.IdCompra > 0 && x.a.StockInsumo != null && comprasDict.ContainsKey(x.IdCompra))

                .GroupBy(x => new { Sede = x.a.StockInsumo!.IdSede, comprasDict[x.IdCompra].IdInsumo });

            foreach (var g in grupos)

            {

                var mayor = await ledgerSvc.CalcularInsumoAsync(g.Key.Sede, g.Key.IdInsumo);

                var conteos = mayor.Conteos.ToDictionary(c => c.Id);

                foreach (var x in g)

                {

                    if (!conteos.TryGetValue(x.a.Id, out var c)) continue;

                    response[x.i].Stock = c.StockSistema;

                    response[x.i].Diferencia = c.Diferencia;

                    response[x.i].StockFinal = c.StockContado;

                }

            }

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


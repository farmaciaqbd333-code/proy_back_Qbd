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
    /// <summary>Ajustes de inventario de la familia PI (productos intermedios).</summary>
    public class ProductoIntermedioAjusteStrategy : IAjusteFamiliaStrategy
    {
        private readonly ApiContext _context;

        public ProductoIntermedioAjusteStrategy(ApiContext context)
        {
            _context = context;
        }

        public string Familia => "PI";

        public async Task<List<TablaAjustesRes>> Listar(int idSede)
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

        /// <summary>Los ajustes de PI se registran como materia prima.</summary>
        public Task Registrar(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0)
            => new MateriaPrimaAjusteStrategy(_context).Registrar(listaAjustes, idCreador, idSede);

        /// <summary>El detalle de PI es el de materia prima.</summary>
        public Task<List<DetalleAjusteRes>> Detalle(int registroId, int? idInsumo, int? idSede)
            => new MateriaPrimaAjusteStrategy(_context).Detalle(registroId, idInsumo, idSede);
    }
}

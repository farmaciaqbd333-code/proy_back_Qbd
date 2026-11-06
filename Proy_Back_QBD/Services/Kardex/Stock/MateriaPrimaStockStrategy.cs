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
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Kardex.Stock
{
    /// <summary>Stock de la lista principal del Kardex para la familia MP (materia prima).</summary>
    public class MateriaPrimaStockStrategy : IStockFamiliaStrategy
    {
        private readonly ApiContext _context;
        private readonly ILogger<MateriaPrimaStockStrategy> _logger;

        public MateriaPrimaStockStrategy(ApiContext context, ILogger<MateriaPrimaStockStrategy> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string Familia => "MP";

        // Fusionar 2 listas para entradas compras y notas de salida
        public async Task<List<StockRes>> ObtenerStock(int idSede)
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
                .OrderBy(o => o.Id)
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
                    ci.Compra != null &&
                    ci.Compra.IdSede == idSede &&
                    (idSede == 15 || ci.Compra.FechaLab != null))
                .GroupBy(ci => ci.IdInsumo)
                .Select(g => new
                {
                    IdInsumo = g.Key,
                    Total = g.Sum(ci => ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0 ? (ci.CantidadRecibida.Value <= ci.CantidadSolicitada * 2 && ci.CantidadRecibida.Value < 500 && (ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? ci.CantidadRecibida.Value * 1000m : ci.CantidadRecibida.Value) : (ci.CantidadSolicitada * ((ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? 1000m : 1m)))
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
                        nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0))))
                .SelectMany(ci => ci.NotaSalidaInsumos
                    .Where(nsi =>
                        nsi.NotaSalida.IdSedeDestino == idSede &&
                        (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                    .Select(nsi => new
                    {
                        ci.IdInsumo,
                        Cantidad = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0 ? nsi.CantidadRecibida.Value : nsi.Cantidad)
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
                    Total = g.Sum(x => (x.Practica.HasValue && x.Practica.Value > 0) ? x.Practica.Value : x.CantidadLote)
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
                        Cantidad = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * nsi.Cantidad
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
                    x.Location,
                    x.Limite,
                    x.CantidadEnviar
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

                var siteSupply = ubicaciones
                    .FirstOrDefault(x => x.IdSupply == insumo.Id);
                var ubicacion = siteSupply?.Location;
                var limite = siteSupply?.Limite;
                var cantidadEnviar = siteSupply?.CantidadEnviar;

                _logger.LogInformation(
                    "Cálculo Insumo | IdSede: {IdSede} | IdInsumo: {IdInsumo} | Descripcion: {Descripcion} | " +
                    "EntradaCompra: {EntradaCompra} | EntradaNS: {EntradaNS} | EntradasTotal: {Entradas} | " +
                    "SalidaPI: {SalidaPI} | SalidaFormula: {SalidaFormula} | SalidaNS: {SalidaNS} | SalidasTotal: {Salidas} | " +
                    "Ajuste: {Ajuste} | Baja: {Baja} | Saldo: {Saldo} | Ubicacion: {Ubicacion} | Limite: {Limite}",
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
                    ubicacion,
                    limite
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
                    Stock = saldo,
                    Saldo = saldo,
                    Tipo = insumo.Tipo,
                    CodigoUbicacion = ubicacion,
                    Limite = limite,
                    CantidadEnviar = cantidadEnviar
                });
            }

            _logger.LogInformation(
                "FIN ObtenerMateriaPrima | IdSede: {IdSede} | CantidadResultados: {CantidadResultados}",
                idSede,
                resultado.Count
            );

            return resultado;
        }
    }
}

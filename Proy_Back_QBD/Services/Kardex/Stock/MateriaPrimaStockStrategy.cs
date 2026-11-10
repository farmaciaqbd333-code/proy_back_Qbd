using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Services.Kardex.Ledger;
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
            // 2. LIBRO MAYOR POR LOTE (fuente única de stock, ver KardexLedger)
            //    Entradas: compras + notas de salida recibidas
            //    Salidas: notas de salida + elaboración PI + fórmulas magistrales
            //    Ajustes: diferencias registradas
            //    Baja: lo que queda (según movimientos) en lotes vencidos
            // ============================================================

            var ledger = await new KardexLedgerMpService(_context).CalcularAsync(idSede, insumoIds);

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
                ledger.TryGetValue(insumo.Id, out var mayor);
                var entradas = mayor?.TotalEntradas ?? 0m;
                var salidas = mayor?.TotalSalidas ?? 0m;
                var ajuste = mayor?.TotalAjustes ?? 0m;
                var baja = mayor?.TotalBaja ?? 0m;
                var saldo = mayor?.Stock ?? 0m;

                var siteSupply = ubicaciones
                    .FirstOrDefault(x => x.IdSupply == insumo.Id);
                var ubicacion = siteSupply?.Location;
                var limite = siteSupply?.Limite;
                var cantidadEnviar = siteSupply?.CantidadEnviar;

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

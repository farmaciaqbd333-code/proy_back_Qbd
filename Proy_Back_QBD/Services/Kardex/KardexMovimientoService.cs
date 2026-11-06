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

namespace proy_back_Qbd.Services.Kardex
{
    /// <summary>Salidas y bajas de insumos, empaques, productos y economato.</summary>
    public class KardexMovimientoService : IKardexMovimientoService
    {
        private readonly ApiContext _context;
        private readonly ILogger<KardexMovimientoService> _logger;

        public KardexMovimientoService(ApiContext context, ILogger<KardexMovimientoService> logger)
        {
            _context = context;
            _logger = logger;
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

        /// <summary>
        /// Salidas por fórmula magistral: si la fórmula guardó su reparto por registro,
        /// se genera una salida por cada registro usado; si no, queda sin registro (FIFO).
        /// </summary>
        private static List<SalidaInsumoRes> ExpandirSalidasFormula(
            List<FormulaCC> formulasCC,
            string tipoSalida,
            Func<string, string?> loteDeRegistro)
        {
            var salidas = new List<SalidaInsumoRes>();
            foreach (var s in formulasCC)
            {
                var regDestino = s.Formula != null ? ("FM-" + s.Formula.Id) : "FM";
                var descDestino = s.Formula != null ? (s.Formula.FormulaMagistral ?? ("Fórmula #" + s.Formula.Id)) : "Fórmula";
                var loteFormula = s.Formula != null ? (s.Formula.Lote ?? "") : "";
                var um = s.Insumo != null ? s.Insumo.UnidadMedida : "G";
                // La salida ocurre cuando se REGISTRA en laboratorio (no cuando se creó la fórmula en el pedido)
                var lab = s.Formula?.Laboratorio;
                var fecha = lab != null
                    ? lab.FechaCreacion
                    : (s.Formula != null ? s.Formula.FechaCreacion : s.FechaCreacion);
                var usuario = lab?.Creador?.Codigo
                    ?? (s.Formula != null && s.Formula.Creador != null ? s.Formula.Creador.Codigo : "ADMIN");

                var reparto = ParsearRegistrosLote(s.RegistrosLote);
                if (reparto.Count == 0)
                {
                    salidas.Add(new SalidaInsumoRes
                    {
                        TipoSalida = tipoSalida,
                        RegistroDestino = regDestino,
                        DescripcionDestino = descDestino,
                        LoteInsumo = loteFormula,
                        RegistroLoteInsumo = "",
                        Cantidad = s.CantidadL,
                        Um = um,
                        Fecha = fecha,
                        Usuario = usuario
                    });
                    continue;
                }

                foreach (var (registro, cantidad) in reparto)
                {
                    salidas.Add(new SalidaInsumoRes
                    {
                        TipoSalida = tipoSalida,
                        RegistroDestino = regDestino,
                        DescripcionDestino = descDestino,
                        LoteInsumo = loteDeRegistro(registro) ?? loteFormula,
                        RegistroLoteInsumo = registro,
                        Cantidad = cantidad,
                        Um = um,
                        Fecha = fecha,
                        Usuario = usuario
                    });
                }
            }
            return salidas;
        }

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasInsumo(int idInsumo, int idSede, string? familia = null)
        {
            var fam = (familia ?? "").ToUpper().Trim();
            if (fam == "ME")
            {
                return await ObtenerSalidasEmpaque(idInsumo, idSede);
            }
            if (fam == "PT")
            {
                return await ObtenerSalidasPT(idInsumo, idSede);
            }
            if (fam == "ECO")
            {
                return await ObtenerSalidasEconomato(idInsumo, idSede);
            }
            if (fam == "PI")
            {
                return await ObtenerSalidasPI(idInsumo, idSede);
            }

            var resultado = await ObtenerSalidasInsumoBase(idInsumo, idSede);
            if (!resultado.Any() && string.IsNullOrEmpty(fam))
            {
                if (await _context.Insumos.AnyAsync(i => i.Id == idInsumo && i.Clasificacion == "PI"))
                {
                    return await ObtenerSalidasPI(idInsumo, idSede);
                }
                if (await _context.Empaques.AnyAsync(e => e.Id == idInsumo))
                {
                    return await ObtenerSalidasEmpaque(idInsumo, idSede);
                }
                if (await _context.Productos.AnyAsync(p => p.Id == idInsumo))
                {
                    return await ObtenerSalidasPT(idInsumo, idSede);
                }
                if (await _context.Economatos.AnyAsync(ec => ec.Id == idInsumo))
                {
                    return await ObtenerSalidasEconomato(idInsumo, idSede);
                }
            }
            return resultado;
        }

        private async Task<List<SalidaInsumoRes>> ObtenerSalidasInsumoBase(int idInsumo, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();

            try
            {
                // Obtener lotes disponibles del insumo en esta sede para asignar origen a las salidas
                var lotesDisponibles = await _context.CompraInsumos
                    .Include(ci => ci.Compra)
                    .Include(ci => ci.NotaSalidaInsumos)
                        .ThenInclude(nsi => nsi.NotaSalida)
                    .Where(w => w.IdInsumo == idInsumo &&
                        (
                            (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                            w.NotaSalidaInsumos.Any(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede && (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                        )
                    )
                    .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                    .ToListAsync();

                var batchTrackers = lotesDisponibles.Select(ci => new
                {
                    Registro = "MP" + Alfanumerico.ConvertToBase36(ci.Id),
                    Lote = ci.Lote ?? "",
                    Capacidad = ((ci.Compra != null && ci.Compra.IdSede == idSede && (idSede == 15 || ci.Compra.FechaLab != null))
                        ? (ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0 ? (ci.CantidadRecibida.Value <= ci.CantidadSolicitada * 2 && ci.CantidadRecibida.Value < 500 && (ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? ci.CantidadRecibida.Value * 1000m : ci.CantidadRecibida.Value) : (ci.CantidadSolicitada * ((ci.Um == "KG" || ci.Um == "KILOGRAMOS" || ci.Um == "Kg" || ci.Um == "Kgs" || ci.Um == "kg" || ci.Um == "KILOGRAMO") ? 1000m : 1m)))
                        : 0m) +
                        ci.NotaSalidaInsumos
                            .Where(nsi => nsi.NotaSalida != null && nsi.NotaSalida.IdSedeDestino == idSede && (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                            .Sum(nsi => ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) * ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad))
                }).Select(b => new { b.Registro, b.Lote, SaldoRestante = b.Capacidad }).ToList();

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
                        .OrderBy(x => x.ProductoIntermedio.FechaCreacion)
                        .Select(s => new SalidaInsumoRes
                        {
                            TipoSalida = "ELABORACIÓN PI",
                            RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.ProductoIntermedio.Id),
                            DescripcionDestino = s.ProductoIntermedio.Insumo != null
                                ? s.ProductoIntermedio.Insumo.Descripcion
                                : (s.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                            LoteInsumo = "",
                            RegistroLoteInsumo = "",
                            Cantidad = (s.Practica.HasValue && s.Practica.Value > 0) ? s.Practica.Value : s.CantidadLote,
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
                        RegistroDestino = s.NotaSalida != null ? ("NT" + s.NotaSalida.Id) : "NS",
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

                // 3. Salidas por Fórmulas Magistrales (respetando el registro elegido si se guardó)
                var formulasCCMp = await _context.FormulasCC
                    .Include(x => x.Formula)
                        .ThenInclude(f => f.Creador)
                    .Include(x => x.Formula)
                        .ThenInclude(f => f!.Laboratorio)
                            .ThenInclude(l => l!.Creador)
                    .Include(x => x.Insumo)
                    .Where(x => x.InsumoId == idInsumo && (x.SedeId == idSede || (x.Formula != null && x.Formula.SedeId == idSede)))
                    .OrderBy(x => x.FechaCreacion)
                    .AsNoTracking()
                    .ToListAsync();
                var formulas = ExpandirSalidasFormula(
                    formulasCCMp,
                    "FÓRMULA MAGISTRAL",
                    reg => batchTrackers.FirstOrDefault(b => string.Equals(b.Registro, reg, StringComparison.OrdinalIgnoreCase))?.Lote);

                if (formulas != null && formulas.Any())
                {
                    resultado.AddRange(formulas);
                }

                // Asignar RegistroLoteInsumo y LoteInsumo a las salidas locales (FM o PI directos) en orden FIFO
                if (batchTrackers.Any())
                {
                    // Lo que ya salió con registro explícito no está disponible para el FIFO
                    var usadoExplicito = resultado
                        .Where(r => !string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .GroupBy(r => r.RegistroLoteInsumo.Trim().ToUpper())
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.Cantidad));
                    decimal SaldoFifo(int idx) =>
                        batchTrackers[idx].SaldoRestante -
                        (usadoExplicito.TryGetValue(batchTrackers[idx].Registro.ToUpper(), out var u) ? u : 0m);

                    int currentBatchIdx = 0;
                    decimal currentBatchRemaining = SaldoFifo(0);

                    var salidasLocales = resultado
                        .Where(r => string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .OrderBy(r => r.Fecha)
                        .ToList();

                    foreach (var s in salidasLocales)
                    {
                        decimal cant = s.Cantidad;
                        while (currentBatchRemaining <= 0 && currentBatchIdx < batchTrackers.Count - 1)
                        {
                            currentBatchIdx++;
                            currentBatchRemaining = SaldoFifo(currentBatchIdx);
                        }

                        var activeBatch = batchTrackers[currentBatchIdx];
                        s.RegistroLoteInsumo = activeBatch.Registro;
                        if (!string.IsNullOrEmpty(activeBatch.Lote))
                        {
                            s.LoteInsumo = activeBatch.Lote;
                        }
                        currentBatchRemaining -= cant;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasInsumo: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        
        public async Task<List<SalidaInsumoRes>> ObtenerSalidasEmpaque(int empaqueId, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();

            try
            {
                // 1. Obtener lotes disponibles del empaque en esta sede para rastrear origen FIFO
                var compras = await _context.CompraEmpaques
                    .Include(w => w.Compra)
                    .Include(w => w.NotaSalidaEmpaques)
                        .ThenInclude(nse => nse.NotaSalida)
                    .Where(w => w.IdEmpaque == empaqueId &&
                        (
                            (w.Compra != null && w.Compra.IdSede == idSede && (idSede == 15 || w.Compra.FechaLab != null)) ||
                            w.NotaSalidaEmpaques.Any(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede &&
                                (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || nse.CantidadRecibida > 0))
                        )
                    )
                    .OrderBy(w => w.Compra != null ? w.Compra.FechaCreacion : w.FechaCreacion)
                    .ToListAsync();

                var batchTrackers = compras.Select(ce => new
                {
                    Registro = "ME" + Alfanumerico.ConvertToBase36(ce.Id),
                    Lote = ce.Lote ?? "",
                    Capacidad = ((ce.Compra != null && ce.Compra.IdSede == idSede && (idSede == 15 || ce.Compra.FechaLab != null))
                        ? (ce.CantidadRecibida.HasValue && ce.CantidadRecibida.Value > 0 ? ce.CantidadRecibida.Value : ce.CantidadSolicitada)
                        : 0m) +
                        ce.NotaSalidaEmpaques
                            .Where(nse => nse.NotaSalida != null && nse.NotaSalida.IdSedeDestino == idSede &&
                                (nse.NotaSalida.Estado == "RECIBIDO" || nse.NotaSalida.Estado == "RECEPCIONADO" || nse.NotaSalida.FechaRecepcion != null || nse.CantidadRecibida > 0))
                            .Sum(nse => (nse.CantidadRecibida > 0 ? nse.CantidadRecibida : nse.Cantidad))
                }).Select(b => new { b.Registro, b.Lote, SaldoRestante = b.Capacidad }).ToList();

                // 2. Salidas por Laboratorios (Fórmulas Magistrales)
                var laboratorios = await _context.Laboratorios
                    .Include(l => l.Formula)
                    .Include(l => l.ElaboradoU)
                    .Where(l => l.EmpaqueId == empaqueId && l.SedeId == idSede)
                    .OrderByDescending(l => l.FechaCreacion)
                    .Select(l => new SalidaInsumoRes
                    {
                        TipoSalida = "FÓRMULA MAGISTRAL",
                        RegistroDestino = l.Formula != null ? ("FM-" + l.Formula.Id) : ("LAB-" + (l.Id ?? 0)),
                        DescripcionDestino = l.Formula != null
                            ? (l.Formula.FormulaMagistral ?? ("Fórmula #" + l.Formula.Id))
                            : (!string.IsNullOrEmpty(l.Procedimiento) ? l.Procedimiento : "Laboratorio"),
                        LoteInsumo = l.Formula != null ? (l.Formula.Lote ?? "") : "",
                        RegistroLoteInsumo = "",
                        Cantidad = 1,
                        Um = "UND",
                        Fecha = l.FechaCreacion,
                        Usuario = l.ElaboradoU != null ? l.ElaboradoU.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (laboratorios != null && laboratorios.Any())
                {
                    resultado.AddRange(laboratorios);
                }

                // 3. Salidas por Consumo en Producto Intermedio (PI)
                var consumosPI = await _context.StockEmpaqueProductoIntermedios
                    .Include(x => x.EmpaqueProductoIntermedio)
                        .ThenInclude(epi => epi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                    .Include(x => x.EmpaqueProductoIntermedio)
                        .ThenInclude(epi => epi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                    .Include(x => x.StockEmpaque)
                        .ThenInclude(se => se.CompraEmpaque)
                    .Where(x => x.EmpaqueProductoIntermedio.IdEmpaque == empaqueId &&
                                x.EmpaqueProductoIntermedio.ProductoIntermedio != null &&
                                x.EmpaqueProductoIntermedio.ProductoIntermedio.IdSede == idSede)
                    .OrderByDescending(x => x.EmpaqueProductoIntermedio.ProductoIntermedio.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "ELABORACIÓN PI",
                        RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.EmpaqueProductoIntermedio.ProductoIntermedio.Id),
                        DescripcionDestino = s.EmpaqueProductoIntermedio.ProductoIntermedio.Insumo != null
                            ? s.EmpaqueProductoIntermedio.ProductoIntermedio.Insumo.Descripcion
                            : (s.EmpaqueProductoIntermedio.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                        LoteInsumo = s.StockEmpaque != null && s.StockEmpaque.CompraEmpaque != null ? (s.StockEmpaque.CompraEmpaque.Lote ?? "") : "",
                        RegistroLoteInsumo = s.StockEmpaque != null && s.StockEmpaque.CompraEmpaque != null ? "ME" + Alfanumerico.ConvertToBase36(s.StockEmpaque.CompraEmpaque.Id) : "",
                        Cantidad = s.Cantidad > 0 ? s.Cantidad : 1,
                        Um = s.UnidadMedida ?? "UND",
                        Fecha = s.EmpaqueProductoIntermedio.ProductoIntermedio.FechaCreacion,
                        Usuario = s.EmpaqueProductoIntermedio.ProductoIntermedio.Elaborador != null
                            ? s.EmpaqueProductoIntermedio.ProductoIntermedio.Elaborador.Codigo
                            : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (consumosPI != null && consumosPI.Any())
                {
                    resultado.AddRange(consumosPI);
                }
                else
                {
                    var consumosDirectosPI = await _context.EmpaqueProductoIntermedios
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                        .Include(x => x.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                        .Where(x => x.IdEmpaque == empaqueId && x.ProductoIntermedio != null && x.ProductoIntermedio.IdSede == idSede)
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
                            Cantidad = 1,
                            Um = "UND",
                            Fecha = s.ProductoIntermedio.FechaCreacion,
                            Usuario = s.ProductoIntermedio.Elaborador != null ? s.ProductoIntermedio.Elaborador.Codigo : "ADMIN"
                        })
                        .AsNoTracking()
                        .ToListAsync();

                    if (consumosDirectosPI != null && consumosDirectosPI.Any())
                    {
                        resultado.AddRange(consumosDirectosPI);
                    }
                }

                // 4. Salidas por Notas de Salida de Empaques
                var notasSalida = await _context.NotaSalidaEmpaques
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraEmpaques)
                    .Where(x => x.CompraEmpaques != null && x.CompraEmpaques.IdEmpaque == empaqueId &&
                                x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NT" + s.NotaSalida.Id) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.CompraEmpaques != null ? (s.CompraEmpaques.Lote ?? "") : (s.Lote ?? ""),
                        RegistroLoteInsumo = s.CompraEmpaques != null ? "ME" + Alfanumerico.ConvertToBase36(s.CompraEmpaques.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = s.Um ?? "UND",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }

                // 5. Asignar RegistroLoteInsumo y LoteInsumo por FIFO para las salidas locales sin lote asignado
                if (batchTrackers.Any())
                {
                    int currentBatchIdx = 0;
                    decimal currentBatchRemaining = batchTrackers[0].SaldoRestante;

                    var salidasLocales = resultado
                        .Where(r => string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .OrderBy(r => r.Fecha)
                        .ToList();

                    foreach (var s in salidasLocales)
                    {
                        decimal cant = s.Cantidad;
                        while (currentBatchRemaining <= 0 && currentBatchIdx < batchTrackers.Count - 1)
                        {
                            currentBatchIdx++;
                            currentBatchRemaining = batchTrackers[currentBatchIdx].SaldoRestante;
                        }

                        var activeBatch = batchTrackers[currentBatchIdx];
                        s.RegistroLoteInsumo = activeBatch.Registro;
                        if (!string.IsNullOrEmpty(activeBatch.Lote))
                        {
                            s.LoteInsumo = activeBatch.Lote;
                        }
                        currentBatchRemaining -= cant;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasEmpaque: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasPT(int productoId, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();
            try
            {
                var notasSalida = await _context.NotaSalidaProductos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraProducto)
                    .Where(x => x.CompraProducto != null && x.CompraProducto.IdProducto == productoId &&
                                x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NT" + s.NotaSalida.Id) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.CompraProducto != null ? (s.CompraProducto.Lote ?? "") : (s.Lote ?? ""),
                        RegistroLoteInsumo = s.CompraProducto != null ? "PT" + Alfanumerico.ConvertToBase36(s.CompraProducto.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = "UND",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasPT: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasEconomato(int economatoId, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();
            try
            {
                var notasSalida = await _context.NotaSalidaEconomatos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns.Creador)
                    .Include(x => x.CompraEconomato)
                    .Where(x => x.CompraEconomato != null && x.CompraEconomato.IdEconomato == economatoId &&
                                x.NotaSalida != null && x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NT" + s.NotaSalida.Id) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envío a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.Lote ?? "",
                        RegistroLoteInsumo = s.CompraEconomato != null ? "ECO" + Alfanumerico.ConvertToBase36(s.CompraEconomato.Id) : "",
                        Cantidad = s.Cantidad,
                        Um = "UND",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    })
                    .AsNoTracking()
                    .ToListAsync();

                if (notasSalida != null && notasSalida.Any())
                {
                    resultado.AddRange(notasSalida);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasEconomato: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<SalidaInsumoRes>> ObtenerSalidasPI(int idInsumo, int idSede)
        {
            var resultado = new List<SalidaInsumoRes>();
            try
            {
                // 1. Obtener lotes y referencias del producto intermedio
                var lotesDelInsumo = await _context.ProductosIntermedios.AsNoTracking()
                    .Where(p => p.IdInsumo == idInsumo && !string.IsNullOrEmpty(p.Lote))
                    .ToListAsync();

                var loteDict = lotesDelInsumo
                    .GroupBy(p => p.Lote!.Trim().ToUpper())
                    .ToDictionary(g => g.Key, g => g.First());

                // 2. Lotes disponibles en esta sede para rastreo FIFO
                var elaboraciones = await _context.ProductosIntermedios
                    .Where(w => w.IdInsumo == idInsumo && w.IdSede == idSede)
                    .OrderBy(w => w.FechaCreacion)
                    .ToListAsync();

                var batchTrackers = elaboraciones.Select(s => new
                {
                    Registro = "PI" + Alfanumerico.ConvertToBase36(s.Id),
                    Lote = s.Lote ?? "",
                    Capacidad = (s.TipoUso == "PI-FMG" || s.Um == "UND")
                        ? (s.LoteEstandar ?? 0)
                        : (s.LoteEstTotal ?? s.LoteEstandar ?? 0)
                }).Select(b => new { b.Registro, b.Lote, SaldoRestante = b.Capacidad }).ToList();

                var notasSalidaRecibidas = await _context.NotaSalidaInsumos.AsNoTracking()
                    .Include(nsi => nsi.NotaSalida)
                    .Where(nsi => !nsi.IdCompraInsumo.HasValue &&
                                  nsi.NotaSalida != null &&
                                  nsi.NotaSalida.IdSedeDestino == idSede &&
                                  (nsi.NotaSalida.Estado == "RECIBIDO" || nsi.NotaSalida.Estado == "RECEPCIONADO" || nsi.NotaSalida.FechaRecepcion != null || (nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0)))
                    .OrderBy(nsi => nsi.NotaSalida!.FechaRecepcion ?? nsi.NotaSalida!.FechaSalida)
                    .ToListAsync();

                foreach (var nsi in notasSalidaRecibidas)
                {
                    if (string.IsNullOrEmpty(nsi.Lote)) continue;
                    var key = nsi.Lote.Trim().ToUpper();
                    if (!loteDict.TryGetValue(key, out var piRef)) continue;

                    decimal cant = ((nsi.Um == "KG" || nsi.Um == "KILOGRAMOS" || nsi.Um == "Kg") ? 1000m : 1m) *
                        ((nsi.CantidadRecibida.HasValue && nsi.CantidadRecibida.Value > 0) ? nsi.CantidadRecibida.Value : nsi.Cantidad);

                    batchTrackers.Add(new
                    {
                        Registro = "PI" + Alfanumerico.ConvertToBase36(piRef.Id),
                        Lote = nsi.Lote,
                        SaldoRestante = cant
                    });
                }

                // 3. Salidas por Notas de Salida (Transferencias/Retornos desde esta sede)
                var notasSalida = await _context.NotaSalidaInsumos
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns!.SedeDestino)
                    .Include(x => x.NotaSalida)
                        .ThenInclude(ns => ns!.Creador)
                    .Where(x => !x.IdCompraInsumo.HasValue &&
                                x.NotaSalida != null &&
                                x.NotaSalida.IdSedeOrigen == idSede)
                    .OrderByDescending(x => x.NotaSalida!.FechaCreacion)
                    .ToListAsync();

                foreach (var s in notasSalida)
                {
                    if (string.IsNullOrEmpty(s.Lote)) continue;
                    var key = s.Lote.Trim().ToUpper();
                    if (!loteDict.TryGetValue(key, out var piRef)) continue;

                    decimal cant = s.Cantidad;
                    string um = (s.Um ?? "G").Trim().ToUpper();
                    if (um == "KG" || um == "KILOGRAMOS" || um == "Kg") cant *= 1000m;

                    resultado.Add(new SalidaInsumoRes
                    {
                        TipoSalida = "NOTA DE SALIDA",
                        RegistroDestino = s.NotaSalida != null ? ("NT" + s.NotaSalida.Id) : "NS",
                        DescripcionDestino = s.NotaSalida != null && s.NotaSalida.SedeDestino != null
                            ? $"Envio a {s.NotaSalida.SedeDestino.Nombre}"
                            : "Nota de Salida",
                        LoteInsumo = s.Lote ?? piRef.Lote ?? "",
                        RegistroLoteInsumo = "PI" + Alfanumerico.ConvertToBase36(piRef.Id),
                        Cantidad = cant,
                        Um = "G",
                        Fecha = s.NotaSalida != null ? s.NotaSalida.FechaCreacion : null,
                        Usuario = s.NotaSalida != null && s.NotaSalida.Creador != null ? s.NotaSalida.Creador.Codigo : "ADMIN"
                    });
                }

                // 4. Salidas por Consumo en Elaboracion de otros PI
                var consumosStockPI = await _context.StockInsumoProductoIntermedios
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Insumo)
                    .Include(x => x.InsumoProductoIntermedio)
                        .ThenInclude(ipi => ipi.ProductoIntermedio)
                            .ThenInclude(pi => pi.Elaborador)
                    .Include(x => x.StockInsumo)
                        .ThenInclude(si => si.ProductoIntermedio)
                    .Where(x => x.InsumoProductoIntermedio.IdInsumo == idInsumo &&
                                x.InsumoProductoIntermedio.ProductoIntermedio.IdSede == idSede)
                    .OrderBy(x => x.InsumoProductoIntermedio.ProductoIntermedio.FechaCreacion)
                    .Select(s => new SalidaInsumoRes
                    {
                        TipoSalida = "ELABORACION PI",
                        RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.InsumoProductoIntermedio.ProductoIntermedio.Id),
                        DescripcionDestino = s.InsumoProductoIntermedio.ProductoIntermedio.Insumo != null
                            ? s.InsumoProductoIntermedio.ProductoIntermedio.Insumo.Descripcion
                            : (s.InsumoProductoIntermedio.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                        LoteInsumo = s.StockInsumo != null && s.StockInsumo.ProductoIntermedio != null ? (s.StockInsumo.ProductoIntermedio.Lote ?? "") : "",
                        RegistroLoteInsumo = s.StockInsumo != null && s.StockInsumo.ProductoIntermedio != null ? "PI" + Alfanumerico.ConvertToBase36(s.StockInsumo.ProductoIntermedio.Id) : "",
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
                        .OrderBy(x => x.ProductoIntermedio.FechaCreacion)
                        .Select(s => new SalidaInsumoRes
                        {
                            TipoSalida = "ELABORACION PI",
                            RegistroDestino = "PI" + Alfanumerico.ConvertToBase36(s.ProductoIntermedio.Id),
                            DescripcionDestino = s.ProductoIntermedio.Insumo != null
                                ? s.ProductoIntermedio.Insumo.Descripcion
                                : (s.ProductoIntermedio.Lote ?? "Producto Intermedio"),
                            LoteInsumo = "",
                            RegistroLoteInsumo = "",
                            Cantidad = (s.Practica.HasValue && s.Practica.Value > 0) ? s.Practica.Value : s.CantidadLote,
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

                // 5. Salidas por Formulas Magistrales (respetando el registro elegido si se guardó)
                var formulasCCPi = await _context.FormulasCC
                    .Include(x => x.Formula)
                        .ThenInclude(f => f.Creador)
                    .Include(x => x.Formula)
                        .ThenInclude(f => f!.Laboratorio)
                            .ThenInclude(l => l!.Creador)
                    .Include(x => x.Insumo)
                    .Where(x => x.InsumoId == idInsumo && (x.SedeId == idSede || (x.Formula != null && x.Formula.SedeId == idSede)))
                    .OrderBy(x => x.FechaCreacion)
                    .AsNoTracking()
                    .ToListAsync();
                var formulas = ExpandirSalidasFormula(
                    formulasCCPi,
                    "FORMULA MAGISTRAL",
                    reg => batchTrackers.FirstOrDefault(b => string.Equals(b.Registro, reg, StringComparison.OrdinalIgnoreCase))?.Lote);

                if (formulas != null && formulas.Any())
                {
                    resultado.AddRange(formulas);
                }

                // 6. Asignar RegistroLoteInsumo y LoteInsumo por FIFO para las salidas locales
                if (batchTrackers.Any())
                {
                    // Lo que ya salió con registro explícito no está disponible para el FIFO
                    var usadoExplicito = resultado
                        .Where(r => !string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .GroupBy(r => r.RegistroLoteInsumo.Trim().ToUpper())
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.Cantidad));
                    decimal SaldoFifo(int idx) =>
                        batchTrackers[idx].SaldoRestante -
                        (usadoExplicito.TryGetValue(batchTrackers[idx].Registro.ToUpper(), out var u) ? u : 0m);

                    int currentBatchIdx = 0;
                    decimal currentBatchRemaining = SaldoFifo(0);

                    var salidasLocales = resultado
                        .Where(r => string.IsNullOrEmpty(r.RegistroLoteInsumo))
                        .OrderBy(r => r.Fecha)
                        .ToList();

                    foreach (var s in salidasLocales)
                    {
                        decimal cant = s.Cantidad;
                        while (currentBatchRemaining <= 0 && currentBatchIdx < batchTrackers.Count - 1)
                        {
                            currentBatchIdx++;
                            currentBatchRemaining = SaldoFifo(currentBatchIdx);
                        }

                        var activeBatch = batchTrackers[currentBatchIdx];
                        s.RegistroLoteInsumo = activeBatch.Registro;
                        if (string.IsNullOrEmpty(s.LoteInsumo))
                        {
                            s.LoteInsumo = activeBatch.Lote;
                        }

                        currentBatchRemaining -= cant;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerSalidasPI: {ex.Message}");
            }

            return resultado.OrderByDescending(x => x.Fecha).ToList();
        }

        public async Task<List<BajaInsumoRes>> ObtenerBajasInsumo(int idInsumo, int idSede, string? familia = null)
        {
            var resultado = new List<BajaInsumoRes>();
            try
            {
                var ahora = DateTime.UtcNow;
                var fam = (familia ?? "").ToUpper().Trim();

                if (fam == "ME")
                {
                    var empaquesVencidos = await _context.CompraEmpaques
                        .Include(ce => ce.Empaque)
                        .Include(ce => ce.Compra)
                            .ThenInclude(c => c!.Proveedor)
                        .Include(ce => ce.Compra)
                            .ThenInclude(c => c!.Sede)
                        .Include(ce => ce.StockEmpaques)
                        .Where(ce =>
                            ce.IdEmpaque == idInsumo &&
                            ce.FechaVencimiento < ahora &&
                            ce.StockEmpaques != null &&
                            ce.StockEmpaques.Any(se => se.IdSede == idSede && se.StockDisponible > 0))
                        .OrderBy(ce => ce.FechaVencimiento)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var ce in empaquesVencidos)
                    {
                        var stockSede = ce.StockEmpaques?
                            .Where(se => se.IdSede == idSede)
                            .Sum(se => se.StockDisponible) ?? 0m;

                        if (stockSede <= 0) continue;

                        int diasVencido = 0;
                        if (ce.FechaVencimiento.HasValue)
                        {
                            diasVencido = Math.Max(0, (int)(ahora.Date - ce.FechaVencimiento.Value.Date).TotalDays);
                        }

                        string reg = "ME" + Alfanumerico.ConvertToBase36(ce.Id);

                        resultado.Add(new BajaInsumoRes
                        {
                            IdCompraInsumo = ce.Id,
                            Registro = reg,
                            IdInsumo = ce.IdEmpaque,
                            Codigo = ce.IdEmpaque.ToString(),
                            Descripcion = ce.Empaque?.Descripcion ?? "",
                            Lote = ce.Lote ?? "-",
                            CantidadBaja = stockSede,
                            Um = ce.Um ?? "UND",
                            FechaFabricacion = ce.FechaFabricacion,
                            FechaVencimiento = ce.FechaVencimiento,
                            DiasVencido = diasVencido,
                            Estado = "VENCIDO",
                            Proveedor = ce.Compra?.Proveedor?.Datos ?? "",
                            SedeOrigen = ce.Compra?.Sede?.Nombre ?? "",
                            DocumentoOrigen = ce.Compra?.NumeroComprobante ?? ce.Compra?.CodFacQBD ?? (ce.Compra != null && ce.Compra.Id != 0 ? $"OC-{ce.Compra.Id}" : ""),
                            Observacion = ce.Observacion
                        });
                    }

                    return resultado;
                }

                if (fam == "PT")
                {
                    var productosVencidos = await _context.CompraProductos
                        .Include(cp => cp.Producto)
                        .Include(cp => cp.Compra)
                            .ThenInclude(c => c!.Proveedor)
                        .Include(cp => cp.Compra)
                            .ThenInclude(c => c!.Sede)
                        .Include(cp => cp.StockProductoTerminados)
                        .Where(cp =>
                            cp.IdProducto == idInsumo &&
                            cp.FechaVencimiento < ahora &&
                            cp.StockProductoTerminados.Any(sp => sp.IdSede == idSede && sp.StockDisponible > 0))
                        .OrderBy(cp => cp.FechaVencimiento)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var cp in productosVencidos)
                    {
                        var stockSede = cp.StockProductoTerminados
                            .Where(sp => sp.IdSede == idSede)
                            .Sum(sp => sp.StockDisponible);

                        if (stockSede <= 0) continue;

                        int diasVencido = 0;
                        if (cp.FechaVencimiento.HasValue)
                        {
                            diasVencido = Math.Max(0, (int)(ahora.Date - cp.FechaVencimiento.Value.Date).TotalDays);
                        }

                        string reg = "PT" + Alfanumerico.ConvertToBase36(cp.Id);

                        resultado.Add(new BajaInsumoRes
                        {
                            IdCompraInsumo = cp.Id,
                            Registro = reg,
                            IdInsumo = cp.IdProducto,
                            Codigo = cp.IdProducto.ToString(),
                            Descripcion = cp.Producto?.Descripcion ?? "",
                            Lote = cp.Lote ?? "-",
                            CantidadBaja = stockSede,
                            Um = cp.Um ?? "UND",
                            FechaFabricacion = cp.FechaFabricacion,
                            FechaVencimiento = cp.FechaVencimiento,
                            DiasVencido = diasVencido,
                            Estado = "VENCIDO",
                            Proveedor = cp.Compra?.Proveedor?.Datos ?? "",
                            SedeOrigen = cp.Compra?.Sede?.Nombre ?? "",
                            DocumentoOrigen = cp.Compra?.NumeroComprobante ?? (cp.Compra != null && cp.Compra.Id != 0 ? $"OC-{cp.Compra.Id}" : ""),
                            Observacion = cp.Observacion
                        });
                    }

                    return resultado;
                }

                if (fam == "PI")
                {
                    var pisVencidos = await _context.ProductosIntermedios
                        .Include(pi => pi.Insumo)
                        .Include(pi => pi.StockInsumo)
                        .Include(pi => pi.Sede)
                        .Where(pi =>
                            (pi.IdInsumo == idInsumo || pi.Id == idInsumo) &&
                            pi.IdSede == idSede &&
                            pi.FechaVencimiento < ahora &&
                            pi.StockInsumo != null &&
                            pi.StockInsumo.StockDisponible > 0)
                        .OrderBy(pi => pi.FechaVencimiento)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var pi in pisVencidos)
                    {
                        var stockSede = pi.StockInsumo?.StockDisponible ?? 0m;
                        if (stockSede <= 0) continue;

                        int diasVencido = 0;
                        if (pi.FechaVencimiento.HasValue)
                        {
                            diasVencido = Math.Max(0, (int)(ahora.Date - pi.FechaVencimiento.Value.Date).TotalDays);
                        }

                        string reg = "PI" + Alfanumerico.ConvertToBase36(pi.Id);

                        resultado.Add(new BajaInsumoRes
                        {
                            IdCompraInsumo = pi.Id,
                            Registro = reg,
                            IdInsumo = pi.IdInsumo,
                            Codigo = $"PI-QbD-{pi.IdInsumo}",
                            Descripcion = pi.Insumo?.Descripcion ?? "",
                            Lote = pi.Lote ?? "-",
                            CantidadBaja = stockSede,
                            Um = pi.Um ?? "G",
                            FechaFabricacion = pi.FechaEmision ?? pi.FechaCreacion,
                            FechaVencimiento = pi.FechaVencimiento,
                            DiasVencido = diasVencido,
                            Estado = "VENCIDO",
                            Proveedor = "PRODUCCIÓN INTERNA",
                            SedeOrigen = pi.Sede?.Nombre ?? "",
                            DocumentoOrigen = $"PI-{pi.Id}",
                            Observacion = pi.Procedimiento
                        });
                    }

                    // Transferencias recibidas por Nota de Salida vencidas
                    var nsVencidos = await _context.StockInsumos
                        .Include(si => si.NotaSalidaInsumo)
                            .ThenInclude(nsi => nsi!.NotaSalida)
                                .ThenInclude(ns => ns!.SedeOrigen)
                        .Where(si =>
                            si.Tipo == "PI" &&
                            si.IdSede == idSede &&
                            si.StockDisponible > 0 &&
                            si.IdNotaSalidaInsumo.HasValue &&
                            si.NotaSalidaInsumo != null &&
                            !string.IsNullOrEmpty(si.NotaSalidaInsumo.Lote))
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var sns in nsVencidos)
                    {
                        var loteKey = (sns.NotaSalidaInsumo?.Lote ?? "").Trim().ToUpper();
                        var piRef = await _context.ProductosIntermedios
                            .Include(p => p.Insumo)
                            .Include(p => p.Sede)
                            .FirstOrDefaultAsync(p => p.Lote == loteKey && (p.IdInsumo == idInsumo || p.Id == idInsumo));

                        if (piRef != null && piRef.FechaVencimiento.HasValue && piRef.FechaVencimiento.Value < ahora)
                        {
                            int diasVencido = Math.Max(0, (int)(ahora.Date - piRef.FechaVencimiento.Value.Date).TotalDays);
                            string reg = "PI" + Alfanumerico.ConvertToBase36(piRef.Id);

                            resultado.Add(new BajaInsumoRes
                            {
                                IdCompraInsumo = piRef.Id,
                                Registro = reg,
                                IdInsumo = piRef.IdInsumo,
                                Codigo = $"PI-QbD-{piRef.IdInsumo}",
                                Descripcion = piRef.Insumo?.Descripcion ?? "",
                                Lote = sns.NotaSalidaInsumo?.Lote ?? piRef.Lote ?? "-",
                                CantidadBaja = sns.StockDisponible,
                                Um = piRef.Um ?? "G",
                                FechaFabricacion = piRef.FechaEmision ?? piRef.FechaCreacion,
                                FechaVencimiento = piRef.FechaVencimiento,
                                DiasVencido = diasVencido,
                                Estado = "VENCIDO",
                                Proveedor = "TRANSFERENCIA INTERNA",
                                SedeOrigen = sns.NotaSalidaInsumo?.NotaSalida?.SedeOrigen?.Nombre ?? piRef.Sede?.Nombre ?? "",
                                DocumentoOrigen = sns.NotaSalidaInsumo?.NotaSalida != null ? $"NT-{sns.NotaSalidaInsumo.NotaSalida.Id}" : $"PI-{piRef.Id}",
                                Observacion = sns.NotaSalidaInsumo?.Observacion ?? piRef.Procedimiento
                            });
                        }
                    }

                    var comprasVencidasPI = await _context.CompraInsumos
                        .Include(ci => ci.Insumo)
                        .Include(ci => ci.Compra).ThenInclude(c => c!.Proveedor)
                        .Include(ci => ci.Compra).ThenInclude(c => c!.Sede)
                        .Include(ci => ci.StockInsumos)
                        .Where(ci =>
                            ci.IdInsumo == idInsumo &&
                            ci.FechaVencimiento < ahora &&
                            ci.StockInsumos.Any(si => si.IdSede == idSede && si.StockDisponible > 0))
                        .OrderBy(ci => ci.FechaVencimiento)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var ci in comprasVencidasPI)
                    {
                        var stockSede = ci.StockInsumos
                            .Where(si => si.IdSede == idSede)
                            .Sum(si => si.StockDisponible);

                        if (stockSede <= 0) continue;

                        int diasVencido = 0;
                        if (ci.FechaVencimiento.HasValue)
                        {
                            diasVencido = Math.Max(0, (int)(ahora.Date - ci.FechaVencimiento.Value.Date).TotalDays);
                        }

                        string reg = "PI" + Alfanumerico.ConvertToBase36(ci.Id);

                        resultado.Add(new BajaInsumoRes
                        {
                            IdCompraInsumo = ci.Id,
                            Registro = reg,
                            IdInsumo = ci.IdInsumo,
                            Codigo = $"PI-QbD-{ci.IdInsumo}",
                            Descripcion = ci.Insumo?.Descripcion ?? "",
                            Lote = ci.Lote ?? "-",
                            CantidadBaja = stockSede,
                            Um = ci.Um ?? "G",
                            FechaFabricacion = ci.FechaFabricacion,
                            FechaVencimiento = ci.FechaVencimiento,
                            DiasVencido = diasVencido,
                            Estado = "VENCIDO",
                            Proveedor = ci.Compra?.Proveedor?.Datos ?? "",
                            SedeOrigen = ci.Compra?.Sede?.Nombre ?? "",
                            DocumentoOrigen = ci.Compra?.NumeroComprobante ?? (ci.Compra != null && ci.Compra.Id != 0 ? $"OC-{ci.Compra.Id}" : ""),
                            Observacion = ci.Observacion
                        });
                    }

                    return resultado;
                }

                if (fam == "ECO")
                {
                    return resultado;
                }

                // Default: MP (Materia Prima)
                var comprasVencidas = await _context.CompraInsumos
                    .Include(ci => ci.Insumo)
                    .Include(ci => ci.Compra)
                        .ThenInclude(c => c!.Proveedor)
                    .Include(ci => ci.Compra)
                        .ThenInclude(c => c!.Sede)
                    .Include(ci => ci.StockInsumos)
                    .Where(ci =>
                        ci.IdInsumo == idInsumo &&
                        ci.FechaVencimiento < ahora &&
                        ci.StockInsumos.Any(si => si.IdSede == idSede && si.StockDisponible > 0))
                    .OrderBy(ci => ci.FechaVencimiento)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var ci in comprasVencidas)
                {
                    var stockSede = ci.StockInsumos
                        .Where(si => si.IdSede == idSede)
                        .Sum(si => si.StockDisponible);

                    if (stockSede <= 0) continue;

                    int diasVencido = 0;
                    if (ci.FechaVencimiento.HasValue)
                    {
                        diasVencido = (int)(ahora.Date - ci.FechaVencimiento.Value.Date).TotalDays;
                        if (diasVencido < 0) diasVencido = 0;
                    }

                    string reg = "MP" + Alfanumerico.ConvertToBase36(ci.Id);

                    decimal cantidadBajaReal = stockSede;
                    bool esKgBaja = ci.Um != null && (
                        string.Equals(ci.Um.Trim(), "KG", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ci.Um.Trim(), "KILOGRAMO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ci.Um.Trim(), "KILOGRAMOS", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ci.Um.Trim(), "KGS", StringComparison.OrdinalIgnoreCase)
                    );
                    decimal entradasCI = (ci.CantidadRecibida.HasValue && ci.CantidadRecibida.Value > 0)
                        ? (esKgBaja && ci.CantidadRecibida.Value <= ci.CantidadSolicitada * 2 && ci.CantidadRecibida.Value < 500 ? ci.CantidadRecibida.Value * 1000m : ci.CantidadRecibida.Value)
                        : (esKgBaja ? ci.CantidadSolicitada * 1000m : ci.CantidadSolicitada);

                    if (entradasCI > 0 && cantidadBajaReal > entradasCI * 1.5m && cantidadBajaReal >= 1000m)
                    {
                        if (cantidadBajaReal / 1000m <= entradasCI * 1.1m)
                        {
                            cantidadBajaReal /= 1000m;
                        }
                    }

                    resultado.Add(new BajaInsumoRes
                    {
                        IdCompraInsumo = ci.Id,
                        Registro = reg,
                        IdInsumo = ci.IdInsumo,
                        Codigo = $"MP-QbD-{ci.IdInsumo}",
                        Descripcion = ci.Insumo?.Descripcion ?? "",
                        Lote = ci.Lote ?? "-",
                        CantidadBaja = cantidadBajaReal,
                        Um = ci.Insumo?.UnidadMedida ?? (ci.StockInsumos.FirstOrDefault(si => si.IdSede == idSede)?.UnidadMedida ?? "G"),
                        FechaFabricacion = ci.FechaFabricacion,
                        FechaVencimiento = ci.FechaVencimiento,
                        DiasVencido = diasVencido,
                        Estado = "VENCIDO",
                        Proveedor = ci.Compra?.Proveedor?.Datos ?? "",
                        SedeOrigen = ci.Compra?.Sede?.Nombre ?? "",
                        DocumentoOrigen = ci.Compra?.NumeroComprobante ?? ci.Compra?.CodFacQBD ?? (ci.Compra != null && ci.Compra.Id != 0 ? $"OC-{ci.Compra.Id}" : ""),
                        Observacion = ci.Observacion
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener bajas de insumo {IdInsumo} en sede {IdSede} (Familia: {Familia})", idInsumo, idSede, familia);
            }

            return resultado;
        }
    }
}

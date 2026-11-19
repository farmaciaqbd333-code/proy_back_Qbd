using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Services.Kardex.Ledger;
using proy_back_Qbd.Util;
using Proy_back_QBD.Data;
using Proy_back_QBD.Service.AjusteService.Familias;

namespace proy_back_Qbd.Services.Ajuste
{
    /// <summary>Resultado del ajuste masivo de MP por sede.</summary>
    public class AjusteMasivoSedeRes
    {
        public int IdSede { get; set; }
        public string Sede { get; set; } = "";
        public int InsumosAjustados { get; set; }
        public int InsumosYaEnObjetivo { get; set; }
        /// <summary>Insumos con movimientos en la sede pero sin ningún lote vigente: no se pueden llevar al objetivo.</summary>
        public int InsumosSinLoteVigente { get; set; }
        public int RegistrosAjustados { get; set; }
        public decimal StockAntes { get; set; }
        public decimal StockDespues { get; set; }
        public List<AjusteMasivoDetalleRes> Detalle { get; set; } = new();
    }

    public class AjusteMasivoDetalleRes
    {
        public int IdInsumo { get; set; }
        public string Descripcion { get; set; } = "";
        public string Registro { get; set; } = "";
        public string? Lote { get; set; }
        public decimal SaldoAntes { get; set; }
        public decimal SaldoNuevo { get; set; }
    }

    public class AjusteMasivoRes
    {
        public bool Simulacion { get; set; }
        public decimal StockObjetivo { get; set; }
        public int TotalInsumosAjustados { get; set; }
        public int TotalRegistrosAjustados { get; set; }
        public List<AjusteMasivoSedeRes> Sedes { get; set; } = new();
    }

    /// <summary>
    /// Lleva el stock de cada insumo MP (no PI) a un valor objetivo en cada sede, con ajustes (conteos físicos)
    /// normales: el lote vigente más reciente queda en el objetivo y los demás lotes vigentes en 0.
    /// Los lotes vencidos no se tocan. Usa el libro mayor para saber el saldo actual de cada lote.
    /// </summary>
    public class AjusteMasivoMpService
    {
        public const string CONFIRMACION = "AJUSTAR-MP";
        private readonly ApiContext _context;

        public AjusteMasivoMpService(ApiContext context)
        {
            _context = context;
        }

        public async Task<AjusteMasivoRes> Ejecutar(decimal objetivo, bool aplicar, int idCreador, int? soloSede = null, bool incluirDetalle = false)
        {
            if (objetivo < 0) throw new BadRequestException("El stock objetivo no puede ser negativo.");

            var ahora = DateTime.UtcNow;
            var insumosMp = await _context.Insumos.AsNoTracking()
                .Where(i => i.Clasificacion == "MP" || i.Clasificacion == null)
                .Select(i => new { i.Id, i.Descripcion })
                .ToListAsync();
            var idsMp = insumosMp.Select(i => i.Id).ToList();
            var descripcion = insumosMp.ToDictionary(i => i.Id, i => i.Descripcion ?? "");

            var sedes = await _context.Sedes.AsNoTracking()
                .Where(s => soloSede == null || s.Id == soloSede)
                .Select(s => new { s.Id, s.Nombre })
                .OrderBy(s => s.Id)
                .ToListAsync();

            var resultado = new AjusteMasivoRes { Simulacion = !aplicar, StockObjetivo = objetivo };
            var ledgerSvc = new KardexLedgerMpService(_context);

            await using var tx = aplicar ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                foreach (var sede in sedes)
                {
                    var mayor = await ledgerSvc.CalcularAsync(sede.Id, idsMp, ahora);
                    var resSede = new AjusteMasivoSedeRes { IdSede = sede.Id, Sede = sede.Nombre ?? $"Sede {sede.Id}" };
                    var ajustes = new List<CrearAjustes>();

                    foreach (var (idInsumo, libro) in mayor)
                    {
                        if (libro.Partes.Count == 0) continue;
                        resSede.StockAntes += libro.Stock;

                        // Saldo por registro (lote) y si está vigente
                        var registros = libro.Partes
                            .GroupBy(p => p.IdCompraInsumo)
                            .Select(g => new
                            {
                                IdCompra = g.Key,
                                Saldo = g.Sum(p => p.Saldo),
                                Vigente = g.All(p => p.FechaVencimiento == null || p.FechaVencimiento.Value >= ahora),
                                Ingreso = g.Max(p => p.FechaIngreso ?? DateTime.MinValue)
                            })
                            .ToList();
                        var vigentes = registros.Where(r => r.Vigente).OrderByDescending(r => r.Ingreso).ThenByDescending(r => r.IdCompra).ToList();
                        if (vigentes.Count == 0)
                        {
                            resSede.InsumosSinLoteVigente++;
                            resSede.StockDespues += libro.Stock;
                            continue;
                        }

                        var cambios = vigentes
                            .Select((r, i) => new { r.IdCompra, r.Saldo, Nuevo = i == 0 ? objetivo : 0m })
                            .Where(c => Math.Round(c.Saldo, 3) != Math.Round(c.Nuevo, 3))
                            .ToList();
                        var stockDespues = libro.Stock - vigentes.Sum(r => r.Saldo) + objetivo;
                        resSede.StockDespues += stockDespues;
                        if (cambios.Count == 0)
                        {
                            resSede.InsumosYaEnObjetivo++;
                            continue;
                        }

                        resSede.InsumosAjustados++;
                        foreach (var c in cambios)
                        {
                            ajustes.Add(new CrearAjustes
                            {
                                IdCompraFamilia = c.IdCompra,
                                StockAnterior = Math.Round(c.Saldo, 3),
                                StockNuevo = c.Nuevo,
                                Ajuste = Math.Round(c.Nuevo - c.Saldo, 3),
                                Observacion = $"Ajuste masivo MP a {objetivo:0.###}"
                            });
                            if (incluirDetalle)
                            {
                                resSede.Detalle.Add(new AjusteMasivoDetalleRes
                                {
                                    IdInsumo = idInsumo,
                                    Descripcion = descripcion.TryGetValue(idInsumo, out var d) ? d : "",
                                    Registro = "MP" + Alfanumerico.ConvertToBase36(c.IdCompra),
                                    SaldoAntes = Math.Round(c.Saldo, 3),
                                    SaldoNuevo = c.Nuevo
                                });
                            }
                        }
                    }

                    resSede.RegistrosAjustados = ajustes.Count;
                    if (aplicar && ajustes.Count > 0)
                    {
                        await new MateriaPrimaAjusteStrategy(_context).Registrar(ajustes, idCreador, sede.Id);
                        await _context.SaveChangesAsync();
                    }
                    resultado.Sedes.Add(resSede);
                }

                if (tx != null) await tx.CommitAsync();
            }
            catch
            {
                if (tx != null) await tx.RollbackAsync();
                throw;
            }

            resultado.TotalInsumosAjustados = resultado.Sedes.Sum(s => s.InsumosAjustados);
            resultado.TotalRegistrosAjustados = resultado.Sedes.Sum(s => s.RegistrosAjustados);
            return resultado;
        }
    }
}

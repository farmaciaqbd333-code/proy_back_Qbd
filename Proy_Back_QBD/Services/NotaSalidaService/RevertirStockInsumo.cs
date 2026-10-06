using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    public partial class NotaSalidaService
    {
        private async Task RevertirStockDeNotaSalida(NotaSalida notaSalida)
        {
            int sedeOrigen = notaSalida.IdSedeOrigen > 0 ? notaSalida.IdSedeOrigen : 15;

            // 1. INSUMOS (MP y PI)
            if (notaSalida.NotaSalidaInsumos != null)
            {
                foreach (var nsi in notaSalida.NotaSalidaInsumos)
                {
                    // Si hubo stock creado en destino (confirmado), eliminarlo
                    var stocksDestino = await _context.StockInsumos
                        .Where(x => x.IdNotaSalidaInsumo == nsi.Id)
                        .ToListAsync();
                    if (stocksDestino.Any())
                    {
                        _context.StockInsumos.RemoveRange(stocksDestino);
                    }

                    // Devolver stock a la sede de origen
                    StockInsumo? stockOrigen = null;
                    if (nsi.IdCompraInsumo.HasValue && nsi.IdCompraInsumo.Value > 0)
                    {
                        stockOrigen = await _context.StockInsumos
                            .Where(x => x.IdCompraInsumo == nsi.IdCompraInsumo.Value && (x.IdSede == sedeOrigen || x.IdSede == 0))
                            .OrderBy(x => x.IdNotaSalidaInsumo != null ? 1 : 0)
                            .FirstOrDefaultAsync();

                        if (stockOrigen == null)
                        {
                            stockOrigen = await _context.StockInsumos
                                .Where(x => x.IdCompraInsumo == nsi.IdCompraInsumo.Value)
                                .OrderBy(x => x.IdNotaSalidaInsumo != null ? 1 : 0)
                                .FirstOrDefaultAsync();
                        }
                    }
                    else if (!string.IsNullOrEmpty(nsi.Lote))
                    {
                        stockOrigen = await _context.StockInsumos
                            .Include(s => s.ProductoIntermedio)
                            .Where(x => (x.ProductoIntermedio != null && x.ProductoIntermedio.Lote == nsi.Lote) && (x.IdSede == sedeOrigen || x.IdSede == 0))
                            .OrderBy(x => x.IdNotaSalidaInsumo != null ? 1 : 0)
                            .FirstOrDefaultAsync();

                        if (stockOrigen == null)
                        {
                            stockOrigen = await _context.StockInsumos
                                .Include(s => s.ProductoIntermedio)
                                .Where(x => x.ProductoIntermedio != null && x.ProductoIntermedio.Lote == nsi.Lote)
                                .OrderBy(x => x.IdNotaSalidaInsumo != null ? 1 : 0)
                                .FirstOrDefaultAsync();
                        }
                    }

                    if (stockOrigen != null)
                    {
                        string stockUm = (stockOrigen.UnidadMedida ?? "G").Trim().ToUpper();
                        string itemUm = (nsi.Um ?? "G").Trim().ToUpper();
                        decimal cantDevolver = nsi.Cantidad;

                        if ((stockUm == "G" || stockUm == "GR") && (itemUm == "KG" || itemUm == "KILOGRAMOS"))
                        {
                            cantDevolver = nsi.Cantidad * 1000m;
                        }
                        else if ((stockUm == "KG" || stockUm == "KILOGRAMOS") && (itemUm == "G" || itemUm == "GR"))
                        {
                            cantDevolver = nsi.Cantidad / 1000m;
                        }

                        stockOrigen.StockDisponible += cantDevolver;
                        if (stockOrigen.IdNotaSalidaInsumo == nsi.Id)
                        {
                            stockOrigen.IdNotaSalidaInsumo = null;
                            stockOrigen.NotaSalidaInsumo = null;
                        }
                    }
                }
            }

            // 2. EMPAQUES (ME)
            if (notaSalida.NotaSalidaEmpaques != null)
            {
                foreach (var nse in notaSalida.NotaSalidaEmpaques)
                {
                    var stocksDestino = await _context.StockEmpaques
                        .Where(x => x.IdNotaSalidaEmpaque == nse.Id)
                        .ToListAsync();
                    if (stocksDestino.Any())
                    {
                        _context.StockEmpaques.RemoveRange(stocksDestino);
                    }

                    if (nse.IdCompraEmpaque.HasValue && nse.IdCompraEmpaque.Value > 0)
                    {
                        var stockOrigen = await _context.StockEmpaques
                            .Where(x => x.IdCompraEmpaque == nse.IdCompraEmpaque.Value && (x.IdSede == sedeOrigen || x.IdSede == 0))
                            .OrderBy(x => x.IdNotaSalidaEmpaque != null ? 1 : 0)
                            .FirstOrDefaultAsync();

                        if (stockOrigen == null)
                        {
                            stockOrigen = await _context.StockEmpaques
                                .Where(x => x.IdCompraEmpaque == nse.IdCompraEmpaque.Value)
                                .OrderBy(x => x.IdNotaSalidaEmpaque != null ? 1 : 0)
                                .FirstOrDefaultAsync();
                        }

                        if (stockOrigen != null)
                        {
                            stockOrigen.StockDisponible += nse.Cantidad;
                        }
                    }
                }
            }

            // 3. ECONOMATOS (ECO)
            if (notaSalida.NotaSalidaEconomatos != null)
            {
                foreach (var nso in notaSalida.NotaSalidaEconomatos)
                {
                    var stocksDestino = await _context.StockEconomatos
                        .Where(x => x.IdNotaSalidaEconomato == nso.Id)
                        .ToListAsync();
                    if (stocksDestino.Any())
                    {
                        _context.StockEconomatos.RemoveRange(stocksDestino);
                    }

                    if (nso.IdCompraEconomato.HasValue && nso.IdCompraEconomato.Value > 0)
                    {
                        var stockOrigen = await _context.StockEconomatos
                            .Where(x => x.IdCompraEconomato == nso.IdCompraEconomato.Value && (x.IdSede == sedeOrigen || x.IdSede == 0))
                            .OrderBy(x => x.IdNotaSalidaEconomato != null ? 1 : 0)
                            .FirstOrDefaultAsync();

                        if (stockOrigen == null)
                        {
                            stockOrigen = await _context.StockEconomatos
                                .Where(x => x.IdCompraEconomato == nso.IdCompraEconomato.Value)
                                .OrderBy(x => x.IdNotaSalidaEconomato != null ? 1 : 0)
                                .FirstOrDefaultAsync();
                        }

                        if (stockOrigen != null)
                        {
                            stockOrigen.StockDisponible += nso.Cantidad;
                        }
                    }
                }
            }

            // 4. PRODUCTOS TERMINADOS (PT)
            if (notaSalida.NotaSalidaProductos != null)
            {
                foreach (var nsp in notaSalida.NotaSalidaProductos)
                {
                    var stocksDestino = await _context.StockProductos
                        .Where(x => x.IdNotaSalidaProducto == nsp.Id)
                        .ToListAsync();
                    if (stocksDestino.Any())
                    {
                        _context.StockProductos.RemoveRange(stocksDestino);
                    }

                    if (nsp.IdCompraProducto.HasValue && nsp.IdCompraProducto.Value > 0)
                    {
                        var stockOrigen = await _context.StockProductos
                            .Where(x => x.IdCompraProducto == nsp.IdCompraProducto.Value && (x.IdSede == sedeOrigen || x.IdSede == 0))
                            .OrderBy(x => x.IdNotaSalidaProducto != null ? 1 : 0)
                            .FirstOrDefaultAsync();

                        if (stockOrigen == null)
                        {
                            stockOrigen = await _context.StockProductos
                                .Where(x => x.IdCompraProducto == nsp.IdCompraProducto.Value)
                                .OrderBy(x => x.IdNotaSalidaProducto != null ? 1 : 0)
                                .FirstOrDefaultAsync();
                        }

                        if (stockOrigen != null)
                        {
                            stockOrigen.StockDisponible += nsp.Cantidad;
                        }
                    }
                }
            }
        }
    }
}

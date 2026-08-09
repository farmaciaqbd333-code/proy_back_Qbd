using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    public partial class NotaSalidaService
    {
        private async Task RevertirStockInsumo(int idNotaSalida)
        {
            var stocksDestino = await _context.StockInsumos
                .Where(x => x.IdNotaSalidaInsumo != null &&
                            x.IdNotaSalidaInsumo == idNotaSalida)
                .ToListAsync();


            foreach (var stockDestino in stocksDestino)
            {
                // Buscar stock origen
                var stockOrigen = await _context.StockInsumos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraInsumo == stockDestino.IdCompraInsumo &&
                        x.IdSede != stockDestino.IdSede &&
                        x.IdNotaSalidaInsumo == null);


                if (stockOrigen != null)
                {
                    // devolver cantidad
                    stockOrigen.StockDisponible += stockDestino.StockDisponible;
                }


                // eliminar movimiento creado
                _context.StockInsumos.Remove(stockDestino);
            }
        }
        private async Task RevertirStockEmpaque(int idNotaSalida)
        {
            var stocksDestino = await _context.StockEmpaques
                .Where(x => x.IdNotaSalidaEmpaque != null &&
                            x.IdNotaSalidaEmpaque == idNotaSalida)
                .ToListAsync();


            foreach (var stockDestino in stocksDestino)
            {
                // Buscar stock origen
                var stockOrigen = await _context.StockEmpaques
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEmpaque == stockDestino.IdCompraEmpaque &&
                        x.IdSede != stockDestino.IdSede &&
                        x.IdNotaSalidaEmpaque == null);


                if (stockOrigen != null)
                {
                    // devolver cantidad
                    stockOrigen.StockDisponible += stockDestino.StockDisponible;
                }


                // eliminar movimiento creado
                _context.StockEmpaques.Remove(stockDestino);
            }
        }
        private async Task RevertirStockEconomato(int idNotaSalida)
        {
            var stocksDestino = await _context.StockEconomatos
                .Where(x => x.IdNotaSalidaEconomato != null &&
                            x.IdNotaSalidaEconomato == idNotaSalida)
                .ToListAsync();


            foreach (var stockDestino in stocksDestino)
            {
                // Buscar stock origen
                var stockOrigen = await _context.StockEconomatos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEconomato == stockDestino.IdCompraEconomato &&
                        x.IdSede != stockDestino.IdSede &&
                        x.IdNotaSalidaEconomato == null);


                if (stockOrigen != null)
                {
                    // devolver cantidad
                    stockOrigen.StockDisponible += stockDestino.StockDisponible;
                }


                // eliminar movimiento creado
                _context.StockEconomatos.Remove(stockDestino);
            }
        }
        private async Task RevertirStockProducto(int idNotaSalida)
        {
            var stocksDestino = await _context.StockProductos
                .Where(x => x.IdNotaSalidaProducto != null &&
                            x.IdNotaSalidaProducto == idNotaSalida)
                .ToListAsync();


            foreach (var stockDestino in stocksDestino)
            {
                // Buscar stock origen
                var stockOrigen = await _context.StockProductos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraProducto == stockDestino.IdCompraProducto &&
                        x.IdSede != stockDestino.IdSede &&
                        x.IdNotaSalidaProducto == null);


                if (stockOrigen != null)
                {
                    // devolver cantidad
                    stockOrigen.StockDisponible += stockDestino.StockDisponible;
                }


                // eliminar movimiento creado
                _context.StockProductos.Remove(stockDestino);
            }
        }


    }
}
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    public partial class NotaSalidaService
    {
        private async Task EliminarStockInsumo(
        List<NotaSalidaInsumo> detalles)
        {
            var idsDetalle = detalles
                .Select(x => x.Id)
                .ToList();


            var notaSalidaDestino = await _context.NotaSalidaInsumos
                .Where(x => idsDetalle.Contains(x.Id))
                .ToListAsync();


            foreach (var NSDestino in notaSalidaDestino)
            {
                // buscar stock origen
                var stockOrigen = await _context.StockInsumos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraInsumo == NSDestino.IdCompraInsumo &&
                        x.IdNotaSalidaInsumo == null &&
                        x.IdSede == NSDestino.NotaSalida.IdSedeOrigen);


                if (stockOrigen != null)
                {
                    stockOrigen.StockDisponible += NSDestino.Cantidad;
                }


            }
        }
        private async Task EliminarStockEmpaque(
        List<NotaSalidaEmpaque> detalles)
        {
            var idsDetalle = detalles
                .Select(x => x.Id)
                .ToList();


            var notaSalidaEmpaques = await _context.NotaSalidaEmpaques
                .Where(x => idsDetalle.Contains(x.Id))
                .ToListAsync();


            foreach (var notaSalidaEmpaque in notaSalidaEmpaques)
            {
                // buscar stock origen
                var stockOrigen = await _context.StockEmpaques
                    .FirstOrDefaultAsync(x =>
                        x.IdStockEmpaque == notaSalidaEmpaque.IdCompraEmpaque &&
                        x.IdNotaSalidaEmpaque == null &&
                        x.IdSede == notaSalidaEmpaque.NotaSalida.IdSedeOrigen);


                if (stockOrigen != null)
                {
                    stockOrigen.StockDisponible += notaSalidaEmpaque.Cantidad;
                }


            }
        }
        private async Task EliminarStockEconomato(
        List<NotaSalidaEconomato> detalles)
        {
            var idsNotaSalidaEco = detalles
                .Select(x => x.Id)
                .ToList();


            var notaSalidaEconomatos = await _context.NotaSalidaEconomatos
                .Where(x => idsNotaSalidaEco.Contains(x.Id))
                .ToListAsync();


            foreach (var notaSalidaEconomato in notaSalidaEconomatos)
            {
                // buscar stock origen
                var stockOrigen = await _context.StockEconomatos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEconomato == notaSalidaEconomato.IdCompraEconomato &&
                        x.IdNotaSalidaEconomato == null &&
                        x.IdSede == notaSalidaEconomato.NotaSalida.IdSedeOrigen);


                if (stockOrigen != null)
                {
                    stockOrigen.StockDisponible += notaSalidaEconomato.Cantidad;
                }

            }
        }
        private async Task EliminarStockProducto(
        List<NotaSalidaProducto> detalles)
        {
            //Seleccionar ids de notas de salida
            var idsNotaSalidaProd = detalles
                .Select(x => x.Id)
                .ToList();

            //Buscar notas de salida
            var notasSalidaProductos = await _context.NotaSalidaProductos
                .Where(x => idsNotaSalidaProd.Contains(x.Id))
                .ToListAsync();


            foreach (var notasSalidaProducto in notasSalidaProductos)
            {
                // buscar stock origen
                var stockOrigen = await _context.StockProductos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraProducto == notasSalidaProducto.IdCompraProducto &&
                        x.IdNotaSalidaProducto == null &&
                        x.IdSede == notasSalidaProducto.NotaSalida.IdSedeOrigen);


                if (stockOrigen != null)
                {
                    //Eliminamos cantidad mencionada
                    stockOrigen.StockDisponible += notasSalidaProducto.Cantidad;
                }

            }
        }


    }
}
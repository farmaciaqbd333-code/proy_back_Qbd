using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    public partial class NotaSalidaService
    {
        private async Task ProcesarInsumos(
    List<ConfirmarArticulosReq> articulos,
    int idSedeOrigen,
    int idSedeDestino)
        {
            _logger.LogInformation(
                "Inicio procesamiento de insumos. Cantidad: {Cantidad}, SedeOrigen: {SedeOrigen}, SedeDestino: {SedeDestino}",
                articulos.Count,
                idSedeOrigen,
                idSedeDestino);

            foreach (var item in articulos)
            {
                _logger.LogInformation(
                    "Procesando insumo. IdCompraInsumo: {IdCompraInsumo}, Cantidad: {Cantidad}",
                    item.IdCompraArticulo,
                    item.CantidadRecibida);

                var stockOrigen = await _context.StockInsumos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraInsumo == item.IdCompraArticulo &&
                        x.IdSede == idSedeOrigen &&
                        x.IdNotaSalidaInsumo == null);

                if (stockOrigen == null)
                    continue;

                // Descontar stock origen
                stockOrigen.StockDisponible -= item.CantidadRecibida;

                // Crear stock destino
                var stockDestino = new StockInsumo
                {
                    IdCompraInsumo = item.IdCompraArticulo,
                    Tipo = "MP",
                    StockDisponible = item.CantidadRecibida,
                    UnidadMedida = item.UnidadMedida ?? stockOrigen.UnidadMedida,
                    IdSede = idSedeDestino,
                    IdNotaSalidaInsumo = item.IdNotaSalidaArticulo
                };

                _context.StockInsumos.Add(stockDestino);

                _logger.LogInformation(
                    "Insumo procesado correctamente. IdCompraInsumo: {IdCompraInsumo}, " +
                    "StockDescontado: {Cantidad}, StockRestanteOrigen: {StockRestante}, SedeDestino: {SedeDestino}",
                    item.IdCompraArticulo,
                    item.CantidadRecibida,
                    stockOrigen.StockDisponible,
                    idSedeDestino);
            }

            _logger.LogInformation("Final procesamiento de insumos.");
        }
        private async Task ProcesarProductos(
    List<ConfirmarArticulosReq> articulos,
    int idSedeOrigen,
    int idSedeDestino)
        {
            foreach (var item in articulos)
            {
                var stockOrigen = await _context.StockProductos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraProducto == item.IdCompraArticulo &&
                        x.IdSede == idSedeOrigen &&
                        x.IdNotaSalidaProducto == null);

                if (stockOrigen == null)
                    continue;

                // Descontar stock origen
                stockOrigen.StockDisponible -= item.CantidadRecibida;

                // Crear stock destino
                var stockDestino = new StockProducto
                {
                    IdCompraProducto = item.IdCompraArticulo,
                    StockDisponible = item.CantidadRecibida,
                    UnidadMedida = item.UnidadMedida ?? stockOrigen.UnidadMedida,
                    IdSede = idSedeDestino,
                    IdNotaSalidaProducto = item.IdNotaSalidaArticulo
                };

                _context.StockProductos.Add(stockDestino);
            }
        }
        private async Task ProcesarEconomatos(
    List<ConfirmarArticulosReq> articulos,
    int idSedeOrigen,
    int idSedeDestino)
        {
            foreach (var item in articulos)
            {
                var stockOrigen = await _context.StockEconomatos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEconomato == item.IdCompraArticulo &&
                        x.IdSede == idSedeOrigen &&
                        x.IdNotaSalidaEconomato == null);

                if (stockOrigen == null)
                    continue;

                // Descontar stock origen
                stockOrigen.StockDisponible -= item.CantidadRecibida;

                // Crear stock destino
                var stockDestino = new StockEconomato
                {
                    IdCompraEconomato = item.IdCompraArticulo,
                    StockDisponible = item.CantidadRecibida,
                    UnidadMedida = item.UnidadMedida ?? stockOrigen.UnidadMedida,
                    IdSede = idSedeDestino,
                    IdNotaSalidaEconomato = item.IdNotaSalidaArticulo
                };

                _context.StockEconomatos.Add(stockDestino);
            }
        }
        private async Task ProcesarEmpaques(
    List<ConfirmarArticulosReq> articulos,
    int idSedeOrigen,
    int idSedeDestino)
        {
            foreach (var item in articulos)
            {
                var stockOrigen = await _context.StockEmpaques
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEmpaque == item.IdCompraArticulo &&
                        x.IdSede == idSedeOrigen &&
                        x.IdNotaSalidaEmpaque == null);

                if (stockOrigen == null)
                    continue;

                // Descontar stock origen
                stockOrigen.StockDisponible -= item.CantidadRecibida;

                // Crear stock destino
                var stockDestino = new StockEmpaque
                {
                    IdCompraEmpaque = item.IdCompraArticulo,
                    StockDisponible = item.CantidadRecibida,
                    UnidadMedida = item.UnidadMedida ?? stockOrigen.UnidadMedida,
                    IdSede = idSedeDestino,
                    IdNotaSalidaEmpaque = item.IdNotaSalidaArticulo
                };

                _context.StockEmpaques.Add(stockDestino);
            }
        }
    }
}
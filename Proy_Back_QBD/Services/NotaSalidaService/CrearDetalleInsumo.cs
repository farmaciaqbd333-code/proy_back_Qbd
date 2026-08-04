using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    public partial class NotaSalidaService
    {
        private async Task CrearDetalleInsumo(
     int idNotaSalida,
     CreateReq request,
     NotaSalidaFamiliasCreateReq item)
        {
            _logger.LogInformation(
                "Iniciando creación de detalle de insumo. NotaSalida={NotaSalida}, Registro={Registro}, Cantidad={Cantidad}, SedeOrigen={SedeOrigen}, SedeDestino={SedeDestino}",
                idNotaSalida,
                item.Registro,
                item.Cantidad,
                request.IdSedeOrigen,
                request.IdSedeDestino);

            var stockOrigen = await _context.StockInsumos
                .FirstOrDefaultAsync(x =>
                    x.IdCompraInsumo == item.Registro &&
                    x.IdSede == request.IdSedeOrigen);

            if (stockOrigen == null)
            {
                _logger.LogWarning(
                    "No se encontró stock. Registro={Registro}, SedeOrigen={SedeOrigen}",
                    item.Registro,
                    request.IdSedeOrigen);

                throw new Exception("No existe stock en la sede proveniente.");
            }

            _logger.LogInformation(
                "Stock encontrado. StockActual={StockActual}, CantidadSolicitada={CantidadSolicitada}",
                stockOrigen.StockDisponible,
                item.Cantidad);

            if (stockOrigen.StockDisponible < item.Cantidad)
            {
                _logger.LogWarning(
                    "Stock insuficiente. Disponible={Disponible}, Solicitado={Solicitado}",
                    stockOrigen.StockDisponible,
                    item.Cantidad);

                throw new Exception("Stock insuficiente.");
            }

            var detalle = new NotaSalidaInsumo
            {
                IdNotaSalida = idNotaSalida,
                IdCompraInsumo = item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                Paquete = item.Paquete,
                PesoBruto = item.PesoBruto,
                Tara = item.Tara,
                PesoNeto = item.PesoNeto,
                IdCreador = request.IdCreador,
                CantidadPaquete = item.CantidadPaquete
            };

            _context.NotaSalidaInsumos.Add(detalle);

            _logger.LogInformation(
                "Detalle de NotaSalidaInsumo agregado. Registro={Registro}",
                item.Registro);


            _logger.LogInformation(
                "Finalizó creación de detalle de insumo. NotaSalida={NotaSalida}",
                idNotaSalida);
        }
        private async Task CrearDetalleEconomato(
             int idNotaSalida,
             CreateReq request,
             NotaSalidaFamiliasCreateReq item)
        {
            var stockOrigen = await _context.StockEconomatos
                .FirstOrDefaultAsync(x =>
                    x.IdCompraEconomato == item.Registro &&
                    x.IdSede == request.IdSedeOrigen);

            if (stockOrigen == null)
                throw new Exception("No existe stock en la sede proveniente.");

            if (stockOrigen.StockDisponible < item.Cantidad)
                throw new Exception("Stock insuficiente.");

            var detalle = new NotaSalidaEconomato
            {
                IdNotaSalida = idNotaSalida,
                IdCompraEconomato = item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                Paquete = item.Paquete,
                IdCreador = request.IdCreador,
                CantidadPaquete = item.CantidadPaquete
            };

            _context.NotaSalidaEconomatos.Add(detalle);
        }
        private async Task CrearDetalleEmpaque(
            int idNotaSalida,
            CreateReq request,
            NotaSalidaFamiliasCreateReq item)
        {
            var stockOrigen = await _context.StockEmpaques
                .FirstOrDefaultAsync(x =>
                    x.IdCompraEmpaque == item.Registro &&
                    x.IdSede == request.IdSedeOrigen);

            if (stockOrigen == null)
                throw new Exception("No existe stock en la sede proveniente.");

            if (stockOrigen.StockDisponible < item.Cantidad)
                throw new Exception("Stock insuficiente.");

            var detalle = new NotaSalidaEmpaque
            {
                IdNotaSalida = idNotaSalida,
                IdCompraEmpaque = item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                Paquete = item.Paquete,
                IdCreador = request.IdCreador,
                CantidadPaquete = item.CantidadPaquete
            };

            _context.NotaSalidaEmpaques.Add(detalle);


        }
        private async Task CrearDetalleProducto(
            int idNotaSalida,
            CreateReq request,
            NotaSalidaFamiliasCreateReq item)
        {
            var stockOrigen = await _context.StockProductos
                .FirstOrDefaultAsync(x =>
                    x.IdCompraProducto == item.Registro &&
                    x.IdSede == request.IdSedeOrigen);

            if (stockOrigen == null)
                throw new Exception("No existe stock en la sede proveniente.");

            if (stockOrigen.StockDisponible < item.Cantidad)
                throw new Exception("Stock insuficiente.");

            var detalle = new NotaSalidaProducto
            {
                IdNotaSalida = idNotaSalida,
                IdCompraProducto = item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                Paquete = item.Paquete,
                IdCreador = request.IdCreador,
                CantidadPaquete = item.CantidadPaquete
            };

            _context.NotaSalidaProductos.Add(detalle);


        }


    }
}
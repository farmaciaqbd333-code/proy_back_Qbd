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
                "Iniciando creación de detalle de insumo/PI. NotaSalida={NotaSalida}, Registro={Registro}, Cantidad={Cantidad}, Familia={Familia}, SedeOrigen={SedeOrigen}",
                idNotaSalida,
                item.Registro,
                item.Cantidad,
                item.Familia,
                request.IdSedeOrigen);

            bool isPI = (item.Familia ?? "").Trim().ToUpper() == "PI";

            var stockOrigen = await _context.StockInsumos
                .Include(s => s.ProductoIntermedio)
                .FirstOrDefaultAsync(x =>
                    (isPI ? (x.IdProductoIntermedio == item.Registro || (x.ProductoIntermedio != null && x.ProductoIntermedio.Id == item.Registro)) : x.IdCompraInsumo == item.Registro) &&
                    (request.IdSedeOrigen == 0 || request.IdSedeOrigen == 15 || x.IdSede == request.IdSedeOrigen));

            if (stockOrigen == null)
            {
                stockOrigen = await _context.StockInsumos
                    .Include(s => s.ProductoIntermedio)
                    .FirstOrDefaultAsync(x => isPI ? (x.IdProductoIntermedio == item.Registro || (x.ProductoIntermedio != null && x.ProductoIntermedio.Id == item.Registro)) : x.IdCompraInsumo == item.Registro);
            }

            if (stockOrigen == null)
            {
                _logger.LogWarning(
                    "No se encontró stock. Registro={Registro}, SedeOrigen={SedeOrigen}, Familia={Familia}",
                    item.Registro,
                    request.IdSedeOrigen,
                    item.Familia);

                throw new Exception($"No existe stock en la sede origen para el registro {item.Registro} de {item.Familia}.");
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

                throw new Exception($"Stock insuficiente. Disponible: {stockOrigen.StockDisponible}, Solicitado: {item.Cantidad}");
            }

            var detalle = new NotaSalidaInsumo
            {
                IdNotaSalida = idNotaSalida,
                IdCompraInsumo = isPI ? null : item.Registro,
                Cantidad = item.Cantidad,
                Um = item.Um,
                Lote = isPI ? (stockOrigen.ProductoIntermedio?.Lote ?? stockOrigen.IdProductoIntermedio?.ToString()) : null,
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaInsumos = item.Paquetes?.Select(p => new PaqueteNotaSalidaInsumo
                {
                    IdCreador = request.IdCreador,
                    CantidadPaquete = p.CantidadPaquete,
                    Peso = p.Peso,
                    Tara = p.Tara,
                    Um = p.Um,
                    PesoNeto = p.PesoNeto,
                    PesoBruto = p.PesoBruto
                }).ToList()
            };

            _context.NotaSalidaInsumos.Add(detalle);

            stockOrigen.StockDisponible -= item.Cantidad;
            stockOrigen.NotaSalidaInsumo = detalle;

            _logger.LogInformation(
                "Detalle de NotaSalidaInsumo agregado. Registro={Registro}, isPI={isPI}",
                item.Registro, isPI);

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
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaEconomatos = item.Paquetes?.Select(p => new PaqueteNotaSalidaEconomato
                {
                    IdCreador = request.IdCreador,
                    CantidadPaquete = p.CantidadPaquete,
                    Peso = p.Peso,
                    Tara = p.Tara,
                    Um = p.Um,
                    PesoNeto = p.PesoNeto,
                    PesoBruto = p.PesoBruto
                }).ToList()
            };

            _context.NotaSalidaEconomatos.Add(detalle);
            stockOrigen.StockDisponible -= item.Cantidad;
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
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaEmpaques = item.Paquetes?.Select(p => new PaqueteNotaSalidaEmpaque
                {
                    IdCreador = request.IdCreador,
                    CantidadPaquete = p.CantidadPaquete,
                    Peso = p.Peso,
                    Tara = p.Tara,
                    Um = p.Um,
                    PesoNeto = p.PesoNeto,
                    PesoBruto = p.PesoBruto
                }).ToList()
            };

            _context.NotaSalidaEmpaques.Add(detalle);
            stockOrigen.StockDisponible -= item.Cantidad;
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
                IdCreador = request.IdCreador,
                PaqueteNotaSalidaProductos = item.Paquetes?.Select(p => new PaqueteNotaSalidaProducto
                {
                    IdCreador = request.IdCreador,
                    CantidadPaquete = p.CantidadPaquete,
                    Peso = p.Peso,
                    Tara = p.Tara,
                    Um = p.Um,
                    PesoNeto = p.PesoNeto,
                    PesoBruto = p.PesoBruto
                }).ToList()
            };

            _context.NotaSalidaProductos.Add(detalle);
            stockOrigen.StockDisponible -= item.Cantidad;
        }
    }
}
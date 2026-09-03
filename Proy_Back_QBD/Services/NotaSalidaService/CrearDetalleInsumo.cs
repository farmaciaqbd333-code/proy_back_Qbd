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
                "Iniciando creación de detalle de insumo/PI. NotaSalida={NotaSalida}, Registro={Registro}, Cantidad={Cantidad}, Um={Um}, Familia={Familia}, SedeOrigen={SedeOrigen}",
                idNotaSalida,
                item.Registro,
                item.Cantidad,
                item.Um,
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

                throw new Exception($"No existe stock registrado para el registro {item.Registro} de {item.Familia}.");
            }

            string stockUm = (stockOrigen.UnidadMedida ?? "G").Trim().ToUpper();
            string itemUm = (item.Um ?? "G").Trim().ToUpper();
            decimal cantDescontar = item.Cantidad;

            // Unit conversions between G and KG
            if ((stockUm == "G" || stockUm == "GR") && itemUm == "KG")
            {
                cantDescontar = item.Cantidad * 1000m;
            }
            else if (stockUm == "KG" && (itemUm == "G" || itemUm == "GR"))
            {
                cantDescontar = item.Cantidad / 1000m;
            }
            else if (stockOrigen.StockDisponible < item.Cantidad && item.Cantidad <= stockOrigen.StockDisponible * 1000m && (itemUm == "G" || itemUm == "GR"))
            {
                cantDescontar = item.Cantidad / 1000m;
            }
            else if (stockOrigen.StockDisponible > 0 && stockOrigen.StockDisponible >= item.Cantidad * 1000m && itemUm == "KG")
            {
                cantDescontar = item.Cantidad * 1000m;
            }

            _logger.LogInformation(
                "Stock encontrado. StockActual={StockActual} {StockUm}, CantidadSolicitada={CantidadSolicitada} {ItemUm}, CantidadDescontar={CantidadDescontar}",
                stockOrigen.StockDisponible,
                stockUm,
                item.Cantidad,
                itemUm,
                cantDescontar);

            if (stockOrigen.StockDisponible < cantDescontar)
            {
                _logger.LogWarning(
                    "Stock insuficiente. Disponible={Disponible} {StockUm}, Solicitado={Solicitado} {ItemUm} (descuento={CantDescontar})",
                    stockOrigen.StockDisponible,
                    stockUm,
                    item.Cantidad,
                    itemUm,
                    cantDescontar);

                throw new Exception($"Stock insuficiente. Disponible: {stockOrigen.StockDisponible} {stockUm}, Solicitado: {item.Cantidad} {itemUm}");
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

            stockOrigen.StockDisponible -= cantDescontar;
            stockOrigen.NotaSalidaInsumo = detalle;

            _logger.LogInformation(
                "Detalle de NotaSalidaInsumo agregado exitosamente. Registro={Registro}, isPI={isPI}, NuevoStock={NuevoStock}",
                item.Registro, isPI, stockOrigen.StockDisponible);

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
                    (request.IdSedeOrigen == 0 || request.IdSedeOrigen == 15 || x.IdSede == request.IdSedeOrigen));

            if (stockOrigen == null)
            {
                stockOrigen = await _context.StockEconomatos
                    .FirstOrDefaultAsync(x => x.IdCompraEconomato == item.Registro);
            }

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
                    (request.IdSedeOrigen == 0 || request.IdSedeOrigen == 15 || x.IdSede == request.IdSedeOrigen));

            if (stockOrigen == null)
            {
                stockOrigen = await _context.StockEmpaques
                    .FirstOrDefaultAsync(x => x.IdCompraEmpaque == item.Registro);
            }

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
                    (request.IdSedeOrigen == 0 || request.IdSedeOrigen == 15 || x.IdSede == request.IdSedeOrigen));

            if (stockOrigen == null)
            {
                stockOrigen = await _context.StockProductos
                    .FirstOrDefaultAsync(x => x.IdCompraProducto == item.Registro);
            }

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
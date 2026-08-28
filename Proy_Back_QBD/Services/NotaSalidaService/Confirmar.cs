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
                    "Procesando insumo. IdCompraInsumo: {IdCompraInsumo}, CantidadRecibida: {Cantidad}",
                    item.IdCompraArticulo,
                    item.CantidadRecibida);

                // Actualizar detalle de la nota de salida con cantidad recibida y observación
                var nsInsumo = await _context.NotaSalidaInsumos.FirstOrDefaultAsync(x => x.Id == item.IdNotaSalidaArticulo);
                if (nsInsumo == null && item.IdCompraArticulo > 0)
                {
                    nsInsumo = await _context.NotaSalidaInsumos.FirstOrDefaultAsync(x => x.IdCompraInsumo == item.IdCompraArticulo);
                }

                decimal cantidadDespachada = item.CantidadRecibida;
                if (nsInsumo != null)
                {
                    cantidadDespachada = nsInsumo.Cantidad;
                    nsInsumo.CantidadRecibida = item.CantidadRecibida;
                    nsInsumo.Observacion = item.Observacion;

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsInsumo.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                // Verificar si ya existe stock destino para esta nota/artículo (modificación)
                var stockDestino = await _context.StockInsumos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraInsumo == item.IdCompraArticulo &&
                        x.IdSede == idSedeDestino &&
                        x.IdNotaSalidaInsumo == item.IdNotaSalidaArticulo);

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = item.CantidadRecibida;
                    stockDestino.UnidadMedida = item.UnidadMedida ?? stockDestino.UnidadMedida;
                }
                else
                {
                    var stockOrigen = await _context.StockInsumos
                        .FirstOrDefaultAsync(x =>
                            x.IdCompraInsumo == item.IdCompraArticulo &&
                            x.IdSede == idSedeOrigen &&
                            x.IdNotaSalidaInsumo == null);

                    if (stockOrigen != null)
                    {
                        stockOrigen.StockDisponible -= cantidadDespachada;
                    }

                    stockDestino = new StockInsumo
                    {
                        IdCompraInsumo = item.IdCompraArticulo,
                        Tipo = "MP",
                        StockDisponible = item.CantidadRecibida,
                        UnidadMedida = item.UnidadMedida ?? (stockOrigen != null ? stockOrigen.UnidadMedida : "G"),
                        IdSede = idSedeDestino,
                        IdNotaSalidaInsumo = item.IdNotaSalidaArticulo
                    };

                    _context.StockInsumos.Add(stockDestino);
                }

                _logger.LogInformation(
                    "Insumo procesado correctamente. IdCompraInsumo: {IdCompraInsumo}, " +
                    "StockDestino: {CantidadRecibida}, SedeDestino: {SedeDestino}",
                    item.IdCompraArticulo,
                    item.CantidadRecibida,
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
                var nsProd = await _context.NotaSalidaProductos.FirstOrDefaultAsync(x => x.Id == item.IdNotaSalidaArticulo);
                if (nsProd == null && item.IdCompraArticulo > 0)
                {
                    nsProd = await _context.NotaSalidaProductos.FirstOrDefaultAsync(x => x.IdCompraProducto == item.IdCompraArticulo);
                }

                decimal cantidadDespachada = item.CantidadRecibida;
                if (nsProd != null)
                {
                    cantidadDespachada = nsProd.Cantidad;
                    nsProd.CantidadRecibida = item.CantidadRecibida;
                    nsProd.Observacion = item.Observacion;

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsProd.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                var stockDestino = await _context.StockProductos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraProducto == item.IdCompraArticulo &&
                        x.IdSede == idSedeDestino &&
                        x.IdNotaSalidaProducto == item.IdNotaSalidaArticulo);

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = item.CantidadRecibida;
                    stockDestino.UnidadMedida = item.UnidadMedida ?? stockDestino.UnidadMedida;
                }
                else
                {
                    var stockOrigen = await _context.StockProductos
                        .FirstOrDefaultAsync(x =>
                            x.IdCompraProducto == item.IdCompraArticulo &&
                            x.IdSede == idSedeOrigen &&
                            x.IdNotaSalidaProducto == null);

                    if (stockOrigen != null)
                    {
                        stockOrigen.StockDisponible -= cantidadDespachada;
                    }

                    stockDestino = new StockProducto
                    {
                        IdCompraProducto = item.IdCompraArticulo,
                        StockDisponible = item.CantidadRecibida,
                        UnidadMedida = item.UnidadMedida ?? (stockOrigen != null ? stockOrigen.UnidadMedida : "UND"),
                        IdSede = idSedeDestino,
                        IdNotaSalidaProducto = item.IdNotaSalidaArticulo
                    };

                    _context.StockProductos.Add(stockDestino);
                }
            }
        }

        private async Task ProcesarEconomatos(
            List<ConfirmarArticulosReq> articulos,
            int idSedeOrigen,
            int idSedeDestino)
        {
            foreach (var item in articulos)
            {
                var nsEco = await _context.NotaSalidaEconomatos.FirstOrDefaultAsync(x => x.Id == item.IdNotaSalidaArticulo);
                if (nsEco == null && item.IdCompraArticulo > 0)
                {
                    nsEco = await _context.NotaSalidaEconomatos.FirstOrDefaultAsync(x => x.IdCompraEconomato == item.IdCompraArticulo);
                }

                decimal cantidadDespachada = item.CantidadRecibida;
                if (nsEco != null)
                {
                    cantidadDespachada = nsEco.Cantidad;
                    nsEco.CantidadRecibida = item.CantidadRecibida;
                    nsEco.Observacion = item.Observacion;

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsEco.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                var stockDestino = await _context.StockEconomatos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEconomato == item.IdCompraArticulo &&
                        x.IdSede == idSedeDestino &&
                        x.IdNotaSalidaEconomato == item.IdNotaSalidaArticulo);

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = item.CantidadRecibida;
                    stockDestino.UnidadMedida = item.UnidadMedida ?? stockDestino.UnidadMedida;
                }
                else
                {
                    var stockOrigen = await _context.StockEconomatos
                        .FirstOrDefaultAsync(x =>
                            x.IdCompraEconomato == item.IdCompraArticulo &&
                            x.IdSede == idSedeOrigen &&
                            x.IdNotaSalidaEconomato == null);

                    if (stockOrigen != null)
                    {
                        stockOrigen.StockDisponible -= cantidadDespachada;
                    }

                    stockDestino = new StockEconomato
                    {
                        IdCompraEconomato = item.IdCompraArticulo,
                        StockDisponible = item.CantidadRecibida,
                        UnidadMedida = item.UnidadMedida ?? (stockOrigen != null ? stockOrigen.UnidadMedida : "UND"),
                        IdSede = idSedeDestino,
                        IdNotaSalidaEconomato = item.IdNotaSalidaArticulo
                    };

                    _context.StockEconomatos.Add(stockDestino);
                }
            }
        }

        private async Task ProcesarEmpaques(
            List<ConfirmarArticulosReq> articulos,
            int idSedeOrigen,
            int idSedeDestino)
        {
            foreach (var item in articulos)
            {
                var nsEmp = await _context.NotaSalidaEmpaques.FirstOrDefaultAsync(x => x.Id == item.IdNotaSalidaArticulo);
                if (nsEmp == null && item.IdCompraArticulo > 0)
                {
                    nsEmp = await _context.NotaSalidaEmpaques.FirstOrDefaultAsync(x => x.IdCompraEmpaque == item.IdCompraArticulo);
                }

                decimal cantidadDespachada = item.CantidadRecibida;
                if (nsEmp != null)
                {
                    cantidadDespachada = nsEmp.Cantidad;
                    nsEmp.CantidadRecibida = item.CantidadRecibida;
                    nsEmp.Observacion = item.Observacion;

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsEmp.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                var stockDestino = await _context.StockEmpaques
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEmpaque == item.IdCompraArticulo &&
                        x.IdSede == idSedeDestino &&
                        x.IdNotaSalidaEmpaque == item.IdNotaSalidaArticulo);

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = item.CantidadRecibida;
                    stockDestino.UnidadMedida = item.UnidadMedida ?? stockDestino.UnidadMedida;
                }
                else
                {
                    var stockOrigen = await _context.StockEmpaques
                        .FirstOrDefaultAsync(x =>
                            x.IdCompraEmpaque == item.IdCompraArticulo &&
                            x.IdSede == idSedeOrigen &&
                            x.IdNotaSalidaEmpaque == null);

                    if (stockOrigen != null)
                    {
                        stockOrigen.StockDisponible -= cantidadDespachada;
                    }

                    stockDestino = new StockEmpaque
                    {
                        IdCompraEmpaque = item.IdCompraArticulo,
                        StockDisponible = item.CantidadRecibida,
                        UnidadMedida = item.UnidadMedida ?? (stockOrigen != null ? stockOrigen.UnidadMedida : "UND"),
                        IdSede = idSedeDestino,
                        IdNotaSalidaEmpaque = item.IdNotaSalidaArticulo
                    };

                    _context.StockEmpaques.Add(stockDestino);
                }
            }
        }
    }
}
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

            var grupos = articulos.GroupBy(x => x.IdNotaSalidaArticulo > 0 ? x.IdNotaSalidaArticulo : x.IdCompraArticulo).ToList();

            foreach (var grupo in grupos)
            {
                var primerItem = grupo.First();
                decimal cantRecibidaGramosTotal = 0;
                var observaciones = grupo.Select(g => g.Observacion).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToList();
                string obs = string.Join("; ", observaciones);

                foreach (var item in grupo)
                {
                    decimal cant = item.CantidadRecibida;
                    string umItem = (item.UnidadMedida ?? "").Trim().ToUpper();
                    if (umItem == "KG" || umItem == "KILOGRAMOS" || umItem == "Kg")
                    {
                        cant *= 1000m;
                    }
                    cantRecibidaGramosTotal += cant;
                }

                _logger.LogInformation(
                    "Procesando grupo insumo. IdNotaSalidaArticulo: {IdNotaSalidaArticulo}, IdCompraInsumo: {IdCompraInsumo}, CantidadTotal: {Cantidad}",
                    primerItem.IdNotaSalidaArticulo,
                    primerItem.IdCompraArticulo,
                    cantRecibidaGramosTotal);

                // Actualizar detalle de la nota de salida con cantidad recibida y observacion
                var nsInsumo = await _context.NotaSalidaInsumos.FirstOrDefaultAsync(x => x.Id == primerItem.IdNotaSalidaArticulo);
                if (nsInsumo == null && primerItem.IdCompraArticulo > 0)
                {
                    nsInsumo = await _context.NotaSalidaInsumos.FirstOrDefaultAsync(x => x.IdCompraInsumo == primerItem.IdCompraArticulo);
                }

                bool isPI = nsInsumo != null && !nsInsumo.IdCompraInsumo.HasValue;

                decimal cantidadDespachada = cantRecibidaGramosTotal;
                if (nsInsumo != null)
                {
                    string nsUm = (nsInsumo.Um ?? "").Trim().ToUpper();
                    if (nsUm == "KG" || nsUm == "KILOGRAMOS" || nsUm == "Kg")
                    {
                        nsInsumo.Cantidad *= 1000m;
                        nsInsumo.Um = "G";
                    }

                    cantidadDespachada = nsInsumo.Cantidad;
                    nsInsumo.CantidadRecibida = cantRecibidaGramosTotal;
                    if (!string.IsNullOrWhiteSpace(obs))
                    {
                        nsInsumo.Observacion = obs;
                    }

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsInsumo.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                decimal diferenciaNoRecibida = cantidadDespachada - cantRecibidaGramosTotal;
                if (diferenciaNoRecibida > 0)
                {
                    StockInsumo? stockOrigenReembolso = null;
                    if (isPI)
                    {
                        stockOrigenReembolso = await _context.StockInsumos
                            .Include(x => x.ProductoIntermedio)
                            .FirstOrDefaultAsync(x => x.Tipo == "PI" && x.IdSede == idSedeOrigen &&
                                (x.ProductoIntermedio != null && x.ProductoIntermedio.Lote == nsInsumo!.Lote));
                    }
                    else if (nsInsumo?.IdCompraInsumo.HasValue == true)
                    {
                        stockOrigenReembolso = await _context.StockInsumos
                            .FirstOrDefaultAsync(x => x.IdCompraInsumo == nsInsumo.IdCompraInsumo.Value && x.IdSede == idSedeOrigen);
                    }
                    else if (primerItem.IdCompraArticulo > 0)
                    {
                        stockOrigenReembolso = await _context.StockInsumos
                            .FirstOrDefaultAsync(x => x.IdCompraInsumo == primerItem.IdCompraArticulo && x.IdSede == idSedeOrigen);
                    }

                    if (stockOrigenReembolso != null)
                    {
                        stockOrigenReembolso.StockDisponible += diferenciaNoRecibida;
                    }
                }

                // Verificar si ya existe stock destino para esta nota/articulo (modificacion)
                StockInsumo? stockDestino = null;
                if (primerItem.IdNotaSalidaArticulo > 0)
                {
                    stockDestino = await _context.StockInsumos
                        .FirstOrDefaultAsync(x =>
                            x.IdSede == idSedeDestino &&
                            x.IdNotaSalidaInsumo == primerItem.IdNotaSalidaArticulo);
                }

                if (stockDestino == null && !isPI && primerItem.IdCompraArticulo > 0)
                {
                    stockDestino = await _context.StockInsumos
                        .FirstOrDefaultAsync(x =>
                            x.IdCompraInsumo == primerItem.IdCompraArticulo &&
                            x.IdSede == idSedeDestino &&
                            x.IdNotaSalidaInsumo == primerItem.IdNotaSalidaArticulo);
                }

                decimal cantDestino = cantRecibidaGramosTotal;

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = cantDestino;
                    stockDestino.UnidadMedida = "G";
                }
                else
                {
                    int? fkCompra = null;
                    if (!isPI)
                    {
                        if (nsInsumo?.IdCompraInsumo.HasValue == true)
                        {
                            fkCompra = nsInsumo.IdCompraInsumo.Value;
                        }
                        else if (primerItem.IdCompraArticulo > 0 && await _context.CompraInsumos.AnyAsync(c => c.Id == primerItem.IdCompraArticulo))
                        {
                            fkCompra = primerItem.IdCompraArticulo;
                        }
                    }

                    stockDestino = new StockInsumo
                    {
                        IdCompraInsumo = fkCompra,
                        IdProductoIntermedio = null, // En destino se mantiene null para evitar violar el indice unico
                        Tipo = isPI ? "PI" : "MP",
                        StockDisponible = cantDestino,
                        UnidadMedida = "G",
                        IdSede = idSedeDestino,
                        IdNotaSalidaInsumo = primerItem.IdNotaSalidaArticulo > 0 ? primerItem.IdNotaSalidaArticulo : (nsInsumo?.Id)
                    };

                    _context.StockInsumos.Add(stockDestino);
                }

                _logger.LogInformation(
                    "Insumo procesado correctamente. IdNotaSalidaArticulo: {IdNotaSalidaArticulo}, isPI: {IsPI}, " +
                    "StockDestino: {CantidadRecibida}, SedeDestino: {SedeDestino}",
                    primerItem.IdNotaSalidaArticulo,
                    isPI,
                    cantRecibidaGramosTotal,
                    idSedeDestino);
            }
        }

        private async Task ProcesarProductos(
            List<ConfirmarArticulosReq> articulos,
            int idSedeOrigen,
            int idSedeDestino)
        {
            var grupos = articulos.GroupBy(x => x.IdNotaSalidaArticulo > 0 ? x.IdNotaSalidaArticulo : x.IdCompraArticulo).ToList();

            foreach (var grupo in grupos)
            {
                var primerItem = grupo.First();
                decimal totalRecibido = grupo.Sum(g => g.CantidadRecibida);
                var observaciones = grupo.Select(g => g.Observacion).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToList();
                string obs = string.Join("; ", observaciones);

                var nsProd = await _context.NotaSalidaProductos.FirstOrDefaultAsync(x => x.Id == primerItem.IdNotaSalidaArticulo);
                if (nsProd == null && primerItem.IdCompraArticulo > 0)
                {
                    nsProd = await _context.NotaSalidaProductos.FirstOrDefaultAsync(x => x.IdCompraProducto == primerItem.IdCompraArticulo);
                }

                // Redireccion segura si el item era realmente un insumo o producto intermedio
                if (nsProd == null)
                {
                    var esInsumoOPI = await _context.NotaSalidaInsumos.AnyAsync(x => x.Id == primerItem.IdNotaSalidaArticulo);
                    if (esInsumoOPI)
                    {
                        _logger.LogInformation("Item {Id} recibido en productos pero pertenece a NotaSalidaInsumos. Redirigiendo a ProcesarInsumos.", primerItem.IdNotaSalidaArticulo);
                        await ProcesarInsumos(grupo.ToList(), idSedeOrigen, idSedeDestino);
                        continue;
                    }
                }

                decimal cantidadDespachada = totalRecibido;
                if (nsProd != null)
                {
                    cantidadDespachada = nsProd.Cantidad;
                    nsProd.CantidadRecibida = totalRecibido;
                    if (!string.IsNullOrWhiteSpace(obs)) nsProd.Observacion = obs;

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsProd.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                decimal diferenciaNoRecibida = cantidadDespachada - totalRecibido;
                if (diferenciaNoRecibida > 0)
                {
                    var stockOrigenReembolso = await _context.StockProductos
                        .FirstOrDefaultAsync(x => x.IdCompraProducto == primerItem.IdCompraArticulo && x.IdSede == idSedeOrigen);
                    if (stockOrigenReembolso != null)
                    {
                        stockOrigenReembolso.StockDisponible += diferenciaNoRecibida;
                    }
                }

                StockProducto? stockDestino = null;
                if (primerItem.IdNotaSalidaArticulo > 0)
                {
                    stockDestino = await _context.StockProductos
                        .FirstOrDefaultAsync(x =>
                            x.IdSede == idSedeDestino &&
                            x.IdNotaSalidaProducto == primerItem.IdNotaSalidaArticulo);
                }

                if (stockDestino == null && primerItem.IdCompraArticulo > 0)
                {
                    stockDestino = await _context.StockProductos
                        .FirstOrDefaultAsync(x =>
                            x.IdCompraProducto == primerItem.IdCompraArticulo &&
                            x.IdSede == idSedeDestino &&
                            x.IdNotaSalidaProducto == primerItem.IdNotaSalidaArticulo);
                }

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = totalRecibido;
                    stockDestino.UnidadMedida = primerItem.UnidadMedida ?? stockDestino.UnidadMedida;
                }
                else
                {
                    int fkCompraProd = (nsProd?.IdCompraProducto.HasValue == true)
                        ? nsProd.IdCompraProducto.Value
                        : primerItem.IdCompraArticulo;

                    stockDestino = new StockProducto
                    {
                        IdCompraProducto = fkCompraProd,
                        StockDisponible = totalRecibido,
                        UnidadMedida = primerItem.UnidadMedida ?? "UND",
                        IdSede = idSedeDestino,
                        IdNotaSalidaProducto = primerItem.IdNotaSalidaArticulo > 0 ? primerItem.IdNotaSalidaArticulo : (nsProd?.Id)
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
            var grupos = articulos.GroupBy(x => x.IdNotaSalidaArticulo > 0 ? x.IdNotaSalidaArticulo : x.IdCompraArticulo).ToList();

            foreach (var grupo in grupos)
            {
                var primerItem = grupo.First();
                decimal totalRecibido = grupo.Sum(g => g.CantidadRecibida);
                var observaciones = grupo.Select(g => g.Observacion).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToList();
                string obs = string.Join("; ", observaciones);

                var nsEco = await _context.NotaSalidaEconomatos.FirstOrDefaultAsync(x => x.Id == primerItem.IdNotaSalidaArticulo);
                if (nsEco == null && primerItem.IdCompraArticulo > 0)
                {
                    nsEco = await _context.NotaSalidaEconomatos.FirstOrDefaultAsync(x => x.IdCompraEconomato == primerItem.IdCompraArticulo);
                }

                decimal cantidadDespachada = totalRecibido;
                if (nsEco != null)
                {
                    cantidadDespachada = nsEco.Cantidad;
                    nsEco.CantidadRecibida = totalRecibido;
                    if (!string.IsNullOrWhiteSpace(obs)) nsEco.Observacion = obs;

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsEco.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                decimal diferenciaNoRecibida = cantidadDespachada - totalRecibido;
                if (diferenciaNoRecibida > 0)
                {
                    var stockOrigenReembolso = await _context.StockEconomatos
                        .FirstOrDefaultAsync(x => x.IdCompraEconomato == primerItem.IdCompraArticulo && x.IdSede == idSedeOrigen);
                    if (stockOrigenReembolso != null)
                    {
                        stockOrigenReembolso.StockDisponible += diferenciaNoRecibida;
                    }
                }

                var stockDestino = await _context.StockEconomatos
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEconomato == primerItem.IdCompraArticulo &&
                        x.IdSede == idSedeDestino &&
                        x.IdNotaSalidaEconomato == primerItem.IdNotaSalidaArticulo);

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = totalRecibido;
                    stockDestino.UnidadMedida = primerItem.UnidadMedida ?? stockDestino.UnidadMedida;
                }
                else
                {
                    stockDestino = new StockEconomato
                    {
                        IdCompraEconomato = primerItem.IdCompraArticulo,
                        StockDisponible = totalRecibido,
                        UnidadMedida = primerItem.UnidadMedida ?? "UND",
                        IdSede = idSedeDestino,
                        IdNotaSalidaEconomato = primerItem.IdNotaSalidaArticulo
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
            var grupos = articulos.GroupBy(x => x.IdNotaSalidaArticulo > 0 ? x.IdNotaSalidaArticulo : x.IdCompraArticulo).ToList();

            foreach (var grupo in grupos)
            {
                var primerItem = grupo.First();
                decimal totalRecibido = grupo.Sum(g => g.CantidadRecibida);
                var observaciones = grupo.Select(g => g.Observacion).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToList();
                string obs = string.Join("; ", observaciones);

                var nsEmp = await _context.NotaSalidaEmpaques.FirstOrDefaultAsync(x => x.Id == primerItem.IdNotaSalidaArticulo);
                if (nsEmp == null && primerItem.IdCompraArticulo > 0)
                {
                    nsEmp = await _context.NotaSalidaEmpaques.FirstOrDefaultAsync(x => x.IdCompraEmpaque == primerItem.IdCompraArticulo);
                }

                decimal cantidadDespachada = totalRecibido;
                if (nsEmp != null)
                {
                    cantidadDespachada = nsEmp.Cantidad;
                    nsEmp.CantidadRecibida = totalRecibido;
                    if (!string.IsNullOrWhiteSpace(obs)) nsEmp.Observacion = obs;

                    var parentNS = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == nsEmp.IdNotaSalida);
                    if (parentNS != null)
                    {
                        parentNS.Estado = "RECIBIDO";
                    }
                }

                decimal diferenciaNoRecibida = cantidadDespachada - totalRecibido;
                if (diferenciaNoRecibida > 0)
                {
                    var stockOrigenReembolso = await _context.StockEmpaques
                        .FirstOrDefaultAsync(x => x.IdCompraEmpaque == primerItem.IdCompraArticulo && x.IdSede == idSedeOrigen);
                    if (stockOrigenReembolso != null)
                    {
                        stockOrigenReembolso.StockDisponible += diferenciaNoRecibida;
                    }
                }

                var stockDestino = await _context.StockEmpaques
                    .FirstOrDefaultAsync(x =>
                        x.IdCompraEmpaque == primerItem.IdCompraArticulo &&
                        x.IdSede == idSedeDestino &&
                        x.IdNotaSalidaEmpaque == primerItem.IdNotaSalidaArticulo);

                if (stockDestino != null)
                {
                    stockDestino.StockDisponible = totalRecibido;
                    stockDestino.UnidadMedida = primerItem.UnidadMedida ?? stockDestino.UnidadMedida;
                }
                else
                {
                    stockDestino = new StockEmpaque
                    {
                        IdCompraEmpaque = primerItem.IdCompraArticulo,
                        StockDisponible = totalRecibido,
                        UnidadMedida = primerItem.UnidadMedida ?? "UND",
                        IdSede = idSedeDestino,
                        IdNotaSalidaEmpaque = primerItem.IdNotaSalidaArticulo
                    };

                    _context.StockEmpaques.Add(stockDestino);
                }
            }
        }
    }
}
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;

public class NotaSalidaService : INotaSalidaService
{
    private readonly ApiContext _context;
    private readonly ILogger<NotaSalidaService> _logger;
    public NotaSalidaService(ApiContext context, ILogger<NotaSalidaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> CrearAsync(NotaSalidaCreateReq request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var notaSalida = new NotaSalida
            {
                FechaSalida = request.FechaSalida,
                IdSedeOrigen = request.IdSedeOrigen,
                IdSedeDestino = request.IdSedeDestino,
                Observacion = request.Observacion,
                IdCreador = request.IdCreador
            };

            _context.NotaSalidas.Add(notaSalida);
            await _context.SaveChangesAsync();

            foreach (var item in request.ListaFamilias)
            {
                switch (item.Familia.ToUpper())
                {
                    case "MP":
                        await CrearDetalleInsumo(notaSalida.Id, request, item);
                        break;

                    case "ME":
                        await CrearDetalleEmpaque(notaSalida.Id, request, item);
                        break;

                    case "ECO":
                        await CrearDetalleEconomato(notaSalida.Id, request, item);
                        break;

                    case "PT":
                        await CrearDetalleProducto(notaSalida.Id, request, item);
                        break;

                    default:
                        throw new Exception($"Familia '{item.Familia}' no válida.");
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return notaSalida.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<List<NotaSalidaListaRes>> ObtenerListaAsync(int idSede)
    {
        return await _context.NotaSalidas
            .AsNoTracking()
            .Include(n => n.SedeDestino)
            .Include(n => n.SedeOrigen)
            .Include(n => n.Creador)
            .OrderByDescending(n => n.FechaCreacion)
            .Where(w => w.IdSedeOrigen == idSede)
            .Select(n => new NotaSalidaListaRes
            {
                IdNotaSalida = n.Id,
                Codigo = UtilFamilia.CodigoNotaSalida(n.Id),
                FechaCreacion = n.FechaCreacion,
                Destino = n.SedeDestino != null ? n.SedeDestino.Nombre ?? string.Empty : string.Empty,
                Responsable = n.Creador != null ? n.Creador!.Persona!.NombreCompleto! : "",
                Observacion = n.Observacion
            })
            .ToListAsync();
    }
    private async Task CrearDetalleInsumo(
     int idNotaSalida,
     NotaSalidaCreateReq request,
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

        // Descontar stock
        stockOrigen.StockDisponible -= item.Cantidad;

        _logger.LogInformation(
            "Stock actualizado. NuevoStock={NuevoStock}",
            stockOrigen.StockDisponible);

        var detalle = new NotaSalidaInsumo
        {
            IdNotaSalida = idNotaSalida,
            IdCompraInsumo = item.Registro,
            Cantidad = item.Cantidad,
            Um = item.Um,
            Paquete = item.Paquete,
            IdCreador = request.IdCreador,
            CantidadPaquete = item.CantidadPaquete
        };

        _context.NotaSalidaInsumos.Add(detalle);

        _logger.LogInformation(
            "Detalle de NotaSalidaInsumo agregado. Registro={Registro}",
            item.Registro);

        var nuevoStockDestino = new StockInsumo
        {
            IdCompraInsumo = item.Registro,
            StockDisponible = item.Cantidad,
            UnidadMedida = stockOrigen.UnidadMedida,
            IdSede = request.IdSedeDestino,
            NotaSalidaInsumo = detalle
        };

        _context.StockInsumos.Add(nuevoStockDestino);

        _logger.LogInformation(
            "Stock creado para sede destino. Registro={Registro}, SedeDestino={SedeDestino}, Stock={Stock}",
            item.Registro,
            request.IdSedeDestino,
            item.Cantidad);

        _logger.LogInformation(
            "Finalizó creación de detalle de insumo. NotaSalida={NotaSalida}",
            idNotaSalida);
    }
    public async Task ActualizarAsync(int id, NotaSalidaCreateReq request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var notaSalida = await _context.NotaSalidas
                .Include(x => x.NotaSalidaInsumos)
                .Include(x => x.NotaSalidaEmpaques)
                .Include(x => x.NotaSalidaEconomatos)
                .Include(x => x.NotaSalidaProductos)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (notaSalida == null)
                throw new Exception("Nota salida no encontrada.");

            // 1. Revertir stock anterior
            await RevertirStockInsumo(notaSalida.Id);
            await RevertirStockEmpaque(notaSalida.Id);
            await RevertirStockEconomato(notaSalida.Id);
            await RevertirStockProducto(notaSalida.Id);


            // 2. Eliminar detalles anteriores
            _context.NotaSalidaInsumos.RemoveRange(notaSalida.NotaSalidaInsumos);
            _context.NotaSalidaEmpaques.RemoveRange(notaSalida.NotaSalidaEmpaques);
            _context.NotaSalidaEconomatos.RemoveRange(notaSalida.NotaSalidaEconomatos);
            _context.NotaSalidaProductos.RemoveRange(notaSalida.NotaSalidaProductos);


            // 3. Actualizar cabecera
            notaSalida.FechaSalida = request.FechaSalida;
            notaSalida.IdSedeOrigen = request.IdSedeOrigen;
            notaSalida.IdSedeDestino = request.IdSedeDestino;
            notaSalida.Observacion = request.Observacion;


            await _context.SaveChangesAsync();


            // 4. Crear nuevamente detalles y stock
            foreach (var item in request.ListaFamilias)
            {
                switch (item.Familia.ToUpper())
                {
                    case "MP":
                        await CrearDetalleInsumo(notaSalida.Id, request, item);
                        break;

                    case "ME":
                        await CrearDetalleEmpaque(notaSalida.Id, request, item);
                        break;

                    case "ECO":
                        await CrearDetalleEconomato(notaSalida.Id, request, item);
                        break;

                    case "PT":
                        await CrearDetalleProducto(notaSalida.Id, request, item);
                        break;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
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
    private async Task CrearDetalleEconomato(
        int idNotaSalida,
        NotaSalidaCreateReq request,
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

        // Descontar stock de origen
        stockOrigen.StockDisponible -= item.Cantidad;

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

        _context.StockEconomatos.Add(new StockEconomato
        {
            IdCompraEconomato = stockOrigen.IdCompraEconomato,
            StockDisponible = item.Cantidad,
            UnidadMedida = stockOrigen.UnidadMedida,
            IdSede = request.IdSedeDestino,
            NotaSalidaEconomato = detalle // EF asignará el Id automáticamente
        });
    }

    private async Task CrearDetalleEmpaque(
        int idNotaSalida,
        NotaSalidaCreateReq request,
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

        // Descontar stock de origen
        stockOrigen.StockDisponible -= item.Cantidad;

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

        _context.StockEmpaques.Add(new StockEmpaque
        {
            IdCompraEmpaque = stockOrigen.IdCompraEmpaque,
            StockDisponible = item.Cantidad,
            UnidadMedida = stockOrigen.UnidadMedida,
            IdSede = request.IdSedeDestino,
            NotaSalidaEmpaque = detalle // EF asignará el Id automáticamente
        });
    }
    private async Task CrearDetalleProducto(
        int idNotaSalida,
        NotaSalidaCreateReq request,
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

        // Descontar stock de origen
        stockOrigen.StockDisponible -= item.Cantidad;

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

        _context.StockProductos.Add(new StockProductoTerminado
        {
            IdCompraProducto = stockOrigen.IdCompraProducto,
            StockDisponible = item.Cantidad,
            UnidadMedida = stockOrigen.UnidadMedida,
            IdSede = request.IdSedeDestino,
            NotaSalidaProducto = detalle // EF asignará el Id automáticamente
        });
    }

    public async Task EliminarAsync(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var notaSalida = await _context.NotaSalidas
                .Include(x => x.NotaSalidaInsumos)
                .Include(x => x.NotaSalidaEmpaques)
                .Include(x => x.NotaSalidaEconomatos)
                .Include(x => x.NotaSalidaProductos)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (notaSalida == null)
                throw new Exception("Nota salida no encontrada.");


            // Revertir movimientos de stock
            await EliminarStockInsumo(notaSalida.NotaSalidaInsumos);
            await EliminarStockEmpaque(notaSalida.NotaSalidaEmpaques);
            await EliminarStockEconomato(notaSalida.NotaSalidaEconomatos);
            await EliminarStockProducto(notaSalida.NotaSalidaProductos);



            // Eliminar detalles
            _context.NotaSalidaInsumos.RemoveRange(notaSalida.NotaSalidaInsumos);
            _context.NotaSalidaEmpaques.RemoveRange(notaSalida.NotaSalidaEmpaques);
            _context.NotaSalidaEconomatos.RemoveRange(notaSalida.NotaSalidaEconomatos);
            _context.NotaSalidaProductos.RemoveRange(notaSalida.NotaSalidaProductos);


            // Eliminar cabecera
            _context.NotaSalidas.Remove(notaSalida);


            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    private async Task EliminarStockInsumo(
    List<NotaSalidaInsumo> detalles)
    {
        var idsDetalle = detalles
            .Select(x => x.Id)
            .ToList();


        var stocksDestino = await _context.StockInsumos
            .Where(x => x.IdNotaSalidaInsumo != null &&
                        idsDetalle.Contains(x.IdNotaSalidaInsumo.Value))
            .ToListAsync();


        foreach (var stockDestino in stocksDestino)
        {
            // buscar stock origen
            var stockOrigen = await _context.StockInsumos
                .FirstOrDefaultAsync(x =>
                    x.IdCompraInsumo == stockDestino.IdCompraInsumo &&
                    x.IdNotaSalidaInsumo == null &&
                    x.IdSede != stockDestino.IdSede);


            if (stockOrigen != null)
            {
                stockOrigen.StockDisponible += stockDestino.StockDisponible;
            }


            _context.StockInsumos.Remove(stockDestino);
        }
    }
    private async Task EliminarStockEmpaque(
    List<NotaSalidaEmpaque> detalles)
    {
        var idsDetalle = detalles
            .Select(x => x.Id)
            .ToList();


        var stocksDestino = await _context.StockEmpaques
            .Where(x => x.IdNotaSalidaEmpaque != null &&
                        idsDetalle.Contains(x.IdNotaSalidaEmpaque.Value))
            .ToListAsync();


        foreach (var stockDestino in stocksDestino)
        {
            // buscar stock origen
            var stockOrigen = await _context.StockEmpaques
                .FirstOrDefaultAsync(x =>
                    x.IdCompraEmpaque == stockDestino.IdCompraEmpaque &&
                    x.IdNotaSalidaEmpaque == null &&
                    x.IdSede != stockDestino.IdSede);


            if (stockOrigen != null)
            {
                stockOrigen.StockDisponible += stockDestino.StockDisponible;
            }


            _context.StockEmpaques.Remove(stockDestino);
        }
    }
    private async Task EliminarStockEconomato(
    List<NotaSalidaEconomato> detalles)
    {
        var idsDetalle = detalles
            .Select(x => x.Id)
            .ToList();


        var stocksDestino = await _context.StockEconomatos
            .Where(x => x.IdNotaSalidaEconomato != null &&
                        idsDetalle.Contains(x.IdNotaSalidaEconomato.Value))
            .ToListAsync();


        foreach (var stockDestino in stocksDestino)
        {
            // buscar stock origen
            var stockOrigen = await _context.StockEconomatos
                .FirstOrDefaultAsync(x =>
                    x.IdCompraEconomato == stockDestino.IdCompraEconomato &&
                    x.IdNotaSalidaEconomato == null &&
                    x.IdSede != stockDestino.IdSede);


            if (stockOrigen != null)
            {
                stockOrigen.StockDisponible += stockDestino.StockDisponible;
            }


            _context.StockEconomatos.Remove(stockDestino);
        }
    }
    private async Task EliminarStockProducto(
    List<NotaSalidaProducto> detalles)
    {
        var idsDetalle = detalles
            .Select(x => x.Id)
            .ToList();


        var stocksDestino = await _context.StockProductos
            .Where(x => x.IdNotaSalidaProducto != null &&
                        idsDetalle.Contains(x.IdNotaSalidaProducto.Value))
            .ToListAsync();


        foreach (var stockDestino in stocksDestino)
        {
            // buscar stock origen
            var stockOrigen = await _context.StockProductos
                .FirstOrDefaultAsync(x =>
                    x.IdCompraProducto == stockDestino.IdCompraProducto &&
                    x.IdNotaSalidaProducto == null &&
                    x.IdSede != stockDestino.IdSede);


            if (stockOrigen != null)
            {
                stockOrigen.StockDisponible += stockDestino.StockDisponible;
            }


            _context.StockProductos.Remove(stockDestino);
        }
    }

    public async Task<List<RegistrosListaRes>> ObtenerRegistros(int idArticulo, string familia)
    {
        familia = familia?.Trim().ToUpper();

        return familia switch
        {
            "MP" => await _context.CompraInsumos
                .Where(x => x.IdInsumo == idArticulo )
                .Select(x => new RegistrosListaRes
                {
                    Registro = x.Id,
                    Codigo = UtilFamilia.CodigoInsumo(x.Id)
                })
                .ToListAsync(),

            "ME" => await _context.CompraEmpaques
                .Where(x => x.IdEmpaque == idArticulo)
                .Select(x => new RegistrosListaRes
                {
                    Registro = x.Id,
                    Codigo = UtilFamilia.CodigoEmpaque(x.Id)
                })
                .ToListAsync(),

            "ECO" => await _context.CompraEconomatos
                .Where(x => x.IdEconomato == idArticulo)
                .Select(x => new RegistrosListaRes
                {
                    Registro = x.Id,
                    Codigo = UtilFamilia.CodigoEconomato(x.Id)
                })
                .ToListAsync(),

            "PT" => await _context.CompraProductos
                .Where(x => x.IdProducto == idArticulo)
                .Select(x => new RegistrosListaRes
                {
                    Registro = x.Id,
                    Codigo = UtilFamilia.CodigoProducto(x.Id)
                })
                .ToListAsync(),

            _ => new List<RegistrosListaRes>()
        };
    }
}
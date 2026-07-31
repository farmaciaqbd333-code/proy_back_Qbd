using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;

public class NotaSalidaService : INotaSalidaService
{
    private readonly ApiContext _context;

    public NotaSalidaService(ApiContext context)
    {
        _context = context;
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
        var stockOrigen = await _context.StockInsumos
            .FirstOrDefaultAsync(x =>
                x.IdCompraInsumo == item.Registro &&
                x.IdSede == request.IdSedeOrigen);

        if (stockOrigen == null)
            throw new Exception("No existe stock en la sede proveniente.");

        if (stockOrigen.StockDisponible < item.Cantidad)
            throw new Exception("Stock insuficiente.");

        // Descontar stock de origen
        stockOrigen.StockDisponible -= item.Cantidad;

        var detalle = new NotaSalidaInsumo
        {
            IdNotaSalida = idNotaSalida,
            IdCompraInsumo = item.Registro,
            Cantidad = item.Cantidad,
            Um = item.Um,
            Paquete = item.Paquete,
            CantidadPaquete = item.CantidadPaquete
        };

        _context.NotaSalidaInsumos.Add(detalle);

        _context.StockInsumos.Add(new StockInsumo
        {
            IdCompraInsumo = stockOrigen.IdCompraInsumo,
            StockDisponible = item.Cantidad,
            UnidadMedida = stockOrigen.UnidadMedida,
            IdSede = request.IdSedeDestino,
            NotaSalidaInsumo = detalle // EF asignará el Id automáticamente
        });
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
        var stockOrigen = await _context.StockProductoTerminados
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
            CantidadPaquete = item.CantidadPaquete
        };

        _context.NotaSalidaProductos.Add(detalle);

        _context.StockProductoTerminados.Add(new StockProductoTerminado
        {
            IdCompraProducto = stockOrigen.IdCompraProducto,
            StockDisponible = item.Cantidad,
            UnidadMedida = stockOrigen.UnidadMedida,
            IdSede = request.IdSedeDestino,
            NotaSalidaProducto = detalle // EF asignará el Id automáticamente
        });
    }
}
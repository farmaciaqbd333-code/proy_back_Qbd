using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

public class NotaSalidaService
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
                IdSede = request.IdSedeDestino,
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
                        await CrearDetalleInsumo(notaSalida.Id, request.IdSedeDestino, item);
                        break;

                    case "ME":
                        await CrearDetalleEmpaque(notaSalida.Id, request.IdSedeDestino, item);
                        break;

                    case "ECO":
                        await CrearDetalleEconomato(notaSalida.Id, request.IdSedeDestino, item);
                        break;

                    case "PT":
                        await CrearDetalleProducto(notaSalida.Id, request.IdSedeDestino, item);
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

    private async Task CrearDetalleInsumo(int idNotaSalida, int idSede, NotaSalidaFamiliasCreateReq item)
    {
        _context.NotaSalidaInsumos.Add(new NotaSalidaInsumo
        {
            IdNotaSalida = idNotaSalida,
            IdCompraInsumo = item.Registro,
            Cantidad = item.Cantidad,
            Um = item.Um,
            Paquete = item.Paquete,
            CantidadPaquete = item.CantidadPaquete
        });

        var stock = await _context.StockInsumos
            .FirstOrDefaultAsync(x => x.IdCompraInsumo == item.Registro && x.IdSede == idSede);

        if (stock == null)
            throw new Exception("No existe stock de insumo.");

        if (stock.StockDisponible < item.Cantidad)
            throw new Exception("Stock insuficiente.");

        stock.StockDisponible -= item.Cantidad;
    }

    private async Task CrearDetalleEmpaque(int idNotaSalida, int idSede, NotaSalidaFamiliasCreateReq item)
    {
        _context.NotaSalidaEmpaques.Add(new NotaSalidaEmpaque
        {
            IdNotaSalida = idNotaSalida,
            IdCompraEmpaque = item.Registro,
            Cantidad = item.Cantidad,
            Um = item.Um,
            Paquete = item.Paquete,
            CantidadPaquete = item.CantidadPaquete
        });

        var stock = await _context.StockEmpaques
            .FirstOrDefaultAsync(x => x.IdCompraEmpaque == item.Registro && x.IdSede == idSede);

        if (stock == null)
            throw new Exception("No existe stock de empaque.");

        if (stock.StockDisponible < item.Cantidad)
            throw new Exception("Stock insuficiente.");

        stock.StockDisponible -= item.Cantidad;
    }

    private async Task CrearDetalleEconomato(int idNotaSalida, int idSede, NotaSalidaFamiliasCreateReq item)
    {
        _context.NotaSalidaEconomatos.Add(new NotaSalidaEconomato
        {
            IdNotaSalida = idNotaSalida,
            IdCompraEconomato = item.Registro,
            Cantidad = item.Cantidad,
            Um = item.Um,
            Paquete = item.Paquete,
            CantidadPaquete = item.CantidadPaquete
        });

        var stock = await _context.StockEconomatos
            .FirstOrDefaultAsync(x => x.IdCompraEconomato == item.Registro && x.IdSede == idSede);

        if (stock == null)
            throw new Exception("No existe stock de economato.");

        if (stock.StockDisponible < item.Cantidad)
            throw new Exception("Stock insuficiente.");

        stock.StockDisponible -= item.Cantidad;
    }

    private async Task CrearDetalleProducto(int idNotaSalida, int idSede, NotaSalidaFamiliasCreateReq item)
    {
        _context.NotaSalidaProductos.Add(new NotaSalidaProducto
        {
            IdNotaSalida = idNotaSalida,
            IdCompraProducto = item.Registro,
            Cantidad = item.Cantidad,
            Um = item.Um,
            Paquete = item.Paquete,
            CantidadPaquete = item.CantidadPaquete
        });

        var stock = await _context.StockProductoTerminados
            .FirstOrDefaultAsync(x => x.IdCompraProducto == item.Registro && x.IdSede == idSede);

        if (stock == null)
            throw new Exception("No existe stock del producto.");

        if (stock.StockDisponible < item.Cantidad)
            throw new Exception("Stock insuficiente.");

        stock.StockDisponible -= item.Cantidad;
    }
}
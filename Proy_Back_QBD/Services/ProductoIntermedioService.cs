using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.ProductoIntermedio;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto;
using Proy_back_QBD.Interface;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace proy_back_Qbd.Services
{
    public class ProductoIntermedioService : IProductoIntermedioService
    {
        private readonly ApiContext _context;
        private readonly ILogger<ProductoIntermedioService> _logger;
        public ProductoIntermedioService(ApiContext context, ILogger<ProductoIntermedioService> _logger)
        {
            _context = context;
            this._logger = _logger;
        }

        public async Task<int> CrearProductoIntermedio(CrearProductoIntermedioReq request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var ahora = DateTime.UtcNow;

            try
            {
                if (request.TipoUso is not ("PI-FMG" or "PI-F%"))
                {
                    throw new BadRequestException(
                        $"Tipo de uso no válido: {request.TipoUso}. Los valores permitidos son PI-FMG y PI-F%.");
                }

                ProductoIntermedio productoIntermedio =
                    new ProductoIntermedioMapper().CrearProductoIntermedio(request);

                if (productoIntermedio.FechaEmision.Value.Kind == DateTimeKind.Unspecified)
                {
                    productoIntermedio.FechaEmision =
                        DateTime.SpecifyKind(productoIntermedio.FechaEmision.Value, DateTimeKind.Utc);
                }

                if (productoIntermedio.FechaVencimiento.Value.Kind == DateTimeKind.Unspecified)
                {
                    productoIntermedio.FechaVencimiento =
                        DateTime.SpecifyKind(productoIntermedio.FechaVencimiento.Value, DateTimeKind.Utc);
                }

                _context.ProductosIntermedios.Add(productoIntermedio);

                // ============================================================
                // EMPAQUES
                // ============================================================

                var listaEmpaques = request.IdEmpaques?.ToList() ?? new List<int>();

                if (listaEmpaques.Count > 0)
                {
                    var empaquesRelacionados = new List<int>();

                    foreach (var idEmpaque in listaEmpaques)
                    {
                        var empaque = await _context.Empaques
                            .Where(x => x.Id == idEmpaque)
                            .Select(x => new
                            {
                                x.IdCaja,
                                x.IdFunda,
                                x.IdEtiqueta1,
                                x.IdEtiqueta2
                            })
                            .FirstOrDefaultAsync();

                        if (empaque == null)
                        {
                            throw new NotFoundException(
                                $"No existe este Empaque con id {idEmpaque}");
                        }

                        empaquesRelacionados.Add(idEmpaque);

                        if (empaque.IdCaja.HasValue)
                            empaquesRelacionados.Add(empaque.IdCaja.Value);

                        if (empaque.IdFunda.HasValue)
                            empaquesRelacionados.Add(empaque.IdFunda.Value);

                        if (empaque.IdEtiqueta1.HasValue)
                            empaquesRelacionados.Add(empaque.IdEtiqueta1.Value);

                        if (empaque.IdEtiqueta2.HasValue)
                            empaquesRelacionados.Add(empaque.IdEtiqueta2.Value);
                    }

                    var conteoEmpaques = empaquesRelacionados
                        .GroupBy(x => x)
                        .ToDictionary(
                            g => g.Key,
                            g => (decimal)g.Count());

                    foreach (var conteoEmpaque in conteoEmpaques)
                    {
                        decimal cantidadPendiente = conteoEmpaque.Value;

                        var stockEmpaques = await _context.StockEmpaques
                            .Where(x =>
                                x.CompraEmpaque.IdEmpaque == conteoEmpaque.Key &&
                                x.IdSede == request.IdSede &&
                                x.StockDisponible > 0 &&
                                x.CompraEmpaque.FechaVencimiento >= ahora)
                            .OrderBy(x => x.CompraEmpaque.FechaVencimiento)
                            .ToListAsync();

                        if (stockEmpaques.Count == 0)
                        {
                            throw new NotFoundException(
                                $"No hay stock disponible para el empaque {conteoEmpaque.Key}");
                        }

                        decimal stockDisponibleTotal =
                            stockEmpaques.Sum(x => x.StockDisponible);

                        if (stockDisponibleTotal < cantidadPendiente)
                        {
                            throw new BadRequestException(
                                $"Stock insuficiente para el empaque {conteoEmpaque.Key}. " +
                                $"Disponible: {stockDisponibleTotal}, requerido: {cantidadPendiente}");
                        }

                        var empaqueProductoIntermedio = new EmpaqueProductoIntermedio
                        {
                            IdEmpaque = conteoEmpaque.Key,
                            ProductoIntermedio = productoIntermedio
                        };

                        _context.EmpaqueProductoIntermedios.Add(
                            empaqueProductoIntermedio);

                        foreach (var stockEmpaque in stockEmpaques)
                        {
                            if (cantidadPendiente <= 0)
                                break;

                            decimal cantidadConsumida =
                                Math.Min(stockEmpaque.StockDisponible, cantidadPendiente);

                            _context.StockEmpaqueProductoIntermedios.Add(
                                new StockEmpaqueProductoIntermedio
                                {
                                    Cantidad = cantidadConsumida,
                                    IdStockEmpaque = stockEmpaque.Id,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio =
                                        empaqueProductoIntermedio
                                });

                            stockEmpaque.StockDisponible -= cantidadConsumida;
                            cantidadPendiente -= cantidadConsumida;
                        }

                        if (cantidadPendiente > 0)
                        {
                            throw new BadRequestException(
                                $"No se pudo completar el consumo del empaque {conteoEmpaque.Key}");
                        }
                    }
                }

                // ============================================================
                // INSUMOS
                // ============================================================

                foreach (var fInsumo in request.Insumos)
                {
                    if (fInsumo.Tipo is not ("MP" or "PI"))
                    {
                        throw new BadRequestException(
                            $"Tipo de insumo no válido: {fInsumo.Tipo}");
                    }

                    decimal cantidadUsar = fInsumo.CantidadLote;

                    List<StockInsumo> stockInsumos;

                    if (fInsumo.Tipo == "MP")
                    {
                        stockInsumos = await _context.StockInsumos
                            .Where(x =>
                                x.CompraInsumo.IdInsumo == fInsumo.IdInsumo &&
                                x.IdSede == request.IdSede &&
                                x.StockDisponible > 0 &&
                                x.CompraInsumo.FechaVencimiento >= ahora)
                            .OrderBy(x => x.CompraInsumo.FechaVencimiento)
                            .ToListAsync();
                    }
                    else
                    {
                        stockInsumos = await _context.StockInsumos
                            .Where(x =>
                                x.ProductoIntermedio.IdInsumo == fInsumo.IdInsumo &&
                                x.IdSede == request.IdSede &&
                                x.StockDisponible > 0 &&
                                x.ProductoIntermedio.FechaVencimiento >= ahora)
                            .OrderBy(x => x.ProductoIntermedio.FechaVencimiento)
                            .ToListAsync();
                    }

                    if (stockInsumos.Count == 0)
                    {
                        throw new BadRequestException(
                            $"No hay stock disponible para el insumo {fInsumo.CodigoInsumo ?? fInsumo.IdInsumo.ToString()} en esta sede.");
                    }

                    decimal stockDisponibleTotal =
                        stockInsumos.Sum(x => x.StockDisponible);

                    if (stockDisponibleTotal < cantidadUsar)
                    {
                        throw new BadRequestException(
                            $"Stock insuficiente para el insumo {fInsumo.CodigoInsumo ?? fInsumo.IdInsumo.ToString()}. " +
                            $"Disponible: {stockDisponibleTotal:0.000}, requerido: {cantidadUsar:0.000}");
                    }

                    var insumoProductoIntermedio =
                        new ProductoIntermedioMapper()
                            .CrearInsumosProductoIntermedio(fInsumo);

                    insumoProductoIntermedio.IdCreador = request.IdCreador;
                    insumoProductoIntermedio.ProductoIntermedio = productoIntermedio;

                    _context.InsumoProductoIntermedios.Add(
                        insumoProductoIntermedio);

                    foreach (var stockInsumo in stockInsumos)
                    {
                        if (cantidadUsar <= 0)
                            break;

                        decimal cantidadConsumida =
                            Math.Min(stockInsumo.StockDisponible, cantidadUsar);

                        _context.StockInsumoProductoIntermedios.Add(
                            new StockInsumoProductoIntermedio
                            {
                                Cantidad = cantidadConsumida,
                                IdCreador = request.IdCreador,
                                InsumoProductoIntermedio =
                                    insumoProductoIntermedio,
                                IdStockInsumo = stockInsumo.Id
                            });

                        stockInsumo.StockDisponible -= cantidadConsumida;
                        cantidadUsar -= cantidadConsumida;
                    }

                    if (cantidadUsar > 0)
                    {
                        throw new BadRequestException(
                            $"No se pudo completar el consumo del insumo {fInsumo.IdInsumo}");
                    }
                }

                // ============================================================
                // STOCK DEL PRODUCTO INTERMEDIO
                // ============================================================

                var stockDisponibleProductoIntermedio =
                    request.TipoUso == "PI-FMG"
                        ? request.LoteEstandar
                        : request.LoteEstTotal;

                var stockInsumoAdd = new StockInsumo
                {
                    Tipo = "PI",
                    ProductoIntermedio = productoIntermedio,
                    UnidadMedida = request.Um,
                    IdSede = request.IdSede,
                    StockDisponible = stockDisponibleProductoIntermedio
                };

                _context.StockInsumos.Add(stockInsumoAdd);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return productoIntermedio.Id;
            }
            catch (NotFoundException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (BadRequestException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Falló el rollback de la transacción al crear ProductoIntermedio");
                }

                _logger.LogError(
                    ex,
                    "Error al crear ProductoIntermedio: {Detail}",
                    ex.InnerException?.Message ?? ex.Message);

                throw new BadRequestException(
                    ex.InnerException != null
                        ? $"{ex.Message} -> {ex.InnerException.Message}"
                        : ex.Message);
            }
        }

        public async Task<int> ActualizarProductoIntermedio(
            int id,
            ActualizarProductoIntermedioReq request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var ahora = DateTime.UtcNow;

            try
            {
                if (request.TipoUso is not ("PI-FMG" or "PI-F%"))
                {
                    throw new BadRequestException(
                        $"Tipo de uso no válido: {request.TipoUso}");
                }

                var productoIntermedio = await _context.ProductosIntermedios
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (productoIntermedio == null)
                {
                    throw new NotFoundException(
                        $"No existe el Producto Intermedio con id {id}");
                }

                // ============================================================
                // OBTENER RELACIONES Y STOCK CONSUMIDO ANTERIORMENTE
                // ============================================================

                var empaquesProductoIntermedio =
                    await _context.EmpaqueProductoIntermedios
                        .Where(x => x.ProductoIntermedio.Id == id)
                        .Include(x => x.StockEmpaqueProductoIntermedios)
                        .ToListAsync();

                var consumosEmpaques = empaquesProductoIntermedio
                    .SelectMany(x => x.StockEmpaqueProductoIntermedios)
                    .ToList();

                foreach (var consumo in consumosEmpaques)
                {
                    var stockEmpaque = await _context.StockEmpaques
                        .FirstOrDefaultAsync(x => x.Id == consumo.IdStockEmpaque);

                    if (stockEmpaque != null)
                    {
                        stockEmpaque.StockDisponible += consumo.Cantidad;
                    }
                }

                var insumosProductoIntermedio =
                    await _context.InsumoProductoIntermedios
                        .Where(x => x.ProductoIntermedio.Id == id)
                        .Include(x => x.StockInsumoProductoIntermedios)
                        .ToListAsync();

                var consumosInsumos = insumosProductoIntermedio
                    .SelectMany(x => x.StockInsumoProductoIntermedios)
                    .ToList();

                foreach (var consumo in consumosInsumos)
                {
                    var stockInsumo = await _context.StockInsumos
                        .FirstOrDefaultAsync(x => x.Id == consumo.IdStockInsumo);

                    if (stockInsumo != null)
                    {
                        stockInsumo.StockDisponible += consumo.Cantidad;
                    }
                }

                // ============================================================
                // ELIMINAR CONSUMOS ANTERIORES
                // ============================================================

                if (consumosEmpaques.Count > 0)
                {
                    _context.StockEmpaqueProductoIntermedios.RemoveRange(
                        consumosEmpaques);
                }

                if (empaquesProductoIntermedio.Count > 0)
                {
                    _context.EmpaqueProductoIntermedios.RemoveRange(
                        empaquesProductoIntermedio);
                }

                if (consumosInsumos.Count > 0)
                {
                    _context.StockInsumoProductoIntermedios.RemoveRange(
                        consumosInsumos);
                }

                if (insumosProductoIntermedio.Count > 0)
                {
                    _context.InsumoProductoIntermedios.RemoveRange(
                        insumosProductoIntermedio);
                }

                // ============================================================
                // ACTUALIZAR PRODUCTO INTERMEDIO
                // ============================================================

                productoIntermedio.Lote = request.Lote;
                productoIntermedio.IdInsumo = request.IdInsumo;
                productoIntermedio.LoteEstandar = request.LoteEstandar;
                productoIntermedio.LoteEstTotal = request.LoteEstTotal;
                productoIntermedio.TipoUso = request.TipoUso;
                productoIntermedio.Um = request.Um;
                productoIntermedio.FechaEmision = request.FechaEmision;
                productoIntermedio.FechaVencimiento = request.FechaVencimiento;
                productoIntermedio.IdElaborado = request.IdElaborado;
                productoIntermedio.IdAutorizado = request.IdAutorizado;
                productoIntermedio.Procedimiento = request.Procedimiento;
                productoIntermedio.Aspecto = request.Aspecto;
                productoIntermedio.Color = request.Color;
                productoIntermedio.Olor = request.Olor;
                productoIntermedio.Ph = request.Ph;
                productoIntermedio.CondicionAlmacenamiento =
                    request.CondicionAlmacenamiento;
                productoIntermedio.IdModificador = request.IdModificador;

                if (productoIntermedio.FechaEmision.HasValue &&
                    productoIntermedio.FechaEmision.Value.Kind ==
                    DateTimeKind.Unspecified)
                {
                    productoIntermedio.FechaEmision =
                        DateTime.SpecifyKind(
                            productoIntermedio.FechaEmision.Value,
                            DateTimeKind.Utc);
                }

                if (productoIntermedio.FechaVencimiento.HasValue &&
                    productoIntermedio.FechaVencimiento.Value.Kind ==
                    DateTimeKind.Unspecified)
                {
                    productoIntermedio.FechaVencimiento =
                        DateTime.SpecifyKind(
                            productoIntermedio.FechaVencimiento.Value,
                            DateTimeKind.Utc);
                }

                // ============================================================
                // EMPAQUES
                // ============================================================

                var listaEmpaques = request.IdEmpaques?.ToList()
                    ?? new List<int>();

                if (listaEmpaques.Count > 0)
                {
                    var empaquesRelacionados = new List<int>();

                    foreach (var idEmpaque in listaEmpaques)
                    {
                        var empaque = await _context.Empaques
                            .Where(x => x.Id == idEmpaque)
                            .Select(x => new
                            {
                                x.IdCaja,
                                x.IdFunda,
                                x.IdEtiqueta1,
                                x.IdEtiqueta2
                            })
                            .FirstOrDefaultAsync();

                        if (empaque == null)
                        {
                            throw new NotFoundException(
                                $"No existe este Empaque con id {idEmpaque}");
                        }

                        empaquesRelacionados.Add(idEmpaque);

                        if (empaque.IdCaja.HasValue)
                            empaquesRelacionados.Add(empaque.IdCaja.Value);

                        if (empaque.IdFunda.HasValue)
                            empaquesRelacionados.Add(empaque.IdFunda.Value);

                        if (empaque.IdEtiqueta1.HasValue)
                            empaquesRelacionados.Add(empaque.IdEtiqueta1.Value);

                        if (empaque.IdEtiqueta2.HasValue)
                            empaquesRelacionados.Add(empaque.IdEtiqueta2.Value);
                    }

                    var conteoEmpaques = empaquesRelacionados
                        .GroupBy(x => x)
                        .ToDictionary(
                            x => x.Key,
                            x => (decimal)x.Count());

                    foreach (var conteoEmpaque in conteoEmpaques)
                    {
                        decimal cantidadPendiente = conteoEmpaque.Value;

                        var stockEmpaques = await _context.StockEmpaques
                            .Where(x =>
                                x.CompraEmpaque.IdEmpaque ==
                                    conteoEmpaque.Key &&
                                x.IdSede == productoIntermedio.IdSede &&
                                x.StockDisponible > 0 &&
                                x.CompraEmpaque.FechaVencimiento >= ahora)
                            .OrderBy(x =>
                                x.CompraEmpaque.FechaVencimiento)
                            .ToListAsync();

                        if (stockEmpaques.Count == 0)
                        {
                            throw new NotFoundException(
                                $"No hay stock disponible para el empaque " +
                                $"{conteoEmpaque.Key}");
                        }

                        var stockDisponible =
                            stockEmpaques.Sum(x => x.StockDisponible);

                        if (stockDisponible < cantidadPendiente)
                        {
                            throw new BadRequestException(
                                $"Stock insuficiente para el empaque " +
                                $"{conteoEmpaque.Key}. " +
                                $"Disponible: {stockDisponible}, " +
                                $"requerido: {cantidadPendiente}");
                        }

                        var empaqueProductoIntermedio =
                            new EmpaqueProductoIntermedio
                            {
                                IdEmpaque = conteoEmpaque.Key,
                                ProductoIntermedio = productoIntermedio
                            };

                        _context.EmpaqueProductoIntermedios.Add(
                            empaqueProductoIntermedio);

                        foreach (var stockEmpaque in stockEmpaques)
                        {
                            if (cantidadPendiente <= 0)
                                break;

                            decimal cantidadConsumida = Math.Min(
                                stockEmpaque.StockDisponible,
                                cantidadPendiente);

                            _context.StockEmpaqueProductoIntermedios.Add(
                                new StockEmpaqueProductoIntermedio
                                {
                                    Cantidad = cantidadConsumida,
                                    IdStockEmpaque = stockEmpaque.Id,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio =
                                        empaqueProductoIntermedio
                                });

                            stockEmpaque.StockDisponible -= cantidadConsumida;
                            cantidadPendiente -= cantidadConsumida;
                        }

                        if (cantidadPendiente > 0)
                        {
                            throw new BadRequestException(
                                $"No se pudo completar el consumo del " +
                                $"empaque {conteoEmpaque.Key}");
                        }
                    }
                }

                // ============================================================
                // INSUMOS
                // ============================================================

                foreach (var fInsumo in request.Insumos)
                {
                    if (fInsumo.Tipo is not ("MP" or "PI"))
                    {
                        throw new BadRequestException(
                            $"Tipo de insumo no válido: {fInsumo.Tipo}");
                    }

                    decimal cantidadUsar = fInsumo.CantidadLote;

                    List<StockInsumo> stockInsumos;

                    if (fInsumo.Tipo == "MP")
                    {
                        stockInsumos = await _context.StockInsumos
                            .Where(x =>
                                x.CompraInsumo.IdInsumo ==
                                    fInsumo.IdInsumo &&
                                x.StockDisponible > 0 &&
                                x.CompraInsumo.FechaVencimiento >= ahora)
                            .OrderBy(x =>
                                x.CompraInsumo.FechaVencimiento)
                            .ToListAsync();
                    }
                    else
                    {
                        stockInsumos = await _context.StockInsumos
                            .Where(x =>
                                x.ProductoIntermedio.IdInsumo ==
                                    fInsumo.IdInsumo &&
                                x.StockDisponible > 0 &&
                                x.ProductoIntermedio.FechaVencimiento >= ahora)
                            .OrderBy(x =>
                                x.ProductoIntermedio.FechaVencimiento)
                            .ToListAsync();
                    }

                    if (stockInsumos.Count == 0)
                    {
                        throw new NotFoundException(
                            $"No hay stock disponible para el insumo " +
                            $"{fInsumo.IdInsumo}");
                    }

                    var stockDisponible =
                        stockInsumos.Sum(x => x.StockDisponible);

                    if (stockDisponible < cantidadUsar)
                    {
                        throw new BadRequestException(
                            $"Stock insuficiente para el insumo " +
                            $"{fInsumo.IdInsumo}. " +
                            $"Disponible: {stockDisponible}, " +
                            $"requerido: {cantidadUsar}");
                    }

                    var insumoProductoIntermedio =
                        new ProductoIntermedioMapper()
                            .CrearInsumosProductoIntermedio(fInsumo);

                    insumoProductoIntermedio.IdCreador =
                        request.IdModificador;

                    insumoProductoIntermedio.ProductoIntermedio =
                        productoIntermedio;

                    _context.InsumoProductoIntermedios.Add(
                        insumoProductoIntermedio);

                    foreach (var stockInsumo in stockInsumos)
                    {
                        if (cantidadUsar <= 0)
                            break;

                        decimal cantidadConsumida = Math.Min(
                            stockInsumo.StockDisponible,
                            cantidadUsar);

                        _context.StockInsumoProductoIntermedios.Add(
                            new StockInsumoProductoIntermedio
                            {
                                Cantidad = cantidadConsumida,
                                IdCreador = request.IdModificador,
                                InsumoProductoIntermedio =
                                    insumoProductoIntermedio,
                                IdStockInsumo = stockInsumo.Id
                            });

                        stockInsumo.StockDisponible -= cantidadConsumida;
                        cantidadUsar -= cantidadConsumida;
                    }

                    if (cantidadUsar > 0)
                    {
                        throw new BadRequestException(
                            $"No se pudo completar el consumo del insumo " +
                            $"{fInsumo.IdInsumo}");
                    }
                }

                // ============================================================
                // ACTUALIZAR STOCK DEL PRODUCTO INTERMEDIO
                // ============================================================

                var stockProductoIntermedio =
                    await _context.StockInsumos
                        .FirstOrDefaultAsync(x =>
                            x.ProductoIntermedio.Id == id &&
                            x.Tipo == "PI");

                decimal nuevoStock =
                    request.TipoUso == "PI-FMG"
                        ? request.LoteEstandar
                        : request.LoteEstTotal;

                if (stockProductoIntermedio == null)
                {
                    stockProductoIntermedio = new StockInsumo
                    {
                        Tipo = "PI",
                        ProductoIntermedio = productoIntermedio,
                        UnidadMedida = request.Um,
                        IdSede = productoIntermedio.IdSede,
                        StockDisponible = nuevoStock
                    };

                    _context.StockInsumos.Add(stockProductoIntermedio);
                }
                else
                {
                    stockProductoIntermedio.UnidadMedida = request.Um;
                    stockProductoIntermedio.IdSede = productoIntermedio.IdSede;
                    stockProductoIntermedio.StockDisponible = nuevoStock;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return productoIntermedio.Id;
            }
            catch (NotFoundException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (BadRequestException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(
                        rollbackEx,
                        "Falló el rollback al actualizar ProductoIntermedio {Id}",
                        id);
                }

                _logger.LogError(
                    ex,
                    "Error al actualizar ProductoIntermedio {Id}: {Detail}",
                    id,
                    ex.InnerException?.Message ?? ex.Message);

                throw new BadRequestException(
                    ex.InnerException != null
                        ? $"{ex.Message} -> {ex.InnerException.Message}"
                        : ex.Message);
            }
        }
        public async Task<IEnumerable<ConsumoPIRes>> DetalleConsumo(int id)
        {
            // 1. Intentar obtener consumos registrados con lotes de stock
            var stockConsumos = await _context.StockInsumoProductoIntermedios
                .Where(w => w.InsumoProductoIntermedio.IdProductoIntermedio == id)
                .OrderBy(ob => ob.InsumoProductoIntermedio.Variable)
                .Select(s => new ConsumoPIRes()
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.InsumoProductoIntermedio.IdInsumo),
                    Porcentaje = s.InsumoProductoIntermedio.Porcentaje,
                    Descripcion = s.InsumoProductoIntermedio.Insumo != null ? s.InsumoProductoIntermedio.Insumo.Descripcion : "",
                    V = s.InsumoProductoIntermedio.Variable,
                    Lote = s.StockInsumo != null && s.StockInsumo.CompraInsumo != null ? s.StockInsumo.CompraInsumo.Lote : "",
                    Registro = s.IdStockInsumo > 0 ? Alfanumerico.ConvertToBase36(s.IdStockInsumo) : "",
                    CantidadUnidad = s.Cantidad,
                    FactorCorreccion = s.InsumoProductoIntermedio.FactorCorrecion,
                    Dilucion = s.InsumoProductoIntermedio.Dilucion,
                    Um = s.UnidadMedida,
                    CantidadLote = s.Cantidad,
                    Practica = s.InsumoProductoIntermedio.Practica,
                    CSP = s.InsumoProductoIntermedio.Csp
                })
                .AsNoTracking()
                .ToListAsync();

            if (stockConsumos != null && stockConsumos.Any())
            {
                return stockConsumos;
            }

            // 2. Si no hay lotes de stock vinculados, obtener directamente de los insumos de la fórmula del producto intermedio
            var insumosFormula = await _context.InsumoProductoIntermedios
                .Where(w => w.IdProductoIntermedio == id)
                .OrderBy(ob => ob.Variable)
                .Select(s => new ConsumoPIRes()
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdInsumo),
                    Porcentaje = s.Porcentaje,
                    Descripcion = s.Insumo != null ? s.Insumo.Descripcion : "",
                    V = s.Variable,
                    Lote = "",
                    Registro = "",
                    CantidadUnidad = s.CantidadUnidad,
                    FactorCorreccion = s.FactorCorrecion,
                    Dilucion = s.Dilucion,
                    Um = s.UnidadMedida,
                    CantidadLote = s.CantidadLote,
                    Practica = s.Practica,
                    CSP = s.Csp
                })
                .AsNoTracking()
                .ToListAsync();

            if (insumosFormula != null && insumosFormula.Any())
            {
                return insumosFormula;
            }

            // 3. Fallback maestro: Buscar por el IdInsumo asociado al ProductoIntermedio (fórmula patrón)
            var pi = await _context.ProductosIntermedios.FindAsync(id);
            if (pi != null && pi.IdInsumo > 0)
            {
                int idInsumoMaster = pi.IdInsumo;
                var latestPIId = await _context.InsumoProductoIntermedios
                    .Where(w => w.ProductoIntermedio != null && w.ProductoIntermedio.IdInsumo == idInsumoMaster)
                    .OrderByDescending(ob => ob.IdProductoIntermedio)
                    .Select(s => s.IdProductoIntermedio)
                    .FirstOrDefaultAsync();

                if (latestPIId > 0)
                {
                    return await _context.InsumoProductoIntermedios
                        .Where(w => w.IdProductoIntermedio == latestPIId)
                        .OrderBy(ob => ob.Variable)
                        .Select(s => new ConsumoPIRes()
                        {
                            Codigo = UtilFamilia.CodigoInsumo(s.IdInsumo),
                            Porcentaje = s.Porcentaje,
                            Descripcion = s.Insumo != null ? s.Insumo.Descripcion : "",
                            V = s.Variable,
                            Lote = "",
                            Registro = "",
                            CantidadUnidad = s.CantidadUnidad,
                            FactorCorreccion = s.FactorCorrecion,
                            Dilucion = s.Dilucion,
                            Um = s.UnidadMedida,
                            CantidadLote = s.CantidadLote,
                            Practica = s.Practica,
                            CSP = s.Csp
                        })
                        .AsNoTracking()
                        .ToListAsync();
                }
            }

            return new List<ConsumoPIRes>();
        }

        public async Task<bool> ActualizarCondicionAlmacenamiento(int id, string condicion)
        {
            var pi = await _context.ProductosIntermedios.FindAsync(id);
            if (pi == null) return false;

            pi.CondicionAlmacenamiento = condicion;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TablaPIRes>> ListaProductoIntermedio(int? idSede = null)
        {
            var query = _context.ProductosIntermedios.AsQueryable();
            if (idSede.HasValue && idSede.Value > 0)
            {
                query = query.Where(w => w.IdSede == idSede.Value);
            }

            IEnumerable<TablaPIRes> response = await query
            .OrderByDescending(ob => ob.Id)
            .Select(s => new TablaPIRes()
            {
                Id = s.Id,
                Registro = "PI" + Alfanumerico.ConvertToBase36(s.Id),
                Lote = s.Lote,
                Codigo = s.Insumo != null ? UtilFamilia.CodigoInsumo(s.Insumo.Id) : "",
                Descripcion = s.Insumo != null ? s.Insumo.Descripcion : "",
                LoteEstandar = s.LoteEstandar ?? 0,
                PesoUnidad = s.PesoUnidad,
                LoteEstTotal = s.LoteEstTotal,
                Cantidad = (s.PesoUnidad.HasValue && s.PesoUnidad.Value > 0)
                    ? s.PesoUnidad.Value
                    : ((s.LoteEstTotal.HasValue && s.LoteEstTotal.Value > 0)
                        ? s.LoteEstTotal.Value
                        : (s.LoteEstandar ?? 0)),
                TipoUso = s.Insumo != null ? s.Insumo.Tipo : s.TipoUso,
                Um = (s.Insumo != null && !string.IsNullOrEmpty(s.Insumo.UnidadMedida)) ? s.Insumo.UnidadMedida : (s.Um ?? "G"),
                FechaEmision = s.FechaEmision.Value,
                FechaVencimiento = s.FechaVencimiento,
                Elaborado = s.Elaborador != null ? s.Elaborador.Codigo : "",
                CondicionAlmacenamiento = s.CondicionAlmacenamiento
            })
            .AsNoTracking()
            .ToListAsync();

            return response;
        }

        public async Task<IEnumerable<MasterPIRes>> ListaMaestraPI(string tipoUso)
        {
            var query = _context.Insumos
                .Where(i => i.Clasificacion == "PI" && i.Tipo != null && i.Tipo.ToLower() == tipoUso.ToLower());

            var insumos = await query
                .Select(i => new MasterPIRes
                {
                    IdInsumo = i.Id,
                    Codigo = UtilFamilia.CodigoInsumo(i.Id),
                    Descripcion = i.Descripcion,
                    TipoUso = i.Tipo,
                    Um = i.UnidadMedida,
                    FormaFarmaceutica = i.FormaFarmaceutica,
                    UltimoProductoIntermedioId = _context.ProductosIntermedios
                        .Where(pi => pi.IdInsumo == i.Id)
                        .OrderByDescending(pi => pi.Id)
                        .Select(pi => (int?)pi.Id)
                        .FirstOrDefault()
                })
                .AsNoTracking()
                .ToListAsync();

            return insumos;
        }
        public async Task<int> EliminarProductoIntermedio(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cargar el ProductoIntermedio con todas sus relaciones de consumo
                ProductoIntermedio productoIntermedio = await _context.ProductosIntermedios
                    .Include(p => p.EmpaqueProductoIntermedios)
                        .ThenInclude(e => e.StockEmpaqueProductoIntermedios)
                            .ThenInclude(c => c.StockEmpaque)
                    .Include(p => p.InsumoProductoIntermedio)
                        .ThenInclude(i => i.StockInsumoProductoIntermedios)
                            .ThenInclude(c => c.StockInsumo)
                    .FirstOrDefaultAsync(p => p.Id == id)
                    ?? throw new NotFoundException("No existe este ProductoIntermedio con id " + id);

                // 2. REVERTIR stock de Empaques ya consumidos
                foreach (var empaqueProdInt in productoIntermedio.EmpaqueProductoIntermedios.ToList())
                {
                    foreach (var compraEmpaqueProdInt in empaqueProdInt.StockEmpaqueProductoIntermedios.ToList())
                    {
                        compraEmpaqueProdInt.StockEmpaque.StockDisponible += compraEmpaqueProdInt.Cantidad;
                        _context.StockEmpaqueProductoIntermedios.Remove(compraEmpaqueProdInt);
                    }
                    _context.EmpaqueProductoIntermedios.Remove(empaqueProdInt);
                }

                // 3. REVERTIR stock de Insumos ya consumidos
                foreach (var insumoProdInt in productoIntermedio.InsumoProductoIntermedio.ToList())
                {
                    foreach (var compraInsumoProdInt in insumoProdInt.StockInsumoProductoIntermedios.ToList())
                    {
                        compraInsumoProdInt.StockInsumo.StockDisponible += compraInsumoProdInt.Cantidad;
                        _context.StockInsumoProductoIntermedios.Remove(compraInsumoProdInt);
                    }
                    _context.InsumoProductoIntermedios.Remove(insumoProdInt);
                }

                // 4. Eliminar el ProductoIntermedio
                _context.ProductosIntermedios.Remove(productoIntermedio);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return productoIntermedio.Id;
            }
            catch (Exception ex) when (ex is NotFoundException || ex is BadRequestException)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"Rollback falló: {rollbackEx}");
                }
                throw;
            }
        }

        public async Task<RegistroPIRes> ObtenerRegistro()
        {
            int currentValue = await _context.Database
    .SqlQuery<int>($"""
        SELECT last_value AS "Value"
        FROM base_y_insumo_sequence
    """)
    .SingleAsync();

            var hoy = DateTime.Today;

            var siguienteNumero = await _context.ProductosIntermedios
     .CountAsync(x => x.FechaCreacion.Date == hoy) + 1;


            string lote = $"PI-{hoy:yyyyMMdd}{siguienteNumero}";

            string registro = Alfanumerico.ConvertToBase36(currentValue + 1);

            RegistroPIRes response = new()
            {
                Lote = lote,
                NroReg = registro
            };
            return response;
        }


        public async Task<ObtenerProductoIntermedioReq?> ObtenerPI(int id)
        {
            return await _context.ProductosIntermedios
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ObtenerProductoIntermedioReq
                {
                    Lote = x.Lote,
                    IdInsumo = x.IdInsumo,
                    LoteEstandar = x.LoteEstandar.Value,
                    PesoUnidad = x.PesoUnidad,
                    LoteEstTotal = x.LoteEstTotal,
                    TipoUso = x.TipoUso,
                    Um = x.Um,
                    FechaEmision = x.FechaEmision,
                    FechaVencimiento = x.FechaVencimiento,
                    IdElaborado = x.IdElaborado.Value,
                    IdAutorizado = x.IdAutorizado,
                    Procedimiento = x.Procedimiento,
                    Aspecto = x.Aspecto,
                    Color = x.Color,
                    Olor = x.Olor,
                    Ph = x.Ph.Value,

                    IdEmpaques = x.EmpaqueProductoIntermedios
                        .Select(e => e.IdEmpaque)
                        .ToList(),

                    Insumos = x.InsumoProductoIntermedio
                        .Select(i => new InsumoProductoIntermedioRes
                        {
                            IdInsumo = i.IdInsumo,
                            CodigoInsumo = UtilFamilia.CodigoInsumo(i.IdInsumo),
                            Porcentaje = i.Porcentaje,
                            Variable = i.Variable,
                            CantidadUnidad = i.CantidadUnidad,
                            FactorCorrecion = i.FactorCorrecion,
                            Dilucion = i.Dilucion,
                            UnidadMedida = i.UnidadMedida,
                            CantidadLote = i.CantidadLote,
                            Practica = i.Practica,
                            Csp = i.Csp
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

    }
}
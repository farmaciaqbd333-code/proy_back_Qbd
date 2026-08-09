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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ProductoIntermedio productoIntermedio = new ProductoIntermedioMapper().CrearProductoIntermedio(request);
                _context.ProductosIntermedios.Add(productoIntermedio);
                List<int> listaEmpaques = request.IdEmpaques.ToList();
                if (listaEmpaques.Any())
                {

                    foreach (var item in request.IdEmpaques)
                    {
                        var empaque = await _context.Empaques
                        .Where(w => w.Id == item)
                        .Select(s => new
                        {
                            idCaja = s.IdCaja,
                            idFunda = s.IdFunda,
                            idEtiqueta1 = s.IdEtiqueta1,
                            idEtiqueta2 = s.IdEtiqueta2
                        })
                        .FirstOrDefaultAsync() ?? throw new NotFoundException("No existe este Empaque con id " + item);
                        listaEmpaques.AddRange(
                            new int?[]
                            {
                                empaque.idCaja,
                                empaque.idFunda,
                                empaque.idEtiqueta1,
                                empaque.idEtiqueta2
                            }
                            .OfType<int>()
                        );

                    }
                    Dictionary<int, decimal> conteoEmpaques = listaEmpaques
    .GroupBy(x => x)
    .ToDictionary(g => g.Key, g => (decimal)g.Count());

                    _logger.LogInformation(
                        "Iniciando consumo de empaques. Total de tipos de empaque: {CantidadTiposEmpaque}",
                        conteoEmpaques.Count);

                    foreach (var conteoEmpaque in conteoEmpaques)
                    {
                        decimal cantidadPendiente = conteoEmpaque.Value;

                        _logger.LogInformation(
                            "Procesando empaque {IdEmpaque}. Cantidad requerida: {CantidadRequerida}",
                            conteoEmpaque.Key,
                            cantidadPendiente);

                        List<StockEmpaque> stockEmpaques = await _context.StockEmpaques
                            .Where(w =>
                                w.CompraEmpaque.IdEmpaque == conteoEmpaque.Key &&
                                w.IdSede == request.IdSede &&
                                w.CompraEmpaque.FechaVencimiento >= DateTime.UtcNow)
                            .OrderBy(w => w.CompraEmpaque.FechaVencimiento)
                            .ToListAsync();

                        _logger.LogInformation(
                            "Stock encontrado para empaque {IdEmpaque}: {CantidadRegistros} registros",
                            conteoEmpaque.Key,
                            stockEmpaques.Count);

                        if (stockEmpaques.Count == 0)
                        {
                            _logger.LogWarning(
                                "No hay stock disponible para el empaque {IdEmpaque}",
                                conteoEmpaque.Key);

                            throw new NotFoundException(
                                "No hay stock disponible para este Empaque");
                        }

                        decimal stockDisponibleTotal = stockEmpaques.Sum(s => s.StockDisponible);

                        _logger.LogInformation(
                            "Empaque {IdEmpaque}. Stock disponible total: {StockDisponible}. Cantidad requerida: {CantidadRequerida}",
                            conteoEmpaque.Key,
                            stockDisponibleTotal,
                            cantidadPendiente);

                        if (stockDisponibleTotal < cantidadPendiente)
                        {
                            _logger.LogWarning(
                                "Stock insuficiente para empaque {IdEmpaque}. Stock disponible: {StockDisponible}, requerido: {CantidadRequerida}",
                                conteoEmpaque.Key,
                                stockDisponibleTotal,
                                cantidadPendiente);

                            throw new BadRequestException("Stock empaque insuficiente");
                        }

                        EmpaqueProductoIntermedio empaqueProductoIntermedio = new()
                        {
                            IdEmpaque = conteoEmpaque.Key,
                            ProductoIntermedio = productoIntermedio
                        };

                        _context.EmpaqueProductoIntermedios.Add(empaqueProductoIntermedio);

                        foreach (StockEmpaque stockEmpaque in stockEmpaques)
                        {
                            _logger.LogInformation(
                                "Evaluando lote de stock. IdStockEmpaque: {IdStockEmpaque}, IdCompraEmpaque: {IdCompraEmpaque}, StockDisponible: {StockDisponible}, CantidadPendiente: {CantidadPendiente}",
                                stockEmpaque.Id,
                                stockEmpaque.IdStockEmpaque,
                                stockEmpaque.StockDisponible,
                                cantidadPendiente);

                            StockEmpaqueProductoIntermedio stockEmpaqueProductoIntermedio;

                            if (stockEmpaque.StockDisponible >= cantidadPendiente)
                            {
                                stockEmpaqueProductoIntermedio = new()
                                {
                                    Cantidad = cantidadPendiente,
                                    IdStockEmpaque = stockEmpaque.IdStockEmpaque,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio = empaqueProductoIntermedio
                                };

                                stockEmpaque.StockDisponible -= cantidadPendiente;

                                _context.StockEmpaqueProductoIntermedios
                                    .Add(stockEmpaqueProductoIntermedio);

                                _logger.LogInformation(
                                    "Consumo completado desde un solo lote. IdCompraEmpaque: {IdCompraEmpaque}, CantidadConsumida: {CantidadConsumida}, StockRestante: {StockRestante}",
                                    stockEmpaque.IdStockEmpaque,
                                    cantidadPendiente,
                                    stockEmpaque.StockDisponible);

                                cantidadPendiente = 0;
                                break;
                            }
                            else
                            {
                                decimal cantidadConsumida = stockEmpaque.StockDisponible;

                                stockEmpaqueProductoIntermedio = new()
                                {
                                    Cantidad = cantidadConsumida,
                                    IdStockEmpaque = stockEmpaque.IdStockEmpaque,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio = empaqueProductoIntermedio
                                };

                                cantidadPendiente -= cantidadConsumida;
                                stockEmpaque.StockDisponible = 0;

                                _context.StockEmpaqueProductoIntermedios
                                    .Add(stockEmpaqueProductoIntermedio);

                                _logger.LogInformation(
                                    "Consumo parcial del lote. IdCompraEmpaque: {IdCompraEmpaque}, CantidadConsumida: {CantidadConsumida}, CantidadPendiente: {CantidadPendiente}, StockRestante: 0",
                                    stockEmpaque.IdStockEmpaque,
                                    cantidadConsumida,
                                    cantidadPendiente);
                            }
                        }

                        if (cantidadPendiente > 0)
                        {
                            _logger.LogError(
                                "No se pudo completar el consumo del empaque {IdEmpaque}. Cantidad pendiente: {CantidadPendiente}",
                                conteoEmpaque.Key,
                                cantidadPendiente);

                            throw new BadRequestException(
                                "No se pudo completar el consumo del empaque");
                        }

                        _logger.LogInformation(
                            "Consumo del empaque {IdEmpaque} completado correctamente",
                            conteoEmpaque.Key);
                    }

                    _logger.LogInformation("Procesamiento de empaques finalizado correctamente.");
                }
                foreach (var fInsumo in request.Insumos)
                {
                    _logger.LogInformation(
                        "Iniciando consumo de insumo. IdInsumo: {IdInsumo}, CantidadRequerida: {CantidadRequerida}",
                        fInsumo.IdInsumo,
                        fInsumo.CantidadLote);

                    List<StockInsumo> stockInsumos = await _context.StockInsumos                        
                        .Where(w =>
                            w.CompraInsumo.IdInsumo == fInsumo.IdInsumo &&
                            w.StockDisponible > 0 &&
                            w.CompraInsumo.FechaVencimiento >= DateTime.UtcNow)
                        .OrderBy(w => w.CompraInsumo.FechaVencimiento)
                        .ToListAsync();

                    _logger.LogInformation(
                        "Stock encontrado para insumo {IdInsumo}: {CantidadRegistros} lotes",
                        fInsumo.IdInsumo,
                        stockInsumos.Count);

                    decimal stockDisponibleTotal = stockInsumos.Sum(s => s.StockDisponible);
                    decimal cantidadUsar = fInsumo.CantidadLote;

                    _logger.LogInformation(
                        "Stock del insumo {IdInsumo}. StockDisponibleTotal: {StockDisponibleTotal}, CantidadRequerida: {CantidadRequerida}",
                        fInsumo.IdInsumo,
                        stockDisponibleTotal,
                        cantidadUsar);

                    if (!stockInsumos.Any())
                    {
                        _logger.LogWarning(
                            "No se encontró stock disponible para el insumo {IdInsumo}",
                            fInsumo.IdInsumo);

                        throw new NotFoundException(
                            "No hay stock disponible para este insumo, " + stockDisponibleTotal);
                    }

                    if (stockDisponibleTotal < cantidadUsar)
                    {
                        _logger.LogWarning(
                            "Stock insuficiente para el insumo {IdInsumo}. StockDisponible: {StockDisponible}, CantidadRequerida: {CantidadRequerida}",
                            fInsumo.IdInsumo,
                            stockDisponibleTotal,
                            cantidadUsar);

                        throw new NotFoundException(
                            "No hay stock disponible para este insumo, " + stockDisponibleTotal);
                    }

                    InsumoProductoIntermedio insumoProductoIntermedio =
                        new ProductoIntermedioMapper()
                            .CrearInsumosProductoIntermedio(fInsumo);

                    insumoProductoIntermedio.IdCreador = request.IdCreador;
                    insumoProductoIntermedio.ProductoIntermedio = productoIntermedio;

                    _context.InsumoProductoIntermedios.Add(insumoProductoIntermedio);

                    _logger.LogInformation(
                        "InsumoProductoIntermedio creado para el insumo {IdInsumo}. IdCreador: {IdCreador}",
                        fInsumo.IdInsumo,
                        request.IdCreador);

                    foreach (var stockInsumo in stockInsumos)
                    {
                        _logger.LogInformation(
                            "Procesando lote de insumo. IdInsumo: {IdInsumo}, IdCompraInsumo: {IdCompraInsumo}, StockDisponible: {StockDisponible}, CantidadPendiente: {CantidadPendiente}",
                            fInsumo.IdInsumo,
                            stockInsumo.IdCompraInsumo,
                            stockInsumo.StockDisponible,
                            cantidadUsar);

                        if (stockInsumo.StockDisponible < cantidadUsar)
                        {
                            decimal cantidadConsumida = stockInsumo.StockDisponible;

                            _context.StockInsumoProductoIntermedios.Add(
                                new StockInsumoProductoIntermedio()
                                {
                                    Cantidad = cantidadConsumida,
                                    IdCreador = request.IdCreador,
                                    InsumoProductoIntermedio = insumoProductoIntermedio,
                                    IdStockInsumo = stockInsumo.IdCompraInsumo
                                });

                            cantidadUsar -= cantidadConsumida;
                            stockInsumo.StockDisponible = 0;

                            _logger.LogInformation(
                                "Consumo parcial de lote. IdInsumo: {IdInsumo}, IdCompraInsumo: {IdCompraInsumo}, CantidadConsumida: {CantidadConsumida}, CantidadPendiente: {CantidadPendiente}, StockRestante: 0",
                                fInsumo.IdInsumo,
                                stockInsumo.IdCompraInsumo,
                                cantidadConsumida,
                                cantidadUsar);
                        }
                        else
                        {
                            decimal cantidadConsumida = cantidadUsar;

                            _context.StockInsumoProductoIntermedios.Add(
                                new StockInsumoProductoIntermedio()
                                {
                                    Cantidad = cantidadConsumida,
                                    IdCreador = request.IdCreador,
                                    InsumoProductoIntermedio = insumoProductoIntermedio,
                                    IdStockInsumo = stockInsumo.IdCompraInsumo
                                });

                            stockInsumo.StockDisponible -= cantidadConsumida;
                            cantidadUsar = 0;

                            _logger.LogInformation(
                                "Consumo completado desde lote. IdInsumo: {IdInsumo}, IdCompraInsumo: {IdCompraInsumo}, CantidadConsumida: {CantidadConsumida}, StockRestante: {StockRestante}",
                                fInsumo.IdInsumo,
                                stockInsumo.IdCompraInsumo,
                                cantidadConsumida,
                                stockInsumo.StockDisponible);

                            break;
                        }
                    }

                    if (cantidadUsar > 0)
                    {
                        _logger.LogError(
                            "No se pudo completar el consumo del insumo {IdInsumo}. CantidadPendiente: {CantidadPendiente}",
                            fInsumo.IdInsumo,
                            cantidadUsar);

                        throw new BadRequestException(
                            "No se pudo completar el consumo del insumo");
                    }

                    _logger.LogInformation(
                        "Consumo del insumo {IdInsumo} completado correctamente. CantidadConsumida: {CantidadConsumida}",
                        fInsumo.IdInsumo,
                        fInsumo.CantidadLote);
                }

                if (productoIntermedio.FechaEmision.Kind == DateTimeKind.Unspecified)
                {
                    productoIntermedio.FechaEmision = DateTime.SpecifyKind(productoIntermedio.FechaEmision, DateTimeKind.Utc);
                }
                if (productoIntermedio.FechaVencimiento.Kind == DateTimeKind.Unspecified)
                {
                    productoIntermedio.FechaVencimiento = DateTime.SpecifyKind(productoIntermedio.FechaVencimiento, DateTimeKind.Utc);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return productoIntermedio.Id;
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"Rollback falló: {rollbackEx}");
                }

                _logger.LogError(ex, "Error al crear ProductoIntermedio: {Detail}", ex.InnerException?.Message ?? ex.Message);
                var innerMsg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                throw new BadRequestException(innerMsg);
            }

        }

        public async Task<int> ActualizarProductoIntermedio(int id, ActualizarProductoIntermedioReq request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cargar el ProductoIntermedio existente con todas sus relaciones de consumo
                ProductoIntermedio productoIntermedio = await _context.ProductosIntermedios
                    .Include(p => p.EmpaqueProductoIntermedios)
                        .ThenInclude(e => e.StockEmpaqueProductoIntermedios)
                            .ThenInclude(c => c.StockEmpaque)
                    .Include(p => p.InsumoProductoIntermedio)
                        .ThenInclude(i => i.CompraInsumoProductoIntermedio)
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
                    foreach (var compraInsumoProdInt in insumoProdInt.CompraInsumoProductoIntermedio.ToList())
                    {
                        compraInsumoProdInt.StockInsumo.StockDisponible += compraInsumoProdInt.Cantidad;
                        _context.StockInsumoProductoIntermedios.Remove(compraInsumoProdInt);
                    }
                    _context.InsumoProductoIntermedios.Remove(insumoProdInt);
                }

                // Persistimos la reversión antes de recalcular, para evitar inconsistencias
                // al recontar stock disponible en las siguientes consultas
                await _context.SaveChangesAsync();

                // 4. Actualizar campos base del ProductoIntermedio (ajusta según tu Mapper real)
                new ProductoIntermedioMapper().ActualizarProductoIntermedio(request, productoIntermedio);

                // 5. Volver a recorrer Empaques con los NUEVOS datos del request (misma lógica que Crear)
                var listaEmpaques = new List<int>(request.IdEmpaques);

                if (listaEmpaques.Any())
                {
                    foreach (var item in request.IdEmpaques)
                    {
                        var empaque = await _context.Empaques
                        .Where(w => w.Id == item)
                        .Select(s => new
                        {
                            idCaja = s.IdCaja,
                            idFunda = s.IdFunda,
                            idEtiqueta1 = s.IdEtiqueta1,
                            idEtiqueta2 = s.IdEtiqueta2
                        })
                        .FirstOrDefaultAsync() ?? throw new NotFoundException("No existe este Empaque con id " + item);
                        listaEmpaques.AddRange(
                            new int?[]
                            {
                        empaque.idCaja,
                        empaque.idFunda,
                        empaque.idEtiqueta1,
                        empaque.idEtiqueta2
                            }
                            .OfType<int>()
                        );
                    }

                    Dictionary<int, decimal> conteoEmpaques = listaEmpaques
                        .GroupBy(x => x)
                        .ToDictionary(g => g.Key, g => (decimal)g.Count());

                    foreach (var conteoEmpaque in conteoEmpaques)
                    {
                        decimal cantidadPendiente = conteoEmpaque.Value;
                        List<StockEmpaque> stockEmpaques = await _context.StockEmpaques
                            .Where(w => w.CompraEmpaque.IdEmpaque == conteoEmpaque.Key && w.StockDisponible > 0 && w.CompraEmpaque.FechaVencimiento >= DateTime.UtcNow)
                            .OrderBy(w => w.CompraEmpaque.FechaVencimiento)
                            .ToListAsync();
                        decimal stockDisponibleTotal = stockEmpaques.Sum(s => s.StockDisponible);

                        if (stockDisponibleTotal < conteoEmpaque.Value) throw new BadRequestException("Stock insuficiente");
                        if (stockEmpaques.Count() == 0) throw new NotFoundException("No hay stock disponible para este Empaque");

                        EmpaqueProductoIntermedio empaqueProductoIntermedio = new()
                        {
                            IdEmpaque = conteoEmpaque.Key,
                            ProductoIntermedio = productoIntermedio
                        };
                        _context.EmpaqueProductoIntermedios.Add(empaqueProductoIntermedio);

                        foreach (StockEmpaque stockEmpaque in stockEmpaques)
                        {
                            StockEmpaqueProductoIntermedio compraEmpaqueProductoIntermedio;
                            if (stockEmpaque.StockDisponible >= cantidadPendiente)
                            {
                                compraEmpaqueProductoIntermedio = new()
                                {
                                    Cantidad = cantidadPendiente,
                                    IdStockEmpaque = stockEmpaque.IdStockEmpaque,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio = empaqueProductoIntermedio
                                };
                                stockEmpaque.StockDisponible -= cantidadPendiente;
                                cantidadPendiente = 0;
                                _context.StockEmpaqueProductoIntermedios.Add(compraEmpaqueProductoIntermedio);
                                break;
                            }
                            else
                            {
                                compraEmpaqueProductoIntermedio = new()
                                {
                                    Cantidad = stockEmpaque.StockDisponible,
                                    IdStockEmpaque = stockEmpaque.IdStockEmpaque,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio = empaqueProductoIntermedio
                                };
                                cantidadPendiente -= stockEmpaque.StockDisponible;
                                stockEmpaque.StockDisponible = 0;
                                _context.StockEmpaqueProductoIntermedios.Add(compraEmpaqueProductoIntermedio);
                            }
                        }
                        if (cantidadPendiente > 0)
                            throw new BadRequestException("No se pudo completar el consumo del empaque");
                    }
                }

                // 6. Volver a recorrer Insumos con los NUEVOS datos del request (misma lógica que Crear)
                foreach (var fInsumo in request.Insumos)
                {
                    List<StockInsumo> stockInsumos = await _context.StockInsumos
                        .Include(w => w.StockInsumoProductoIntermedio)
                        .Where(w => w.CompraInsumo.IdInsumo == fInsumo.IdInsumo && w.StockDisponible > 0 && w.CompraInsumo.FechaVencimiento >= DateTime.UtcNow)
                        .OrderBy(w => w.CompraInsumo.FechaVencimiento)
                        .ToListAsync();
                    decimal stockDisponibleTotal = stockInsumos.Sum(s => s.StockDisponible);
                    decimal cantidadUsar = fInsumo.CantidadLote;

                    if (!stockInsumos.Any() || stockDisponibleTotal < cantidadUsar)
                    {
                        _logger.LogInformation("stock disponible total: {cantidad}", stockDisponibleTotal);
                        throw new NotFoundException("No hay stock disponible para este insumo, " + stockDisponibleTotal);
                    }

                    InsumoProductoIntermedio insumoProductoIntermedio = new ProductoIntermedioMapper().CrearInsumosProductoIntermedio(fInsumo);
                    insumoProductoIntermedio.IdCreador = request.IdModificador;
                    insumoProductoIntermedio.ProductoIntermedio = productoIntermedio;
                    _context.InsumoProductoIntermedios.Add(insumoProductoIntermedio);

                    foreach (var stockInsumo in stockInsumos)
                    {
                        if (stockInsumo.StockDisponible < cantidadUsar)
                        {
                            _context.StockInsumoProductoIntermedios.Add(new StockInsumoProductoIntermedio()
                            {
                                Cantidad = stockInsumo.StockDisponible,
                                IdCreador = request.IdModificador,
                                InsumoProductoIntermedio = insumoProductoIntermedio,
                                IdStockInsumo = stockInsumo.IdCompraInsumo
                            });
                            cantidadUsar -= stockInsumo.StockDisponible;
                            stockInsumo.StockDisponible = 0;
                        }
                        else
                        {
                            _context.StockInsumoProductoIntermedios.Add(new StockInsumoProductoIntermedio()
                            {
                                Cantidad = cantidadUsar,
                                IdCreador = request.IdModificador,
                                InsumoProductoIntermedio = insumoProductoIntermedio,
                                IdStockInsumo = stockInsumo.IdCompraInsumo
                            });
                            stockInsumo.StockDisponible -= cantidadUsar;
                            break;
                        }
                    }
                }

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

        public async Task<IEnumerable<TablaPIRes>> ListaProductoIntermedio()
        {
            IEnumerable<TablaPIRes> response = await _context.ProductosIntermedios
            .OrderByDescending(ob => ob.Id)
            .Select(s => new TablaPIRes()
            {
                Id = s.Id,
                Registro = Alfanumerico.ConvertToBase36(s.Id),
                Lote = s.Lote,
                Codigo = s.Insumo != null ? UtilFamilia.CodigoInsumo(s.Insumo.Id) : "",
                Descripcion = s.Insumo != null ? s.Insumo.Descripcion : "",
                LoteEstandar = s.LoteEstandar ?? 0,
                Tipo = s.Tipo,
                TipoUso = s.Insumo != null ? s.Insumo.Tipo : s.TipoUso,
                Um = s.Insumo.UnidadMedida,
                FechaEmision = s.FechaEmision,
                FechaVencimiento = s.FechaVencimiento,
                Elaborado = s.Elaborador.Codigo,
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
                        .ThenInclude(i => i.CompraInsumoProductoIntermedio)
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
                    foreach (var compraInsumoProdInt in insumoProdInt.CompraInsumoProductoIntermedio.ToList())
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
                    Tipo = x.Tipo,
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
                        .Select(i => new InsumoProductoIntermedioReq
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
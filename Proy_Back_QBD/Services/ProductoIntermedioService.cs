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
        public ProductoIntermedioService(ApiContext context)
        {
            _context = context;
        }

        public async Task<int> CrearProductoIntermedio(CrearProductoIntermedioReq request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                ProductoIntermedio productoIntermedio = new ProductoIntermedioMapper().CrearProductoIntermedio(request);
                _context.ProductosIntermedios.Add(productoIntermedio);
                List<int> listaEmpaques = request.IdEmpaques;
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
                        List<CompraEmpaque> compraEmpaques = await _context.CompraEmpaques
                    .Where(w => w.IdEmpaque == conteoEmpaque.Key && w.StockDisponible > 0 && w.FechaVencimiento >= DateTimeOffset.UtcNow)
                    .OrderBy(w => w.FechaVencimiento)
                    .ToListAsync();
                        decimal stockDisponibleTotal = compraEmpaques.Sum(s => s.StockDisponible);

                        if (stockDisponibleTotal < conteoEmpaque.Value) throw new BadRequestException("Stock insuficiente");

                        if (compraEmpaques.Count() == 0) throw new NotFoundException("No hay stock disponible para este Empaque");
                        EmpaqueProductoIntermedio empaqueProductoIntermedio = new()
                        {
                            IdEmpaque = conteoEmpaque.Key,
                            ProductoIntermedio = productoIntermedio
                        };
                        _context.EmpaqueProductoIntermedios.Add(empaqueProductoIntermedio);
                        foreach (CompraEmpaque compraEmpaque in compraEmpaques)
                        {
                            CompraEmpaqueProductoIntermedio compraEmpaqueProductoIntermedio = new();
                            if (compraEmpaque.StockDisponible >= cantidadPendiente)
                            {
                                compraEmpaqueProductoIntermedio = new()
                                {
                                    Cantidad = cantidadPendiente,
                                    IdCompraEmpaque = compraEmpaque.Id,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio = empaqueProductoIntermedio
                                };

                                compraEmpaque.StockDisponible -= cantidadPendiente;
                                _context.CompraEmpaqueProductoIntermedios.Add(compraEmpaqueProductoIntermedio);
                                break;
                            }
                            else
                            {
                                compraEmpaqueProductoIntermedio = new()
                                {
                                    Cantidad = compraEmpaque.StockDisponible,
                                    IdCompraEmpaque = compraEmpaque.Id,
                                    UnidadMedida = "UND",
                                    EmpaqueProductoIntermedio = empaqueProductoIntermedio
                                };
                                cantidadPendiente -= compraEmpaque.StockDisponible;
                                compraEmpaque.StockDisponible = 0;
                                _context.CompraEmpaqueProductoIntermedios.Add(compraEmpaqueProductoIntermedio);
                            }

                        }
                        if (cantidadPendiente > 0)
                            throw new BadRequestException("No se pudo completar el consumo del empaque");
                    }
                }
                foreach (var fInsumo in request.Insumos)
                {
                    List<CompraInsumos> compraInsumos = await _context.CompraInsumos
                    .Where(w => w.IdInsumo == fInsumo.IdInsumo && w.StockDisponible > 0 && w.FechaVencimiento >= DateTimeOffset.UtcNow)
                    .OrderBy(w => w.FechaVencimiento)
                    .ToListAsync();
                    decimal stockDisponibleTotal = compraInsumos.Sum(s => s.StockDisponible);
                    decimal cantidadUsar = fInsumo.CantidadLote;
                    if (!compraInsumos.Any() || stockDisponibleTotal < cantidadUsar) throw new NotFoundException("No hay stock disponible para este insumo, " + stockDisponibleTotal);
                    InsumoProductoIntermedio insumoProductoIntermedio = new ProductoIntermedioMapper().CrearInsumosProductoIntermedio(fInsumo);
                    insumoProductoIntermedio.IdCreador = request.IdCreador;
                    insumoProductoIntermedio.ProductoIntermedio = productoIntermedio;
                    _context.InsumoProductoIntermedios.Add(insumoProductoIntermedio);


                    foreach (var compraInsumo in compraInsumos)
                    {
                        if (compraInsumo.StockDisponible < cantidadUsar)
                        {
                            compraInsumo.CompraInsumoProductoIntermedio.Add(new CompraInsumoProductoIntermedio()
                            {
                                Cantidad = compraInsumo.StockDisponible,
                                IdCreador = request.IdCreador,
                                InsumoProductoIntermedio = insumoProductoIntermedio,
                                IdCompraInsumo = compraInsumo.Id
                            });
                            cantidadUsar -= compraInsumo.StockDisponible;
                            compraInsumo.StockDisponible = 0;
                        }
                        else
                        {
                            compraInsumo.CompraInsumoProductoIntermedio.Add(new CompraInsumoProductoIntermedio()
                            {
                                Cantidad = cantidadUsar,
                                IdCreador = request.IdCreador,
                                InsumoProductoIntermedio = insumoProductoIntermedio,
                                IdCompraInsumo = compraInsumo.Id
                            });
                            compraInsumo.StockDisponible = compraInsumo.StockDisponible - cantidadUsar;
                            break;
                        }

                    }

                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return productoIntermedio.Id;
            }
            catch (NotFoundException ex)
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
            IEnumerable<ConsumoPIRes> response = await _context.CompraInsumoProductoIntermedios
            .Where(w => w.InsumoProductoIntermedio.IdProductoIntermedio == id)
            .OrderBy(ob => ob.InsumoProductoIntermedio.Variable)
            .Select(s => new ConsumoPIRes()
            {
                Codigo = UtilFamilia.CodigoInsumo(s.InsumoProductoIntermedio.IdInsumo),
                Porcentaje = s.InsumoProductoIntermedio.Porcentaje,
                Descripcion = s.InsumoProductoIntermedio.Insumo.Descripcion,
                V = s.InsumoProductoIntermedio.Variable,
                Lote = s.CompraInsumo.Lote,
                Registro = Alfanumerico.ConvertToBase36(s.IdCompraInsumo),
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

            return response;
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
                LoteEstandar = s.LoteEstandar,
                Tipo = s.Tipo,
                TipoUso = s.Insumo != null ? s.Insumo.Tipo : s.TipoUso,
                Cantidad = s.Cantidad,
                Um = s.Insumo.UnidadMedida,
                FechaEmision = s.FechaEmision,
                FechaVencimiento = s.FechaVencimiento,
                Elaborado = s.Elaborador.Codigo
            })
            .AsNoTracking()
            .ToListAsync();

            return response;
        }
    }
}
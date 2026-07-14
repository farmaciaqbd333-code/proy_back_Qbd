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
                foreach (var fInsumo in request.Insumos)
                {
                    List<CompraInsumos> compraInsumos = await _context.CompraInsumos
                    .Where(w => w.IdInsumo == fInsumo.IdInsumo && w.StockDisponible != 0 && w.FechaVencimiento >= DateTimeOffset.UtcNow)
                    .OrderBy(w => w.FechaVencimiento)
                    .ToListAsync();
                    if (!compraInsumos.Any()) throw new NotFoundException("No hay compraInsumos de este insumo");
                    decimal stockDisponibleTotal = compraInsumos.Sum(s => s.StockDisponible);
                    decimal cantidadUsar = fInsumo.CantidadLote;
                    InsumoProductoIntermedio insumoProductoIntermedio = new ProductoIntermedioMapper().CrearInsumosProductoIntermedio(fInsumo);
                    insumoProductoIntermedio.IdCreador = request.IdCreador;
                    insumoProductoIntermedio.ProductoIntermedio = productoIntermedio;
                    _context.InsumoProductoIntermedios.Add(insumoProductoIntermedio);

                    if (stockDisponibleTotal < cantidadUsar) throw new NotFoundException("No hay suficiente stock para realizar este pedido, solo se cuenta con" + stockDisponibleTotal);
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
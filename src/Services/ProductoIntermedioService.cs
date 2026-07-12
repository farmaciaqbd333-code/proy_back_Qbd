using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
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

        public Task<int> CrearProductoIntermedio(CrearProductoIntermedioReq request)
        {
            throw new NotImplementedException();
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
                Elaborado = s.Elaborado
            })
            .AsNoTracking()
            .ToListAsync();

            return response;
        }
    }
}
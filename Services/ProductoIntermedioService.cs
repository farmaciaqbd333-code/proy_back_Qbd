using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto;
using Proy_back_QBD.Interface;

namespace proy_back_Qbd.Services
{
    public class ProductoIntermedioService : IProductoIntermedioService
    {
        private readonly ApiContext _context;
        public ProductoIntermedioService(ApiContext context)
        {
            _context = context;
        }

        public Task<TablaPIRes> DetalleConsumo()
        {
            throw new NotImplementedException();
        }

        public async Task<List<TablaPIRes>> ListaProductoIntermedio()
        {
            List<TablaPIRes> response = await _context.ProductosIntermedios
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
            .ToListAsync();

            return response;
        }
    }
}
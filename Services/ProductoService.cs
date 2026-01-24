using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Services
{
    public class ProductoService : IProductoService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public ProductoService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ProductoRes>?> Obtener()
        {
            List<ProductoRes> response = await _context.Productos
            .OrderBy(obd => obd.FechaCreacion)
                                        .Select(s => new ProductoRes
                                        {
                                            Id = s.Id,
                                            Codigo = $"PT {s.Id}",
                                            Descripcion = s.Descripcion,
                                            Costo = s.Costo,
                                        })
                                        .ToListAsync();

            if (response == null)
            {
                return null;
            }
            return response;
        }
    }
}
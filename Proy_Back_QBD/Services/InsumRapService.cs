using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Services
{
    public class InsumoRService : IInsumoRService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public InsumoRService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public Task<InsumoR?> Actualizar(int id, InsumoRUpdateReq request)
        {
            throw new NotImplementedException();
        }

        public Task<InsumoR?> Crear(InsumoRCreateReq request)
        {
            throw new NotImplementedException();
        }

        public Task<InsumoR?> Eliminar(int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<InsumoRFindAllRes?>> Obtener()
        {
            throw new NotImplementedException();
        }

        public Task<InsumoRFindIdRes?> ObtenerById(int id)
        {
            throw new NotImplementedException();
        }
    }
}
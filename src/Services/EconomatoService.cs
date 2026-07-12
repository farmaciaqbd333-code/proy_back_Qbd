using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using proy_back_Qbd.Models;

namespace Proy_back_QBD.Services
{
    public class EconomatoService : IEconomatoService
    {
        private readonly ApiContext _context;

        public EconomatoService(ApiContext context)
        {
            _context = context;
        }

        public async Task<List<EconomatoRes>?> Obtener()
        {
            var response = await _context.Economatos
                .OrderBy(e => e.Descripcion)
                .Select(s => new EconomatoRes
                {
                    Id = s.Id,
                    Descripcion = s.Descripcion,
                    UnidadMedida = s.UnidadMedida
                })
                .ToListAsync();

            return response;
        }

        public async Task<EconomatoRes?> ObtenerById(int id)
        {
            var s = await _context.Economatos.FindAsync(id);
            if (s == null)
            {
                return null;
            }

            return new EconomatoRes
            {
                Id = s.Id,
                Descripcion = s.Descripcion,
                UnidadMedida = s.UnidadMedida
            };
        }

        public async Task<Economato?> Crear(EconomatoReq request)
        {
            var economato = new Economato
            {
                Descripcion = request.Descripcion,
                UnidadMedida = request.UnidadMedida
            };

            await _context.Economatos.AddAsync(economato);
            await _context.SaveChangesAsync();

            return economato;
        }

        public async Task<Economato?> Actualizar(int id, EconomatoReq request)
        {
            var economato = await _context.Economatos.FindAsync(id);
            if (economato == null)
            {
                return null;
            }

            economato.Descripcion = request.Descripcion;
            economato.UnidadMedida = request.UnidadMedida;

            await _context.SaveChangesAsync();

            return economato;
        }

        public async Task<bool> Eliminar(int id)
        {
            var economato = await _context.Economatos.FindAsync(id);
            if (economato == null)
            {
                return false;
            }

            _context.Economatos.Remove(economato);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

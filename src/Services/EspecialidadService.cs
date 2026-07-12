using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services.Interfaces;

namespace Proy_back_QBD.Services
{
    public class EspecialidadService : IEspecialidadService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public EspecialidadService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<Especialidad?> Actualizar(int id, EspecialidadUpdateReq request)
        {
            var especialidad = await _context.Especialidads.FindAsync(id);
            if (especialidad == null)
            {
                return null;  // O podrías lanzar una excepción si prefieres.
            }

            especialidad.Nombre = request.Nombre ?? especialidad.Nombre;  // Actualizar si no es nulo
            especialidad.ModificadorId = request.ModificadorId;
            await _context.SaveChangesAsync();

            return especialidad;

        }

        public async Task<Especialidad?> Crear(EspecialidadCreateReq request)
        {
            var especialidad = new Especialidad
            {
                Nombre = request.Nombre,
                CreadorId = request.CreadorId,
                ModificadorId = request.CreadorId
            };

            _context.Especialidads.Add(especialidad);
            await _context.SaveChangesAsync();
            return especialidad;
        }

        public async Task<Especialidad?> Eliminar(int id)
        {
            var especialidad = await _context.Especialidads.FindAsync(id);

            if (especialidad == null)
            {
                return null;  // O lanzar una excepción.
            }

            _context.Especialidads.Remove(especialidad);
            await _context.SaveChangesAsync();
            return especialidad;
        }

        public async Task<List<EspecialidadFindAllResponse?>> Obtener()
        {
            var especialidades = await _context.Especialidads
                                          .Select(e => new EspecialidadFindAllResponse
                                          {
                                              Id = e.Id.Value,
                                              Nombre = e.Nombre
                                          })
                                          .ToListAsync();

            return especialidades;
        }

        public async Task<EspecialidadFindIdResponse?> ObtenerById(int id)
        {
            var especialidad = await _context.Especialidads
                                       .Where(e => e.Id == id)
                                       .Select(e => new EspecialidadFindIdResponse
                                       {
                                           Id = e.Id.Value,
                                           Nombre = e.Nombre,
                                       })
                                       .FirstOrDefaultAsync();

            return especialidad;
        }
    }
}
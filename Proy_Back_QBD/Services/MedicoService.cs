using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Services
{
    public class MedicoService : IMedicoService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public MedicoService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<MedicoCreateResponse?> Crear(MedicoCreateReq request)
        {
            MedicoCreateResponse response = new MedicoCreateResponse();
            Persona persona = _mapper.Map<Persona>(request.PersonaCReq);
            persona.ModificadorId = persona.CreadorId;
            bool existe = await _context.Medicos
                .AnyAsync(p => p.Cmp == request.Cmp && p.SedeId == request.SedeId);
            if (existe)
            {
                response.Msg = "Ya existe este CMP";
                return response;
            }
            await _context.Personas.AddAsync(persona);
            await _context.SaveChangesAsync();
            Medico medico = _mapper.Map<Medico>(request);

            medico.PersonaId = persona.Id;
            medico.ModificadorId = medico.CreadorId;
            await _context.Medicos.AddAsync(medico);
            await _context.SaveChangesAsync();
            response.Msg = "Creado Exitosamente";
            response.MedicoRes = medico;
            return response;
        }

        public async Task<MedicoUpdateResponse?> Actualizar(int id, MedicoUpdateReq request)
        {
            MedicoUpdateResponse response = new MedicoUpdateResponse();
            Medico? medico = await _context.Medicos
                                            .Include(i => i.Persona)
                                            .FirstOrDefaultAsync(fod => fod.Id == id);
            if (medico == null)
            {
                response.Msg = "no se encontró";
                return response;
            }
            _mapper.Map(request, medico);
            _mapper.Map(request.PersonaCReq, medico.Persona);
            response.Msg = "Medico Actualizado";
            response.MedicoRes = medico;
            await _context.SaveChangesAsync();
            return response;
        }
        public async Task<Medico?> Eliminar(int id)
        {
            Medico? medico = await _context.Medicos
            .Include(a => a.Persona)
            .FirstOrDefaultAsync(a => a.Id == id);
            _context.Remove(medico);
            await _context.SaveChangesAsync();
            return medico;
        }

        public async Task<List<MedicoFindAllResponse?>> Obtener(int sedeId)
        {
            List<MedicoFindAllResponse>? response = await _context.Medicos
            .Include(a => a.Especialidad)
            .Include(a => a.Persona)
            .Where(w => w.SedeId == sedeId)
            .Select(a => new MedicoFindAllResponse
            {
                Id = a.Id,
                DesEspecialidad = a.Especialidad.Nombre,
                EspecialidadId = a.Especialidad.Id,
                NumeroEspecialidad = a.NumeroEspecialidad,
                Persona = _mapper.Map<PersMedRes2>(a.Persona),
                NombreCompleto = $"{a.Persona.NombreCompleto} ",
                Cmp = a.Cmp
            })
            .ToListAsync();

            if (response == null)
            {
                return null;
            }
            return response;
        }

        public async Task<MedicoFindIdResponse?> ObtenerById(int id)
        {
            MedicoFindIdResponse? response = await _context.Medicos
            .Include(a => a.Especialidad)
            .Include(a => a.Persona)
            .Where(a => a.Id == id)
            .Select(a => new MedicoFindIdResponse
            {
                Id = a.Id,
                EspecialidadId = a.EspecialidadId,
                NumeroEspecialidad = a.NumeroEspecialidad,
                PersonaFk = _mapper.Map<PersMedRes>(a.Persona),
                Cmp = a.Cmp,
            })
            .FirstAsync();

            if (response == null)
            {
                return null;
            }
            return response;
        }
    }
}
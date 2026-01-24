using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Empaque;
using Proy_back_QBD.Models;


namespace Proy_back_QBD.Services
{
    public class EmpaqueService : IEmpaqueService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public EmpaqueService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Empaque?> Actualizar(int id, EmpaqueUpdateReq request)
        {
            Empaque? empaque = await _context.Empaques.FindAsync(id);
            if (empaque == null)
            {
                return null;
            }
            _mapper.Map(request, empaque);
            await _context.SaveChangesAsync();
            return empaque;
        }

        public async Task<Empaque?> Crear(EmpaqueCreateReq request)
        {
            Empaque? empaque = _mapper.Map<Empaque>(request);
            empaque.ModificadorId = empaque.CreadorId;
            _context.Empaques.Add(empaque);
            await _context.SaveChangesAsync();
            return empaque;
        }

        public async Task<Empaque?> Eliminar(int id)
        {
            Empaque? empaque = await _context.Empaques.FindAsync(id);
            if (empaque == null)
            {
                return null;
            }

            _context.Remove(empaque);
            await _context.SaveChangesAsync();
            return empaque;
        }

        public async Task<List<EmpaqueFindAllRes?>> Obtener()
        {
            List<EmpaqueFindAllRes?> response = await _context.Empaques
                                            .Include(i => i.Funda)
                                            .Include(i => i.Caja)
                                            .Include(i => i.Etiqueta1)
                                            .Include(i => i.Etiqueta2)
                                            .OrderBy(obd => obd.FechaCreacion)
                                            .Select(s => new EmpaqueFindAllRes
                                            {
                                                Id = s.Id,
                                                Descripcion = s.Descripcion,
                                                Funda = s.Funda.Descripcion,
                                                Caja = s.Caja.Descripcion,
                                                Etiqueta1 = s.Etiqueta1.Descripcion,
                                                Etiqueta2 = s.Etiqueta2.Descripcion,
                                                Tara = s.Tara
                                            }
                                            )
                                            .ToListAsync();
            if (response == null)
            {
                return null;
            }
            return response;
        }

        public async Task<EmpaqueFindIdRes?> ObtenerById(int id)
        {
            EmpaqueFindIdRes response = await _context.Empaques
                                            .Include(i => i.Funda)
                                            .Include(i => i.Caja)
                                            .Select(s => new EmpaqueFindIdRes
                                            {
                                                Id = s.Id,
                                                Descripcion = s.Descripcion,
                                                Funda = s.Funda.Descripcion,
                                                Caja = s.Caja.Descripcion,
                                                Etiqueta1 = s.Etiqueta1.Descripcion,
                                                Etiqueta2 = s.Etiqueta2.Descripcion,
                                                Tara = s.Tara
                                            }
                                            ).FirstOrDefaultAsync(fod => fod.Id == id);
            if (response == null)
            {
                return null;
            }
            return response;
        }
    }
}
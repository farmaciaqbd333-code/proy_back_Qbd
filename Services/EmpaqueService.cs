using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
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

        public async Task<List<EmpaqueFindAllRes>> Obtener()
        {
            List<EmpaqueFindAllRes> response = await _context.Empaques
                                            .OrderBy(obd => obd.FechaCreacion)
                                            .Select(s => new EmpaqueFindAllRes
                                            {
                                                Id = s.Id,
                                                Descripcion = s.Descripcion,
                                                FundaId = s.IdFunda,
                                                Funda = s.Funda != null ? s.Funda.Codigo : "",
                                                CajaId = s.IdCaja,
                                                Caja = s.Caja != null ? s.Caja.Codigo : "",
                                                EtiquetaId1 = s.IdEtiqueta1,
                                                Etiqueta1 = s.Etiqueta1 != null ? s.Etiqueta1.Codigo : "",
                                                EtiquetaId2 = s.IdEtiqueta2,
                                                Etiqueta2 = s.Etiqueta2 != null ? s.Etiqueta2.Codigo : "",
                                                Codigo = s.Codigo,
                                                Costo = s.Costo,
                                                Tara = s.Tara,
                                                FamiliaId = s.IdFamilia
                                            }
                                            )
                                            .ToListAsync();
            if (!response.Any()) throw new NotFoundException("No hay empaues");
            return response;
        }

        public async Task<EmpaqueFindIdRes?> ObtenerById(int id)
        {
            EmpaqueFindIdRes? response = await _context.Empaques
                                            .Include(i => i.Funda)
                                            .Include(i => i.Caja)
                                            .Select(s => new EmpaqueFindIdRes
                                            {
                                                Id = s.Id,
                                                Descripcion = s.Descripcion,
                                                Funda = s.Funda != null ? s.Funda.Codigo : "",
                                                FundaId = s.IdFunda,
                                                Caja = s.Caja != null ? s.Caja.Codigo : "",
                                                CajaId = s.IdCaja,
                                                Etiqueta1 = s.Etiqueta1 != null ? s.Etiqueta1.Codigo : "",
                                                EtiquetaId1 = s.IdEtiqueta1,
                                                Etiqueta2 = s.Etiqueta2 != null ? s.Etiqueta2.Codigo : "",
                                                EtiquetaId2 = s.IdEtiqueta2,
                                                Codigo = s.Codigo,
                                                Costo = s.Costo,
                                                Tara = s.Tara,
                                                FamiliaId = s.IdFamilia
                                            }
                                            ).FirstOrDefaultAsync(fod => fod.Id == id);
            if (response == null) throw new NotFoundException("No se encontro el empaque");
            return response;
        }
    }
}
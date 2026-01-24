using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Insumo;
using Proy_back_QBD.Models;


namespace Proy_back_QBD.Services
{
    public class InsumoService : IInsumoService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public InsumoService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Insumo?> Actualizar(int id, InsumoUpdateReq request)
        {
            Insumo? insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null)
            {
                return null;
            }
            _mapper.Map(request, insumo);
            await _context.SaveChangesAsync();
            return insumo;
        }

        public async Task<Insumo?> Crear(InsumoCreateReq request)
        {
            Insumo? insumo = _mapper.Map<Insumo>(request);
            insumo.ModificadorId = insumo.CreadorId;
            _context.Insumos.Add(insumo);
            await _context.SaveChangesAsync();
            return insumo;
        }

        public async Task<Insumo?> Eliminar(int id)
        {
            Insumo? insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null)
            {
                return null;
            }

            _context.Remove(insumo);
            await _context.SaveChangesAsync();
            return insumo;
        }

        public async Task<List<InsumoLabRes>?> ListaFormulaR(int FormulaRId)
        {
            List<InsumoLabRes> response = await _context.InsumosR
                                                        .Where(w => w.FormulaRId == FormulaRId)
                                                        .OrderBy(obd => obd.FechaCreacion)
                                                        .Select(s => new InsumoLabRes
                                                        {
                                                            Id = s.InsumoId,
                                                            Descripcion = s.Insumo.Descripcion,
                                                            FactorCorreccion = s.Insumo.FactorCorreccion,
                                                            Dilucion = s.Insumo.Dilucion,
                                                            UnidadMedida = s.Insumo.UnidadMedida
                                                        })
                                                        .ToListAsync();
            return response;
        }
        public async Task<List<InsumoLabRes>?> ListaInsLab(int FormulaRId)
        {
            List<InsumoLabRes> response = await _context.InsumosR
                                                        .Where(w => w.FormulaRId == FormulaRId)
                                                        .OrderBy(obd => obd.FechaCreacion)
                                                        .Select(s => new InsumoLabRes
                                                        {
                                                            Id = s.InsumoId,
                                                            Descripcion = s.Insumo.Descripcion,
                                                            FactorCorreccion = s.Insumo.FactorCorreccion,
                                                            Dilucion = s.Insumo.Dilucion,
                                                            UnidadMedida = s.Insumo.UnidadMedida
                                                        })
                                                        .ToListAsync();
            return response;
        }

        public async Task<List<InsumoFindAllRes?>> Obtener()
        {
            List<InsumoFindAllRes?> response = await _context.Insumos
                                            .OrderBy(obd => obd.FechaCreacion)
                                            .Select(s => new InsumoFindAllRes
                                            {
                                                Id = s.Id,
                                                Descripcion = s.Descripcion,
                                                FactorCorreccion = s.FactorCorreccion,
                                                Dilucion = s.Dilucion,
                                                UnidadMedida = s.UnidadMedida
                                            }
                                            ).ToListAsync();
            if (response == null)
            {
                return null;
            }
            return response;
        }

        public async Task<InsumoFindIdRes?> ObtenerById(int id)
        {
            InsumoFindIdRes response = await _context.Insumos
                                            .OrderBy(obd => obd.FechaCreacion)
                                            .Select(s => new InsumoFindIdRes
                                            {
                                                Id = s.Id,
                                                Descripcion = s.Descripcion,
                                                UnidadMedida = s.UnidadMedida,
                                                FactorCorreccion = s.FactorCorreccion,
                                                Dilucion = s.Dilucion
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
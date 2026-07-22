using AutoMapper;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Dto.Insumo;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Services
{
    public class FormulaRService : IFormulaRService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public FormulaRService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<string> Crear(FormulaRCreReq request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Fórmula
                FormulaR formulaR = _mapper.Map<FormulaR>(request.FormulaR);
                formulaR.Clasificacion = "FORMULA";
                formulaR.ModificadorId = formulaR.CreadorId;

                await _context.FormulasR.AddAsync(formulaR);
                await _context.SaveChangesAsync();

                // Insumos
                foreach (var item in request.InsumosR)
                {
                    InsumoR insumoR = _mapper.Map<InsumoR>(item);
                    insumoR.FormulaRId = formulaR.Id;

                    await _context.InsumosR.AddAsync(insumoR);
                }

                FormulaRapidaSede formulaRapidaSede = new()
                {
                    IdSede = request.FormulaR.IdSede.Value,
                    IdFormular = formulaR.Id
                };

                await _context.FormulaRSedes.AddAsync(formulaRapidaSede);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return "Registro Exitoso";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"Error: {ex.Message}";
            }
        }
        public async Task<string> Actualizar(int id, FormulaRUpdReq request)
        {
            try
            {
                // Buscar la formulaR existente
                var formulaR = await _context.FormulasR
                    .Include(i => i.InsumoR) // Suponiendo que el Id está en la solicitud
                    .FirstOrDefaultAsync(f => f.Id == id); // Suponiendo que el Id está en la solicitud

                if (formulaR == null)
                {
                    return "La fórmula no existe.";
                }

                // Actualizamos las propiedades de FormulaR
                _mapper.Map(request.FormulaR, formulaR);  // Esto actualiza las propiedades de formulaR con los datos de request            

                // Guardamos los cambios

                foreach (var insumoRequest in request.InsumosR)
                {
                    var insumoExistente = formulaR.InsumoR.FirstOrDefault(i => i.InsumoId == insumoRequest.InsumoId);
                    if (insumoExistente != null)
                    {
                        _mapper.Map(insumoRequest, insumoExistente);
                    }
                }
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return "Actualización exitosa";
                }
                else
                {
                    return "No se pudo actualizar la fórmula. Intente nuevamente.";
                }
            }
            catch (Exception ex)
            {
                // Log de error
                // Aquí podrías usar un logger para registrar el error
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> Eliminar(int formulaRId)
        {
            try
            {
                // Buscar la fórmula y sus insumos asociados
                var formulaR = await _context.FormulasR
                    .Include(f => f.InsumoR) // Cargar los insumos relacionados
                    .FirstOrDefaultAsync(f => f.Id == formulaRId);

                if (formulaR == null)
                {
                    return "La fórmula no existe.";
                }

                // Eliminar insumos relacionados (si no hay eliminación en cascada)
                if (formulaR.InsumoR != null && formulaR.InsumoR.Any())
                {
                    _context.InsumosR.RemoveRange(formulaR.InsumoR);
                }

                // Eliminar la fórmula
                _context.FormulasR.Remove(formulaR);

                // Guardar cambios
                var result = await _context.SaveChangesAsync();

                return result > 0
                    ? "Eliminación exitosa"
                    : "No se pudo eliminar la fórmula. Intente nuevamente.";
            }
            catch (Exception ex)
            {
                // Aquí podrías usar un logger para registrar el error
                return $"Error: {ex.Message}";
            }
        }


        public async Task<List<FormulaRRes>?> Listar(int idSede, string clasificacion)
        {
            List<int> idFormulasR = await _context.FormulaRSedes
            .Where(w => w.IdSede == idSede)
            .Select(s => s.IdFormular).ToListAsync();

            List<FormulaRRes> response = await _context.FormulasR
            .Where(w => w.Clasificacion == clasificacion)
                                                        .OrderBy(obd => obd.FechaCreacion)
                                                        .Select(s => new FormulaRRes
                                                        {
                                                            Id = s.Id,
                                                            Descripcion = s.Descripcion,
                                                            EmpaqueId = s.EmpaqueId,
                                                            Procedimiento = s.Procedimiento,
                                                            Aspecto = s.Aspecto,
                                                            Color = s.Color,
                                                            Olor = s.Olor,
                                                            Ph = s.Ph,
                                                            Insumos = s.InsumoR
                                                            .OrderBy(obd => obd.FechaCreacion)
                                                            .Select(i => new InsumoFormR
                                                            {
                                                                Id = i.InsumoId,
                                                                Codigo = "MP-QbD-" + i.InsumoId,
                                                                Porcentaje = i.Porcentaje,
                                                                Descripcion = i.Insumo.Descripcion,
                                                                UnidadMedida = i.Insumo.UnidadMedida,
                                                                FactorCorreccion = i.Insumo.FactorCorreccion,
                                                                Dilucion = i.Insumo.Dilucion,
                                                                Cantidad = i.Cantidad * 1000
                                                            }).ToList()
                                                        })
                                                        .ToListAsync();
            return response;
        }

        public async Task<string> ActualizarSedes(FormulaRapidaSedeUpdReq request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var actuales = await _context.FormulaRSedes
                    .Where(x => x.IdFormular == request.IdFormular)
                    .ToListAsync();

                // Eliminar relaciones que ya no existen
                var eliminar = actuales
                    .Where(x => !request.IdsSede.Contains(x.IdSede))
                    .ToList();

                if (eliminar.Any())
                    _context.FormulaRSedes.RemoveRange(eliminar);

                // Agregar nuevas relaciones
                var existentes = actuales
                    .Select(x => x.IdSede)
                    .ToHashSet();

                var agregar = request.IdsSede
                    .Where(id => !existentes.Contains(id))
                    .Select(id => new FormulaRapidaSede
                    {
                        IdFormular = request.IdFormular,
                        IdSede = id
                    });

                await _context.FormulaRSedes.AddRangeAsync(agregar);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return "Registro actualizado correctamente.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return $"Error: {ex.Message}";
            }
        }
    }
}
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
                FormulaRapida formulaR = _mapper.Map<FormulaRapida>(request.FormulaR);
                if (formulaR.Cantidad != null)
                {
                    formulaR.Cantidad = formulaR.Cantidad / 1000;
                }
                formulaR.IdEmpaque = request.FormulaR.IdEmpaque ?? request.FormulaR.EmpaqueId;
                if (request.FormulaR.IdInsumo.HasValue && request.FormulaR.IdInsumo.Value > 0)
                    formulaR.IdInsumo = request.FormulaR.IdInsumo.Value;
                formulaR.ModificadorId = formulaR.CreadorId;
                formulaR.FechaCreacion = DateTime.Now;
                formulaR.FechaModificacion = DateTime.Now;

                await _context.FormulasR.AddAsync(formulaR);
                await _context.SaveChangesAsync();

                // Insumos
                if (request.InsumosR != null && request.InsumosR.Any())
                {
                    foreach (var item in request.InsumosR)
                    {
                        InsumoR insumoR = _mapper.Map<InsumoR>(item);
                        insumoR.FormulaRId = formulaR.Id;
                        insumoR.FechaCreacion = DateTime.Now;
                        insumoR.Cantidad = item.Cantidad / 1000;
                        insumoR.FechaModificacion = DateTime.Now;

                        await _context.InsumosR.AddAsync(insumoR);
                    }
                }

                if (request.FormulaR.IdSede.HasValue && request.FormulaR.IdSede.Value > 0)
                {
                    FormulaRapidaSede formulaRapidaSede = new()
                    {
                        IdSede = request.FormulaR.IdSede.Value,
                        IdFormulaRapida = formulaR.Id
                    };

                    await _context.FormulaRSedes.AddAsync(formulaRapidaSede);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return "Registro Exitoso";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return $"Error: {ex.Message} -> {innerMsg}";
            }
        }
        public async Task<string> Actualizar(int id, FormulaRUpdReq request)
        {
            try
            {
                // Buscar la formulaR existente
                var formulaR = await _context.FormulasR
                    .Include(i => i.InsumoR)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (formulaR == null)
                {
                    return "La fórmula no existe.";
                }

                // Actualizamos las propiedades de FormulaR
                _mapper.Map(request.FormulaR, formulaR);
                formulaR.FechaModificacion = DateTime.Now;
                formulaR.Cantidad = request.FormulaR.Cantidad / 1000;

                var empaqueVal = request.FormulaR.IdEmpaque ?? request.FormulaR.EmpaqueId;
                if (empaqueVal.HasValue && empaqueVal.Value > 0)
                {
                    formulaR.IdEmpaque = empaqueVal.Value;
                }
                else
                {
                    formulaR.IdEmpaque = null;
                }

                if (request.FormulaR.IdInsumo.HasValue && request.FormulaR.IdInsumo.Value > 0)
                {
                    formulaR.IdInsumo = request.FormulaR.IdInsumo.Value;
                }
                else
                {
                    formulaR.IdInsumo = null;
                }

                // Reemplazar la lista de insumos: eliminar anteriores e insertar la lista recibida
                if (formulaR.InsumoR != null && formulaR.InsumoR.Any())
                {
                    _context.InsumosR.RemoveRange(formulaR.InsumoR);
                }

                if (request.InsumosR != null && request.InsumosR.Any())
                {
                    foreach (var insumoReq in request.InsumosR)
                    {
                        if (insumoReq.InsumoId > 0)
                        {
                            InsumoR nuevoInsumo = new()
                            {
                                FormulaRId = formulaR.Id,
                                InsumoId = insumoReq.InsumoId,
                                Porcentaje = insumoReq.Porcentaje,
                                Cantidad = insumoReq.Cantidad / 1000,
                                CreadorId = insumoReq.CreadorId ?? insumoReq.ModificadorId ?? formulaR.CreadorId,
                                ModificadorId = insumoReq.ModificadorId ?? formulaR.CreadorId,
                                FechaCreacion = DateTime.Now,
                                FechaModificacion = DateTime.Now
                            };
                            await _context.InsumosR.AddAsync(nuevoInsumo);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return "Actualización exitosa";
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return $"Error: {ex.Message} -> {innerMsg}";
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
                .Select(s => s.IdFormulaRapida).ToListAsync();

            List<int> formulasConSede = await _context.FormulaRSedes
                .Select(s => s.IdFormulaRapida).Distinct().ToListAsync();

            var query = _context.FormulasR.AsQueryable();

            if (idSede > 0)
            {
                query = query.Where(w => idFormulasR.Contains(w.Id) || !formulasConSede.Contains(w.Id));
            }

            if (!string.IsNullOrEmpty(clasificacion) && clasificacion.ToUpper() != "TODAS")
            {
                query = query.Where(w => w.Clasificacion == clasificacion);
            }

            List<FormulaRRes> response = await query
                .OrderByDescending(obd => obd.Id)
                .Select(s => new FormulaRRes
                {
                    Id = s.Id,
                    Descripcion = s.Descripcion,
                    IdEmpaque = s.IdEmpaque,
                    IdInsumo = s.IdInsumo,
                    Procedimiento = s.Procedimiento,
                    Clasificacion = s.Clasificacion,
                    Tipo = s.Insumo.Tipo,
                    FormaF = s.Insumo.FormaFarmaceutica,
                    Cantidad = s.Cantidad*1000,
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
                    .Where(x => x.IdFormulaRapida == request.IdFormulaRapida)
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
                        IdFormulaRapida = request.IdFormulaRapida,
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
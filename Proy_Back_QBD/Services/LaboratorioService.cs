using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services.Interfaces;

namespace Proy_back_QBD.Services
{
    public class LaboratorioService(
        ApiContext db,
        IMapper Mappers,
        ILaboratorioRepository repositoryLaboritorio,
        IUnitOfWork repositoryUnitWork) : ILaboratorioService
    {
        private readonly ApiContext _context = db;
        private readonly ILaboratorioRepository _repositoryLaboritorio = repositoryLaboritorio;
        private readonly IUnitOfWork _repositoryUnitWork = repositoryUnitWork;
        private readonly IMapper _Mappers = Mappers;

        public async Task<string> EditarElaborado(int labId, int sedeId, int idElaborado)
        {
            Laboratorio? lab = await _context.Laboratorios
            .Where(w => w.Id == labId && w.SedeId == sedeId)
            .FirstOrDefaultAsync()
            ;
            if (lab == null)
            {
                return "no encontrado";
            }
            lab.Elaborado = idElaborado;

            await _context.SaveChangesAsync();

            return "Cambio Exitoso";
        }

        public async Task<List<PedidoLab>> ListaLab(int sedeId)
        {

            var pedidosLab = await _context.Laboratorios
                                        .Include(i => i.Formula.Pedido.Paciente.Persona)
                                        .Include(i => i.Formula.Laboratorio.ElaboradoU.Persona)
                                        .Where(w => w.Formula.Pedido.SedeId == sedeId)
                                        .OrderByDescending(w => w.FechaCreacion)
                                        .Select(s => new PedidoLab
                                        {
                                            LabId = s.Id,
                                            PedidoId = s.Formula.PedidoId,
                                            Fecha = s.FechaCreacion,
                                            DNI = s.Formula.Pedido.Paciente.Persona.Dni ?? s.Formula.Pedido.Paciente.DniApoderado,
                                            Paciente = s.Formula.Pedido.Paciente.Persona.NombreCompleto,
                                            FormulaMagistral = s.Formula.FormulaMagistral,
                                            Registro = "REG-" + s.Id,
                                            Elaborado = s.ElaboradoU.Persona.NombreCompleto,
                                        })
                                        .ToListAsync();

            return pedidosLab;
        }

        public async Task<LabFindPedIdRes?> ObtenerByCod(string cod, int sedeId)
        {
            int id = 0;
            var partes = cod.Substring(1);

            id = int.Parse(partes);


            LabFindPedIdRes? response = await _context.Pedidos
                                        .Include(i => i.Paciente.Persona)
                                        .Include(i => i.Medico.Persona)
                                        .Where(w => w.Id == id && w.SedeId == sedeId)
                                        .Select(s => new LabFindPedIdRes
                                        {
                                            DNI = s.Paciente.Persona.Dni ?? s.Paciente.DniApoderado,
                                            Paciente = s.Paciente.Persona.NombreCompleto,
                                            Edad = PacienteService.CalcularEdad(s.Paciente.Persona.FechaNacimiento).ToString(),
                                            CMP = s.Medico.Cmp,
                                            Medico = s.Medico.Persona.NombreCompleto,
                                            Formulas = s.Formulas
                                            .Where(w => w.PedidoId == id && w.SedeId == sedeId)
                                            .Select(f => new LabForm
                                            {
                                                Id = f.Id,
                                                FormulaM = f.FormulaMagistral,
                                                FormaF = f.FormaFarmaceutica,
                                                Lote = f.Lote,
                                                Registro = "REG-" + f.Id,
                                                Cantidad = f.Cantidad,
                                                GPorMl = f.GPorMl,
                                                UnidadMedida = f.UnidadMedida,
                                                Diagnostico = f.Diagnostico,
                                                ZonaAplicacion = f.ZonaAplicacion,
                                                CostoTotal = f.Cantidad * f.Costo,

                                            }).ToList()
                                        }
                                        )
                                        .FirstOrDefaultAsync();

            if (response == null)
            {
                return null;
            }
            return response;
        }

        public async Task<string?> RegistrarLabIns(FormLabIns request)
        {

            await _repositoryLaboritorio.ExisteLaboratorio(
                request.Lab.FormulaId,
                request.Lab.SedeId);

            var laboratorio = _Mappers.Map<Laboratorio>(request.Lab);

            laboratorio.ModificadorId = laboratorio.CreadorId;

            foreach (var item in request.Ins)
            {
                var formulaCC = _Mappers.Map<FormulaCC>(item);

                formulaCC.FormulaId = request.Lab.FormulaId;
                formulaCC.SedeId = request.Lab.SedeId;
                formulaCC.ModificadorId = formulaCC.CreadorId;

                await _repositoryLaboritorio.RegistrarFormulaCC(formulaCC);
            }

            await _repositoryLaboritorio.RegistrarLaboratorio(laboratorio);
            await _repositoryUnitWork.GuardarCambios();

            return "Registro exitoso";


        }

        public async Task<string?> EditarLabIns(FormLabIns request)
        {
            // Buscar el laboratorio existente
            Laboratorio? laboratorio = await _context.Laboratorios
                .FirstOrDefaultAsync(w =>
                    w.Id == request.Lab.FormulaId &&
                    w.SedeId == request.Lab.SedeId);

            if (laboratorio == null)
            {
                return null;
            }

            // Actualizar datos del laboratorio
            _Mappers.Map(request.Lab, laboratorio);

            laboratorio.ModificadorId = request.Lab.CreadorId;

            // Buscar las fórmulas asociadas
            List<FormulaCC> formulasExistentes = await _context.FormulasCC
                .Where(w =>
                    w.FormulaId == request.Lab.FormulaId &&
                    w.SedeId == request.Lab.SedeId)
                .ToListAsync();

            // Eliminar las fórmulas anteriores
            _context.FormulasCC.RemoveRange(formulasExistentes);

            // Agregar las nuevas fórmulas
            foreach (var item in request.Ins)
            {
                FormulaCC formulaCC = _Mappers.Map<FormulaCC>(item);

                formulaCC.FormulaId = request.Lab.FormulaId;
                formulaCC.SedeId = request.Lab.SedeId;
                formulaCC.ModificadorId = item.CreadorId;

                _context.FormulasCC.Add(formulaCC);
            }

            await _context.SaveChangesAsync();

            return "Registro actualizado exitosamente";
        }
    }
}
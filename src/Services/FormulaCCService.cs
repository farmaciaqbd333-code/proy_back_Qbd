using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Services
{
    public class FormulaCCService : IFormulaCCService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public FormulaCCService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<string>? Actualizar(int formulaId, int sedeId, FormulaCCUpdReqP request)
        {

            bool duplicates = request.FormulaCCs
    .GroupBy(x => x.Variable)
    .Where(g => g.Count() > 1)
    .Select(g => new
    {
        Variable = g.Key
    })
    .Any();

            if (duplicates)
            {
                return "Variable Duplicada";
            }


            List<FormulaCC?>? formulasCC = await _context.FormulasCC
            .Where(w => w.FormulaId == formulaId && w.SedeId == sedeId)
            .ToListAsync();

            if (formulasCC != null || formulasCC.Count() > 0)
            {
                _context.FormulasCC.RemoveRange(formulasCC);
                await _context.SaveChangesAsync();
            }


            foreach (var formula in request.FormulaCCs)
            {
                FormulaCC formulaM = _mapper.Map<FormulaCC>(formula);
                formulaM.FormulaId = formulaId;
                formulaM.SedeId = sedeId;
                _context.FormulasCC.Add(formulaM);
            }

            Laboratorio? laboratorio = await _context.Laboratorios.FirstOrDefaultAsync(foda => foda.Id == formulaId && foda.SedeId == sedeId);
            if (laboratorio == null)
                return null;

            laboratorio.Procedimiento = request.Procedimiento;
            laboratorio.EmpaqueId = request.EmpaqueId;

            await _context.SaveChangesAsync();
            return "Cambio Exitoso";
        }

        public async Task<List<RecetaRes>?> ListarInsumos(int sedeId)
        {
            List<RecetaRes> response = await _context.FormulasCC
            .Select(s => new RecetaRes
            {

            })
            .ToListAsync();

            if (response == null)
            {
                return null;
            }

            return response;
        }

        public async Task<FormulaCCLabRes>? ListarInsumosLab(int formulaId, int sedeId)
        {
            FormulaCCLabRes? response = await _context.FormulasCC
            .Include(i => i.Formula.Pedido.Paciente.Persona)
            .Include(i => i.Formula.Pedido.Medico.Persona)
            .Include(i => i.Formula.Laboratorio)
            .Include(i => i.Insumo)
            .Where(w => w.FormulaId == formulaId && w.SedeId == sedeId)
            .Select(s => new FormulaCCLabRes
            {
                CodigoPedido = "P-" + s.Formula.PedidoId,
                DniPaciente = s.Formula.Pedido.Paciente.Persona.Dni ?? s.Formula.Pedido.Paciente.DniApoderado,
                NombreCompleto = s.Formula.Pedido.Paciente.Persona.NombreCompleto,
                EdadPaciente = PacienteService.CalcularEdad(s.Formula.Pedido.Paciente.Persona.FechaNacimiento),
                CMP = s.Formula.Pedido.Medico.Cmp,
                NombreCompletoMed = s.Formula.Pedido.Medico.Persona.NombreCompleto,
                FormulaId = s.Formula.Id,
                FormulaMagistral = s.Formula.FormulaMagistral,
                FormaFarmaceutica = s.Formula.FormaFarmaceutica,
                Lote = s.Formula.Lote,
                FechaEmision = s.Formula.Laboratorio.FechaEmision,
                FechaVcto = s.Formula.Laboratorio.FechaVcto,
                NroReg = "REG-" + s.Formula.Id,
                Cantidad = s.Formula.Cantidad,
                GPorMl = s.Formula.GPorMl,
                Elaborado = s.Formula.Laboratorio.Elaborado,
                Autorizado = s.Formula.Laboratorio.Autorizado,
                UnidadMedida = s.Formula.UnidadMedida,
                CostoTotal = s.Formula.Costo,
                EmpaqueId = s.Formula.Laboratorio.EmpaqueId,
                Procedimiento = s.Formula.Laboratorio.Procedimiento,
                Diagnostico = s.Formula.Diagnostico,
                ZonaAplicacion = s.Formula.ZonaAplicacion
            })
            .FirstOrDefaultAsync();
            if (response == null)
            {
                return null;
            }
            List<FormulaCCLabSubRes>? response2 = await _context.FormulasCC
            .Include(i => i.Insumo)
            .Where(w => w.FormulaId == formulaId && w.SedeId == sedeId)
            .OrderBy(ob => ob.Variable)
            .Select(s => new FormulaCCLabSubRes
            {
                InsumoId = s.InsumoId,
                Porcentaje = s.Porcentaje.ToString(),
                Variable = s.Variable,
                Practica = s.Practica.ToString(),
                CSP = s.CSP
            }).ToListAsync();

            if (response2 == null)
            {
                return null;
            }
            response.insumos = response2;
            return response;
        }
    }
}
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

        public async Task<string?> Actualizar(int formulaId, int sedeId, FormulaCCUpdReqP request)
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

            List<FormulaCC> formulasCC = await _context.FormulasCC
                .Where(w => w.FormulaId == formulaId && w.SedeId == sedeId)
                .ToListAsync();

            if (formulasCC != null && formulasCC.Count > 0)
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
            if (laboratorio != null)
            {
                laboratorio.Procedimiento = request.Procedimiento;
                laboratorio.EmpaqueId = request.EmpaqueId;
            }

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

        public async Task<FormulaCCLabRes?> ListarInsumosLab(int formulaId, int sedeId)
        {
            FormulaCCLabRes? response = await _context.Formulas
                .Include(i => i.Pedido.Paciente.Persona)
                .Include(i => i.Pedido.Medico.Persona)
                .Include(i => i.Laboratorio)
                .Where(w => w.Id == formulaId && (w.SedeId == sedeId || w.SedeId == null))
                .Select(s => new FormulaCCLabRes
                {
                    CodigoPedido = s.PedidoId != null ? "P-" + s.PedidoId : "",
                    DniPaciente = s.Pedido != null && s.Pedido.Paciente != null
                        ? (s.Pedido.Paciente.Persona != null ? s.Pedido.Paciente.Persona.Dni : null) ?? s.Pedido.Paciente.DniApoderado ?? ""
                        : "",
                    NombreCompleto = s.Pedido != null && s.Pedido.Paciente != null && s.Pedido.Paciente.Persona != null
                        ? s.Pedido.Paciente.Persona.NombreCompleto ?? ""
                        : "",
                    EdadPaciente = s.Pedido != null && s.Pedido.Paciente != null && s.Pedido.Paciente.Persona != null
                        ? PacienteService.CalcularEdad(s.Pedido.Paciente.Persona.FechaNacimiento)
                        : "",
                    CMP = s.Pedido != null && s.Pedido.Medico != null ? s.Pedido.Medico.Cmp ?? "" : "",
                    NombreCompletoMed = s.Pedido != null && s.Pedido.Medico != null && s.Pedido.Medico.Persona != null
                        ? s.Pedido.Medico.Persona.NombreCompleto ?? ""
                        : "",
                    FormulaId = s.Id,
                    FormulaMagistral = s.FormulaMagistral ?? "",
                    FormaFarmaceutica = s.FormaFarmaceutica ?? "",
                    Lote = s.Lote ?? "",
                    FechaEmision = s.Laboratorio != null ? s.Laboratorio.FechaEmision : DateOnly.FromDateTime(DateTime.Today),
                    FechaVcto = s.Laboratorio != null ? s.Laboratorio.FechaVcto : DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
                    NroReg = "REG-" + s.Id,
                    Cantidad = s.Cantidad,
                    GPorMl = s.GPorMl ?? "",
                    Elaborado = s.Laboratorio != null ? s.Laboratorio.Elaborado : null,
                    Autorizado = s.Laboratorio != null ? s.Laboratorio.Autorizado : null,
                    UnidadMedida = s.UnidadMedida ?? "",
                    CostoTotal = s.Costo,
                    EmpaqueId = s.Laboratorio != null ? s.Laboratorio.EmpaqueId : null,
                    Procedimiento = s.Laboratorio != null ? s.Laboratorio.Procedimiento : null,
                    Diagnostico = s.Diagnostico ?? "",
                    ZonaAplicacion = s.ZonaAplicacion ?? "",
                    insumos = new List<FormulaCCLabSubRes>()
                })
                .FirstOrDefaultAsync();

            if (response == null)
            {
                return null;
            }

            List<FormulaCCLabSubRes> response2 = await _context.FormulasCC
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

            response.insumos = response2 ?? new List<FormulaCCLabSubRes>();
            return response;
        }
    }
}
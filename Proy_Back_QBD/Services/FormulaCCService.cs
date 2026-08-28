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
        private readonly ILogger<FormulaCCService> _logger;
        public FormulaCCService(
        ApiContext context, 
        IMapper mapper,
        ILogger<FormulaCCService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<string?> Actualizar(
    int formulaId,
    int sedeId,
    FormulaCCUpdReqP request)
        {
            try
            {
                _logger.LogInformation(
                    "Iniciando Actualizar. FormulaId: {FormulaId}, SedeId: {SedeId}",
                    formulaId,
                    sedeId);

                // Validar request
                if (request == null)
                {
                    _logger.LogWarning("El request es null.");
                    return "Request inválido";
                }

                if (request.FormulaCCs == null)
                {
                    _logger.LogWarning("FormulaCCs es null.");
                    return "FormulaCCs inválido";
                }

                _logger.LogInformation(
                    "Cantidad de FormulaCC recibidas: {Cantidad}",
                    request.FormulaCCs.Count());

                // Validar variables duplicadas
                bool duplicates = request.FormulaCCs
                    .GroupBy(x => x.Variable)
                    .Any(g => g.Count() > 1);

                _logger.LogInformation(
                    "¿Existen variables duplicadas?: {Duplicates}",
                    duplicates);

                if (duplicates)
                {
                    _logger.LogWarning(
                        "Se encontraron variables duplicadas para FormulaId: {FormulaId}",
                        formulaId);

                    return "Variable Duplicada";
                }

                // Buscar registros existentes
                _logger.LogInformation(
                    "Buscando FormulaCC existentes. FormulaId: {FormulaId}, SedeId: {SedeId}",
                    formulaId,
                    sedeId);

                List<FormulaCC> formulasCC = await _context.FormulasCC
                    .Where(w =>
                        w.FormulaId == formulaId &&
                        w.SedeId == sedeId)
                    .ToListAsync();

                _logger.LogInformation(
                    "FormulaCC existentes encontrados: {Cantidad}",
                    formulasCC.Count);

                // Eliminar registros anteriores
                if (formulasCC.Count > 0)
                {
                    _logger.LogInformation(
                        "Eliminando {Cantidad} FormulaCC anteriores.",
                        formulasCC.Count);

                    _context.FormulasCC.RemoveRange(formulasCC);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "FormulaCC anteriores eliminados correctamente.");
                }

                // Insertar nuevos registros
                foreach (var formula in request.FormulaCCs)
                {
                    _logger.LogInformation(
                        "Insertando FormulaCC. Variable: {Variable}, InsumoId: {InsumoId}",
                        formula.Variable,
                        formula.InsumoId);

                    FormulaCC formulaM = _mapper.Map<FormulaCC>(formula);

                    formulaM.FormulaId = formulaId;
                    formulaM.SedeId = sedeId;

                    _context.FormulasCC.Add(formulaM);
                }

                _logger.LogInformation(
                    "FormulaCC nuevas agregadas al contexto.");

                // Buscar laboratorio
                _logger.LogInformation(
                    "Buscando Laboratorio. FormulaId: {FormulaId}, SedeId: {SedeId}",
                    formulaId,
                    sedeId);

                Laboratorio? laboratorio = await _context.Laboratorios
                    .FirstOrDefaultAsync(x =>
                        x.Id == formulaId &&
                        x.SedeId == sedeId);

                if (laboratorio != null)
                {
                    _logger.LogInformation(
                        "Laboratorio encontrado. Id: {LaboratorioId}",
                        laboratorio.Id);

                    laboratorio.Procedimiento = request.Procedimiento;
                    laboratorio.EmpaqueId = request.EmpaqueId;
                }
                else
                {
                    _logger.LogWarning(
                        "No se encontró Laboratorio para FormulaId: {FormulaId}, SedeId: {SedeId}",
                        formulaId,
                        sedeId);
                }

                // Guardar todo
                _logger.LogInformation("Guardando cambios finales...");

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Actualizar finalizado correctamente. FormulaId: {FormulaId}",
                    formulaId);

                return "Cambio Exitoso";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error en Actualizar. FormulaId: {FormulaId}, SedeId: {SedeId}",
                    formulaId,
                    sedeId);

                return $"Error al actualizar: {ex.Message}";
            }
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
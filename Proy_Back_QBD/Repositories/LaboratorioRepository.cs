using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Models;

public class LaboratorioRepository : ILaboratorioRepository
{
    private readonly ApiContext _context;
    private readonly ILogger<LaboratorioRepository> _logger;

    public LaboratorioRepository(
        ApiContext context,
        ILogger<LaboratorioRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExisteLaboratorio(int formulaId, int sedeId)
    {
        _logger.LogInformation(
            "Validando existencia de laboratorio. FormulaId: {FormulaId}, SedeId: {SedeId}",
            formulaId,
            sedeId);

        var existe = await _context.Laboratorios
            .AnyAsync(x =>
                x.Id == formulaId &&
                x.SedeId == sedeId);

        if (!existe)
        {
            throw new NotFoundException(
                $"No se encontró el laboratorio con FormulaId: {formulaId} y SedeId: {sedeId}.");
        }
    }

    public async Task RegistrarFormulaCC(FormulaCC formulaCC)
    {
        try
        {
            await _context.FormulasCC.AddAsync(formulaCC);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al registrar FormulaCC. FormulaId: {FormulaId}, SedeId: {SedeId}",
                formulaCC.FormulaId,
                formulaCC.SedeId);

            throw new ServerException(
                "Error al registrar la fórmula CC.", ex);
        }
    }

    public async Task RegistrarLaboratorio(Laboratorio laboratorio)
    {
        try
        {
            await _context.Laboratorios.AddAsync(laboratorio);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al registrar Laboratorio. Id: {Id}, SedeId: {SedeId}",
                laboratorio.Id,
                laboratorio.SedeId);

            throw new ServerException(
                "Error al registrar el laboratorio.", ex);
        }
    }


}

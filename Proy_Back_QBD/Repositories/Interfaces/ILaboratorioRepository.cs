using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Models;

public interface ILaboratorioRepository
{
    Task ExisteLaboratorio(int formulaId, int sedeId);
    Task RegistrarFormulaCC(FormulaCC formulaCC);
    Task RegistrarLaboratorio(Laboratorio laboratorio);
}

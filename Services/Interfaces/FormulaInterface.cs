using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services
{
    public interface IFormulaService
    {
        Task<FormulaCreateResponse?> CrearFormPed(FormulaCreateReq request);
        Task<FormulaUpdateResponse?> Actualizar(int formulaId, int sedeId, FormulaUpdateReq request);
        Task<Formula?> ActualizarLab(int formulaId, int sedeId, FormulaUpdLabReq request);
        Task<List<RecetaRes>?> ListarReceta(int sedeId);
        Task<Formula?> Eliminar(int id, int sedeId);
        Task<string?> AgregarInserto(int id, int sedeId, string inserto);
        Task<Formula> ActualizarFormulaM(int formulaId, int sedeId, string FormulaMagistral);
        Task<FormulasLab?> ListarFormulasLab(int pedidoId, int sedeId);
        Task<EtiquetaRes?> ObtenerEtiqueta(int formulaId, int sedeId);
        Task<DetallesRes?> ObtenerDetalles(int formulaId, int sedeId);
        Task<InsertoRes?> ObtenerInserto(int formulaId, int sedeId);
        Task<string?> CambiarTipo(FormulaCambiarTipo request);
    }
}
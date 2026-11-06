using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Resolución de lotes para fórmulas.</summary>
    public interface IKardexLotesService
    {
        Task<List<LoteAsignadoRes>> ResolverLotesFormula(ResolverLotesFormulaReq request);
    }
}

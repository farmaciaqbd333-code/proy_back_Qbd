using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Cálculo del stock de una familia (MP, PI, ME, ECO, PT) para la lista principal.</summary>
    public interface IStockFamiliaStrategy
    {
        /// <summary>Código de la familia que atiende: MP, PI, ME, ECO o PT.</summary>
        string Familia { get; }
        Task<List<StockRes>> ObtenerStock(int idSede);
    }
}

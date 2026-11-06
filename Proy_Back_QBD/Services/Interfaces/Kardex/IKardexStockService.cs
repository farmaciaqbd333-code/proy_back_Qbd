using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Lista principal de stock y compras vencidas.</summary>
    public interface IKardexStockService
    {
        Task<List<StockRes>> StockListaPrincipal(string familia, int idSede);
        Task<List<ComprasVencidasRes>> ObtenerComprasVencidas(string familia, int idSede);
    }
}

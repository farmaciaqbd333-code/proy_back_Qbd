using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Detalle por lote de cada familia.</summary>
    public interface IKardexDetalleService
    {
        Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int insumoId, int idSede);
        Task<List<DetalleInsumoRes>> ObtenerDetallePI(int insumoId, int idSede);
        Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId, int idSede);
        Task<List<DetalleInsumoRes>> ObtenerDetallePT(int productoId, int idSede);
    }
}

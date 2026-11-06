using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Salidas y bajas del Kardex.</summary>
    public interface IKardexMovimientoService
    {
        Task<List<SalidaInsumoRes>> ObtenerSalidasInsumo(int insumoId, int idSede, string? familia = null);
        Task<List<SalidaInsumoRes>> ObtenerSalidasEmpaque(int empaqueId, int idSede);
        Task<List<SalidaInsumoRes>> ObtenerSalidasPT(int productoId, int idSede);
        Task<List<SalidaInsumoRes>> ObtenerSalidasEconomato(int economatoId, int idSede);
        Task<List<SalidaInsumoRes>> ObtenerSalidasPI(int insumoId, int idSede);
        Task<List<BajaInsumoRes>> ObtenerBajasInsumo(int insumoId, int idSede, string? familia = null);
    }
}

using Proy_back_QBD.Dto.Insumo;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services
{
    public interface IInsumoService
    {
        Task<List<InsumoLabRes>?> ListaFormulaR(int sedeId);
        Task<Insumo?> Crear(InsumoCreateReq request);
        Task<Insumo?> Actualizar(int id, InsumoUpdateReq request);
        Task<Insumo?> Eliminar(int id);
        Task<List<InsumoFindAllRes?>> Obtener();
        Task<InsumoFindIdRes?> ObtenerById(int id);
    }
}
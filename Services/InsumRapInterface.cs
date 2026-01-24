using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services
{
    public interface IInsumoRService
    {
        Task<InsumoR?> Crear(InsumoRCreateReq request);
        Task<InsumoR?> Actualizar(int id, InsumoRUpdateReq request);
        Task<InsumoR?> Eliminar(int id);
        Task<List<InsumoRFindAllRes?>> Obtener();
        Task<InsumoRFindIdRes?> ObtenerById(int id);
    }
}
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using proy_back_Qbd.Models;

namespace Proy_back_QBD.Services
{
    public interface IEconomatoService
    {
        Task<List<EconomatoRes>?> Obtener();
        Task<EconomatoRes?> ObtenerById(int id);
        Task<Economato?> Crear(EconomatoReq request);
        Task<Economato?> Actualizar(int id, EconomatoReq request);
        Task<bool> Eliminar(int id);
    }
}

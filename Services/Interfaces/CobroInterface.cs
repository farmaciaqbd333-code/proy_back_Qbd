using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Response.Proy_back_QBD.Dto.Response;

namespace Proy_back_QBD.Services
{
    public interface ICobroService
    {
        Task<CobroCreateRes?> Crear(CobroCreateReq request);
        Task<CobroCreateRes?> Actualizar(int id, int sedeId, CobroUpdateReq request);
        Task<Cobro?> Eliminar(int cobroId, int sedeId);
        Task<List<CobroByPedido?>> Obtener(int PedidoId, int sedeId);
        // Task<CobroFindIdResponse?> ObtenerById(int id);
    }
}
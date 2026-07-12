using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services
{
    public interface IPedidoService
    {
        Task<PedidoCreateRes?> Crear(PedidoCreateReq request);
        Task<PedidoUpdateResponse?> Actualizar(int id, int sedeId, PedidoUpdateReq request);
        Task<Pedido?> ActComprobante(int id, int sedeId, string? boleta);
        Task<string?> ActEstado(int id, int sedeId, string estado);
        Task<List<PedidoFindAllResponse?>> Listar(int sedeId);
        Task<List<PedidoLabFindAllRes2?>> ListarLab(int sedeId);
        Task<PedidoFindIdResponse?> ObtenerById(int id, int sedeId);
        Task<int> ContFormM(int sedeId, int mes, int? anio = null);
    }
}
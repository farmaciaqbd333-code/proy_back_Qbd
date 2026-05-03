using Proy_back_QBD.Request;
using Proy_back_QBD.Response.Proy_back_QBD.Dto.Response;

namespace Proy_back_QBD.Services
{
    public interface ICajaService
    {
        Task<CajaFindAllRes?> Obtener(CajaFindAllReq request, int sedeId);
    }
}
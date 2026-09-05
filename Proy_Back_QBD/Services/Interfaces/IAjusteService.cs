using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;

namespace Proy_back_QBD.Services
{
    public interface IAjusteService
    {
        public Task RegistrarAjuste(CrearAjusteReq request);
        public Task<List<TablaAjustesRes>> ListaAjustes(string familia, int idSede);
        public Task<List<DetalleAjusteRes>> DetalleAjuste(int registroId, string familia, int? idInsumo = null, int? idSede = null);
    }
}
using proy_back_Qbd.Models.Ajuste.request;

namespace Proy_back_QBD.Services
{
    public interface IAjusteService
    {
        public Task RegistrarAjuste(CrearAjusteReq request);
    }
}
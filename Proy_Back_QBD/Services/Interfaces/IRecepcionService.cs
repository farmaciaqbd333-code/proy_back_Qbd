using proy_back_Qbd.Dto.Recepcion;
using System.Threading.Tasks;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface IRecepcionService
    {
        Task ActualizarRecepcionAsync(int idNotaSalida, RecepcionNotaSalidaReq request);
    }
}

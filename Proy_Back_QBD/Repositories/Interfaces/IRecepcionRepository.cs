using proy_back_Qbd.Models;
using System.Threading.Tasks;

namespace proy_back_Qbd.Repositories.Interfaces
{
    public interface IRecepcionRepository
    {
        Task<NotaSalida?> GetNotaSalidaByIdAsync(int id);
        Task GuardarCambiosAsync();
    }
}

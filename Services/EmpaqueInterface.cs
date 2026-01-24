using Proy_back_QBD.Dto.Empaque;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services
{
    public interface IEmpaqueService
    {   
        Task<Empaque?> Crear(EmpaqueCreateReq request);
        Task<Empaque?> Actualizar(int id, EmpaqueUpdateReq request);
        Task<Empaque?> Eliminar(int id);
        Task<List<EmpaqueFindAllRes?>> Obtener();
        Task<EmpaqueFindIdRes?> ObtenerById(int id);
    }
}
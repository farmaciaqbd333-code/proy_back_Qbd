using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Services
{
    public interface ISedeService
    {
        Task<Sede?> Crear(Sede request);
        Task<Sede?> Actualizar(int id, SedeUpdateReq request);
        // Task<SedeUpdateResponse?> Actualizar(int id, SedeUpdateReq request);
        // Task<Sede?> Eliminar(int id);
        Task<List<SedeFindAllResponse?>> Obtener();
        Task<GeneralRes?> ObtGeneral(int id);
        Task<string?> ActualizarGeneral(int id, GeneralReq request);
    }
}
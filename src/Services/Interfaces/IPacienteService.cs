using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services
{
    public interface IPacienteService
    {
        Task<PacienteCreateResponse?> Crear(PacienteCreateReq request);
        Task<PacienteUpdateResponse?> Actualizar(int id, PacienteUpdateReq request);
        Task<Paciente?> Eliminar(int id);
        Task<List<PacienteFindAllResponse?>> Obtener(int sedeId);
        Task<PacienteFindIdResponse?> ObtenerById(int id);
    }
}
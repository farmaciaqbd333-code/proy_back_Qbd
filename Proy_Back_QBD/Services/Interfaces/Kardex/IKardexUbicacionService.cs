using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Asignación de ubicación y límite por sede.</summary>
    public interface IKardexUbicacionService
    {
        Task<object> AssignLocation(AssignLocationReq request);
        Task<object> AssignLimite(AssignLimiteReq request);
    }
}

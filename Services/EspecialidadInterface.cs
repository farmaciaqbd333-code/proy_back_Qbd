using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services.Interfaces
{
    public interface IEspecialidadService
    {
        Task<Especialidad?> Crear(EspecialidadCreateReq request);
        Task<Especialidad?> Actualizar(int id, EspecialidadUpdateReq request);
        Task<Especialidad?> Eliminar(int id);
        Task<List<EspecialidadFindAllResponse?>> Obtener();
        Task<EspecialidadFindIdResponse?> ObtenerById(int id);
    }
}
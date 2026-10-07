using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Services.Interfaces
{
    public interface ICotizacionService
    {
        Task<List<CotizacionRes>> Listar(int? sedeId, DateOnly? fechaInicio, DateOnly? fechaFin, string? search, string? estado);
        Task<CotizacionRes?> ObtenerPorId(int id);
        Task<string> ObtenerSiguienteNumero(int? sedeId);
        Task<CotizacionRes?> Crear(CotizacionCreateReq request, int usuarioId);
        Task<CotizacionRes?> Actualizar(int id, CotizacionUpdateReq request, int usuarioId);
        Task<bool> CambiarEstado(int id, string nuevoEstado, int usuarioId);
        Task<bool> AsignarCpe(int id, string cpe, int usuarioId);
        Task<bool> MarcarEnviado(int id, bool enviado, int usuarioId);
        Task<bool> Eliminar(int id);
    }
}

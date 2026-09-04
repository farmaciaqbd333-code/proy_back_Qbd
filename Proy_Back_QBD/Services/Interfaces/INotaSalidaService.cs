using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;

namespace proy_back_Qbd.Services.Interfaces.INotaSalidaService
{
    public interface INotaSalidaService
    {
        Task<int> Crear(CreateReq request);
        Task<List<RegistrosListaRes>> ObtenerDatosXRegistro(string registro, int idSede);
        Task<List<NotaSalidaListaRes>> ObtenerLista(int idSede);
        Task<List<NotaSalidaListaRes>> ObtenerListaPorSedeOrigen(int idSedeOrigen);
        Task<NotaSalidaListaRes?> ObtenerPorId(int id);
        Task<List<NotaSalidaDetalleRes>> ObtenerDetalles(int idNotaSalida);
        Task Actualizar(int id, CreateReq request);
        Task Eliminar(int id);
        Task Confirmar(ConfirmarReq request);
        Task<List<RegistrosRes>> ObtenerRegistrosXFamilia(ObtenerRegistroReq request);
        Task ActualizarObservacion(int idNotaSalida, string observacion);
    }
}

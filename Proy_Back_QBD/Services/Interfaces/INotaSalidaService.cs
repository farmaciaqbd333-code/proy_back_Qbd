using System.Collections.Generic;
using System.Threading.Tasks;
using proy_back_Qbd.Models;

public interface INotaSalidaService
{
    Task<int> CrearAsync(NotaSalidaCreateReq request);
    Task<List<RegistrosListaRes>> ObtenerRegistros(int idArticulo, string Familia, int idSede);
    Task<List<NotaSalidaListaRes>> ObtenerListaAsync(int idSede);
    Task<List<NotaSalidaDetalleRes>> ObtenerDetalleAsync(int idNotaSalida);
    Task ActualizarAsync(int id, NotaSalidaCreateReq request);
    Task EliminarAsync(int id);
    Task<List<FamiliasListaRes>> ObtenerFamiliaAsync(FamiliasListaReq request);
}
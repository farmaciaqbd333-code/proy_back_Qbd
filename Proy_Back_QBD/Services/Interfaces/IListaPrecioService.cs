using proy_back_Qbd.Models;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Lista de precios (Excel PREC. AV).</summary>
    public interface IListaPrecioService
    {
        Task<List<ListaPrecio>> Listar(string? search, string? especialidad, int? take, string? categoria);
        Task<List<EspecialidadConteoRes>> Especialidades();
        Task<ListaPrecio> Crear(ListaPrecioReq request);
        Task<ListaPrecio> Actualizar(int id, ListaPrecioReq request);
        Task Eliminar(int id);
        Task<ListaPrecioImportRes> Importar(ListaPrecioImportReq request);
    }
}

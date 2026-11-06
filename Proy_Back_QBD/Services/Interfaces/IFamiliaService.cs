using proy_back_Qbd.Models;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Familias de productos.</summary>
    public interface IFamiliaService
    {
        Task<List<Familia>> Listar();
        Task<Familia> Obtener(int id);
        Task<Familia> Crear(Familia familia);
        Task Actualizar(int id, Familia familia);
        Task Eliminar(int id);
    }
}

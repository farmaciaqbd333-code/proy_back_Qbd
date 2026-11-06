using proy_back_Qbd.Models;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Proveedores.</summary>
    public interface IProveedorService
    {
        Task<List<ProveedorDto>> Listar();
        Task<ProveedorDto> Obtener(int id);
        Task<ProveedorDto> Crear(ProveedorCreateDto dto);
        Task Actualizar(int id, ProveedorUpdateDto dto);
        Task Eliminar(int id);
    }
}

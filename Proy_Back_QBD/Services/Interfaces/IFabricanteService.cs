using proy_back_Qbd.Models;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    /// <summary>Fabricantes y su relación con proveedores.</summary>
    public interface IFabricanteService
    {
        Task<List<FabricanteResponseDto>> Listar();
        Task<FabricanteResponseDto> Obtener(int id);
        Task<List<FabricanteResponseDto>> ListarPorProveedor(int idProveedor);
        Task<FabricanteResponseDto> Crear(FabricanteCreateDto dto);
        Task Actualizar(int id, FabricanteUpdateDto dto);
        Task Eliminar(int id);
        Task AsociarProveedor(AsociarProveedorDto dto);
        Task DesasociarProveedor(AsociarProveedorDto dto);
    }
}

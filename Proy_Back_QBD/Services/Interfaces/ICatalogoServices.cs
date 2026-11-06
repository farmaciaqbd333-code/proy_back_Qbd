using proy_back_Qbd.Models;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    // Servicios de catálogos (familias, proveedores, fabricantes y lista de precios).
    // Cuando algo no existe lanzan NotFoundException, y cuando los datos no son válidos
    // BadRequestException. El middleware global los convierte en 404/400 con { message }.

    /// <summary>Familias de productos.</summary>
    public interface IFamiliaService
    {
        Task<List<Familia>> Listar();
        Task<Familia> Obtener(int id);
        Task<Familia> Crear(Familia familia);
        Task Actualizar(int id, Familia familia);
        Task Eliminar(int id);
    }

    /// <summary>Proveedores.</summary>
    public interface IProveedorService
    {
        Task<List<ProveedorDto>> Listar();
        Task<ProveedorDto> Obtener(int id);
        Task<ProveedorDto> Crear(ProveedorCreateDto dto);
        Task Actualizar(int id, ProveedorUpdateDto dto);
        Task Eliminar(int id);
    }

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

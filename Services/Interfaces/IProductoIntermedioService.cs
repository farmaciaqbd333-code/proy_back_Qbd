using Proy_back_QBD.Dto;

namespace Proy_back_QBD.Interface
{
    public interface IProductoIntermedioService
    {
        public Task<List<TablaPIRes>> ListaProductoIntermedio();
        public Task<TablaPIRes> DetalleConsumo();
        // public Task<PanelPIRes> CrearProductoIntermedio();
        // public Task<PanelPIRes> ModificarProductoIntermedio();
        // public Task<PanelPIRes> EliminarProductoIntermedio();
    }
}
using Proy_back_QBD.Dto;

namespace Proy_back_QBD.Interface
{
    public interface IProductoIntermedioService
    {
        public Task<IEnumerable<TablaPIRes>> ListaProductoIntermedio();
        public Task<IEnumerable<ConsumoPIRes>> DetalleConsumo(int id);
        // public Task<PanelPIRes> CrearProductoIntermedio();
        // public Task<PanelPIRes> ModificarProductoIntermedio();
        // public Task<PanelPIRes> EliminarProductoIntermedio();
    }
}
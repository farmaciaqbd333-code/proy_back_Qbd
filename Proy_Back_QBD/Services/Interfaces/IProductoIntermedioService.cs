using Proy_back_QBD.Dto;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Interface
{
    public interface IProductoIntermedioService
    {
        public Task<IEnumerable<TablaPIRes>> ListaProductoIntermedio();
        public Task<IEnumerable<ConsumoPIRes>> DetalleConsumo(int id);
        public Task<int> CrearProductoIntermedio(CrearProductoIntermedioReq request);
        public Task<int> ActualizarProductoIntermedio(int id, ActualizarProductoIntermedioReq request);
        public Task<IEnumerable<MasterPIRes>> ListaMaestraPI(string tipoUso);
        public Task<int> EliminarProductoIntermedio(int id);
        public Task<string> ObtenerRegistro();
        public Task<string> ObtenerPI();
    }
}
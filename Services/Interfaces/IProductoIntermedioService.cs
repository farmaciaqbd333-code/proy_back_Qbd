using Proy_back_QBD.Dto;

namespace Proy_back_QBD.Service
{
    public interface IProductoIntermedioService
    {
        public Task<PanelPIRes> ListaProductoIntermedio();
    }
}
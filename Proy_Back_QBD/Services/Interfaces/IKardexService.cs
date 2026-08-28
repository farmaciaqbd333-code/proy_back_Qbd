using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface IKardexService
    {
        public Task<List<StockRes>> StockListaPrincipal(string familia, int idSede);
        public Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int insumoId,int idSede);
        public Task<List<DetalleInsumoRes>> ObtenerDetallePI(int insumoId,int idSede);
        public Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId,int idSede);
        public Task<List<DetalleInsumoRes>> ObtenerDetallePT(int productoId, int idSede);
        public Task<List<ComprasVencidasRes>> ObtenerComprasVencidas(string familia,int idSede);
        public Task<List<SalidaInsumoRes>> ObtenerSalidasInsumo(int insumoId, int idSede);
        
    }
}
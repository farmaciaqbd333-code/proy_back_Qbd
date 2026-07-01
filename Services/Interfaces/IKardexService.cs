using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Kardex;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface IKardexService
    {
        public Task<ListarStockRes> StockListaPrincipal();
        public Task<List<DetalleInsumoRes>> ObtenerDetalleInsumo(int insumoId);
        public Task<List<DetalleEmpaqueRes>> ObtenerDetalleEmpaque(int empaqueId);
        
    }
}
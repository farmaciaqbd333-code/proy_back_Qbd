using proy_back_Qbd.Models.Ajuste.request;
using Riok.Mapperly.Abstractions;

namespace proy_back_Qbd.Models.Ajuste
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public partial class AjusteMapper
    {
        public AjusteInsumo CrearAjusteInsumo(CrearAjustes source, int idCreador)
        {
            return new AjusteInsumo
            {
                Ajuste = source.Ajuste,
                IdStockInsumo = source.IdCompraFamilia,
                StockAnterior = source.StockAnterior,
                StockNuevo = source.StockNuevo,
                IdCreador = idCreador,
                Observacion = source.Observacion
            };
        }
        public List<AjusteInsumo> CrearAjusteInsumoList(List<CrearAjustes> crearAjusteReqs, int idCreador)
        {
            return crearAjusteReqs.Select(s => CrearAjusteInsumo(s, idCreador)).ToList();
        }
        public AjusteEmpaque CrearAjusteEmpaque(CrearAjustes source, int idCreador)
        {
            return new AjusteEmpaque
            {
                Ajuste = source.Ajuste,
                IdStockEmpaque = source.IdCompraFamilia,
                StockAnterior = source.StockAnterior,
                StockNuevo = source.StockNuevo,
                IdCreador = idCreador,
                Observacion = source.Observacion
            };
        }
        public List<AjusteEmpaque> CrearAjusteEmpaqueList(List<CrearAjustes> crearAjusteReqs, int idCreador)
        {
            return crearAjusteReqs.Select(s => CrearAjusteEmpaque(s, idCreador)).ToList();
        }
        public AjusteEconomato CrearAjusteEconomato(CrearAjustes source, int idCreador)
        {
            return new AjusteEconomato
            {
                Ajuste = source.Ajuste,
                IdStockEconomato = source.IdCompraFamilia,
                StockAnterior = source.StockAnterior,
                StockNuevo = source.StockNuevo,
                IdCreador = idCreador,
                Observacion = source.Observacion
            };
        }
        public List<AjusteEconomato> CrearAjusteEconomatoList(List<CrearAjustes> crearAjusteReqs, int idCreador)
        {
            return crearAjusteReqs.Select(s => CrearAjusteEconomato(s, idCreador)).ToList();
        }
        public AjusteProducto CrearAjusteProductoTerminado(CrearAjustes source, int idCreador)
        {
            return new AjusteProducto
            {
                Ajuste = source.Ajuste,
                IdStockProducto = source.IdCompraFamilia,
                StockAnterior = source.StockAnterior,
                StockNuevo = source.StockNuevo,
                IdCreador = idCreador,
                Observacion = source.Observacion
            };
        }
        public List<AjusteProducto> CrearAjusteProductoTerminadoList(List<CrearAjustes> crearAjusteReqs, int idCreador)
        {
            return crearAjusteReqs.Select(s => CrearAjusteProductoTerminado(s, idCreador)).ToList();
        }
    }
}
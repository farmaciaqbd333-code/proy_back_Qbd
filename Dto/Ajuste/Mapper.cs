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
                IdCompraInsumo = source.IdCompraFamilia,
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
                IdCompraEmpaque = source.IdCompraFamilia,
                IdCreador = idCreador,
                Observacion = source.Observacion
            };
        }
        public List<AjusteEmpaque> CrearAjusteEmpaqueList(List<CrearAjustes> crearAjusteReqs, int idCreador)
        {
            return crearAjusteReqs.Select(s => CrearAjusteEmpaque(s, idCreador)).ToList();
        }
    }
}
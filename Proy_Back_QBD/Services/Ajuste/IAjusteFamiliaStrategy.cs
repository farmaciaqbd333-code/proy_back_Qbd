using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;

namespace Proy_back_QBD.Service.AjusteService.Familias
{
    /// <summary>
    /// Ajustes de inventario de una familia (MP, PI, ME, PT, ECO).
    /// Para añadir una familia basta con crear una clase nueva y registrarla.
    /// </summary>
    public interface IAjusteFamiliaStrategy
    {
        string Familia { get; }

        /// <summary>Lista principal de ajustes de la familia en la sede.</summary>
        Task<List<TablaAjustesRes>> Listar(int idSede);

        /// <summary>Agrega los ajustes al contexto (el guardado y la transacción los maneja AjusteService).</summary>
        Task Registrar(List<CrearAjustes> listaAjustes, int idCreador, int idSede = 0);

        Task<List<DetalleAjusteRes>> Detalle(int registroId, int? idInsumo, int? idSede);
    }
}

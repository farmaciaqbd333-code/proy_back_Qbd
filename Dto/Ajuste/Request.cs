namespace proy_back_Qbd.Models.Ajuste.request
{
    public class CrearAjusteReq
    {
        public string Familia { get; set; } = string.Empty;
        public int IdCreador { get; set; }
        public List<CrearAjustes> ListaAjustes { get; set; } = [];
    }
    public class CrearAjustes
    {
        public int IdCompraFamilia { get; set; }
        public decimal Ajuste { get; set; }
        public string? Observacion { get; set; }
    }
}
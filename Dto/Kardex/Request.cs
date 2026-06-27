namespace proy_back_Qbd.Models.Kardex
{
    public class CrearAjusteReq
    {
        public string Familia { get; set; } = string.Empty;
        public int Id { get; set; }
        public decimal Ajuste { get; set; }
        public int IdCreador { get; set; }
    }
}

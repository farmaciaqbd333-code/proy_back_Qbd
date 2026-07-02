namespace proy_back_Qbd.Models.Ajuste.response
{
    public class TablaAjustesRes
    {
        public required string Codigo { get; set; }
        public required string Registro { get; set; }
        public required string Descripcion { get; set; }
        public DateTimeOffset? FechaFabricacion { get; set; }
        public DateTimeOffset? FechaVencimiento { get; set; }
        public required string Lote { get; set; }
        public decimal Saldo { get; set; }
        public decimal Ajuste { get; set; }
        public string? Observacion { get; set; }
    }

}
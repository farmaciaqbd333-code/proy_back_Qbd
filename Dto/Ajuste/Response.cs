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
        public string? Observacion { get; set; }
    }
    public class DetalleAjusteRes
    {
        public DateTimeOffset? FechaCreacion { get; set; }
        public required decimal Stock { get; set; }
        public required decimal Diferencia { get; set; }
        public required decimal StockFinal { get; set; }
        public required string Usuario { get; set; }
        public required string Observacion { get; set; }
    }

}
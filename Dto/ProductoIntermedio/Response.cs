namespace Proy_back_QBD.Dto
{
    public class PanelPIRes
    {
        public int Id { get; set; }

        public string? Registro { get; set; }

        public string? Lote { get; set; }

        public string? Codigo { get; set; }

        public string? Descripcion { get; set; }

        public string? LoteEstandar { get; set; }

        public string? Tipo { get; set; }

        public decimal Cantidad { get; set; }

        public string? Um { get; set; } // Unidad de medida

        public DateTime? FechaEmision { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        public string? Elaborado { get; set; }
    }
}
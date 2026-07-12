using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models.ProductoIntermedio
{
    public class ProductoIntermedio
    {
        public int Id { get; set; }
        public string? Lote { get; set; }
        public int IdInsumo { get; set; }
        public string? LoteEstandar { get; set; }
        public string? Tipo { get; set; }
        public decimal Cantidad { get; set; }
        public string? Um { get; set; }
        public DateTimeOffset FechaEmision { get; set; }
        public DateTimeOffset FechaVencimiento { get; set; }
        public string? Elaborado { get; set; }
        public string? Autorizado { get; set; }
        public string? Procedimiento { get; set; }
        public int IdEmpaque { get; set; }
        public int IdEtiqueta { get; set; }
        public string? CodTermo { get; set; }
        public string? CodEtiqueta1 { get; set; }
        public string? CodEtiqueta2 { get; set; }
        public string? CodAdicional { get; set; }
        public string? Aspecto { get; set; }
        public string? Color { get; set; }
        public string? Olor { get; set; }
        public string? Ph { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public DateTimeOffset FechaModificacion { get; set; }
        public int IdCreador { get; set; }
        public int IdModificador { get; set; }

        // Navigation Properties
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public Empaque? Empaque { get; set; }
        public Insumo? Insumo { get; set; }
    }
}
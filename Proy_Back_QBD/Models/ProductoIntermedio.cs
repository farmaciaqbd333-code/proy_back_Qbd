using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models.ProductoIntermedio
{
    public class ProductoIntermedio
    {
        public int Id { get; set; }
        public string? Lote { get; set; }
        public int IdInsumo { get; set; }
        public int? LoteEstandar { get; set; }
        public decimal? PesoUnidad { get; set; }
        public decimal? LoteEstTotal { get; set; }
        public string? Tipo { get; set; }
        public string? TipoUso { get; set; }
        public decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int? IdElaborado { get; set; }
        public int? IdAutorizado { get; set; }
        public string? Procedimiento { get; set; }
        public string? Aspecto { get; set; }
        public string? Color { get; set; }
        public string? Olor { get; set; }
        public decimal? Ph { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int IdCreador { get; set; }
        public int? IdModificador { get; set; }

        // Navigation Properties
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public Usuario? Elaborador { get; set; }
        public Usuario? Autorizador { get; set; }
        public Insumo? Insumo { get; set; }
        public List<InsumoProductoIntermedio> InsumoProductoIntermedio { get; set; } = new();
        public List<EmpaqueProductoIntermedio> EmpaqueProductoIntermedios { get; set; } = new();
    }
}
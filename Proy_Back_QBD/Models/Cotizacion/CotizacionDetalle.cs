using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{
    [Table("cotizaciones_detalle")]
    public class CotizacionDetalle
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("cotizacion_id")]
        public int CotizacionId { get; set; }

        [JsonIgnore]
        public Cotizacion? Cotizacion { get; set; }

        [Column("producto_id")]
        public int? ProductoId { get; set; }

        [Column("formula_id")]
        public int? FormulaId { get; set; }

        [Column("codigo")]
        public string? Codigo { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("unidad")]
        public string Unidad { get; set; } = "NIU";

        [Column("cantidad")]
        public decimal Cantidad { get; set; } = 1;

        [Column("precio_unitario")]
        public decimal PrecioUnitario { get; set; } = 0;

        /// <summary>Tipo de producto: FM (fórmula magistral), FMVET, OFI, PT (producto terminado)...</summary>
        [Column("tipo_producto")]
        public string? TipoProducto { get; set; }
        [Column("descuento")]
        public decimal Descuento { get; set; } = 0;

        [Column("forma_farmaceutica")]
        public string? FormaFarmaceutica { get; set; }

        [Column("g_por_ml")]
        public decimal? GPorMl { get; set; }

        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; } = 0;
    }
}

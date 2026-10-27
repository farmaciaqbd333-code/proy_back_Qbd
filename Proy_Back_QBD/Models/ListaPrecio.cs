using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Models
{
    /// <summary>Lista de precios de fórmulas y productos (Excel QbD_PRECIOS_2025).</summary>
    [Table("lista_precios")]
    public class ListaPrecio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo")]
        public string? Codigo { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = "";

        /// <summary>FORMULA MAGISTRAL, FORMULA VETERINARIA, CATALOGO QbD, QF PROMOCIONES, PREPARADOS FARMACEUTICOS, ALAIA.</summary>
        [Column("categoria")]
        public string? Categoria { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("precio_fraccion")]
        public decimal PrecioFraccion { get; set; }

        [Column("clase")]
        public string? Clase { get; set; }

        /// <summary>Hoja del Excel de donde salió el registro.</summary>
        [Column("origen")]
        public string? Origen { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}

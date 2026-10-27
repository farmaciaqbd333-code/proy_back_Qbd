using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Models
{
    /// <summary>Precio propio de una sede para un producto de la lista de precios (si no existe, se usa el precio general).</summary>
    [Table("lista_precios_sede")]
    public class ListaPrecioSede
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_lista_precio")]
        public int IdListaPrecio { get; set; }

        [Column("id_sede")]
        public int IdSede { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;
    }
}

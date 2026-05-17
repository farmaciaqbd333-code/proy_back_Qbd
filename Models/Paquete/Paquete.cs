using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("paquete")]
    public class Paquete
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("cantidad_paquete")] public decimal CantidadPaquete { get; set; }
        [Column("peso_unitario")] public decimal PesoUnitario { get; set; }
        [Column("tara")] public decimal Tara { get; set; }
        [Column("fecha_creacion")] public DateTime? FechaCreacion { get; set; }
        [Column("id_creador")] public int IdCreador { get; set; }
        [Column("id_modificador")] public int? IdModificador { get; set; } = null;
        [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }
        [Column("id_detalle_compra")] public int IdDetalleCompra { get; set; }

        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public DetalleCompra? DetalleCompra { get; set; }
    }
}
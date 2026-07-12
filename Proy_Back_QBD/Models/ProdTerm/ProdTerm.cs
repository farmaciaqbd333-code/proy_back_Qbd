using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{

    [Table("productos_terminados")]
    public class ProdTerm
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }                          // ID único del pedido 
        [Column("costo")]
        public decimal Costo { get; set; }                    // Costo del pedido
        [Column("cantidad")]
        public int Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        [Column("producto_id")]
        public int? ProductoId { get; set; }                   // g/ml (gramos por mililitro)
        [Column("zona_aplicacion")]
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        [Column("diagnostico")]
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        [Column("estado")]
        public string? Estado { get; set; }                    // Estado del pedido (pendiente, procesado, entregado, etc.)
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }           // Fecha de creación del pedido
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaModificacion { get; set; }       // Fecha de la última modificación del pedido
        [Column("creador_id")]
        public int CreadorId { get; set; }
        [Column("sede_id")]
        public int? SedeId { get; set; }
        [Column("modificador_id")]
        public int ModificadorId { get; set; }
        [Column("pedido_id")]
        public int? PedidoId { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [JsonIgnore]
        public Pedido? Pedido { get; set; }
        [JsonIgnore]
        public Producto? Producto { get; set; }

    }

}
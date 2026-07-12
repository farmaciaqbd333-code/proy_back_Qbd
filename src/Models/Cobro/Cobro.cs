using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{

    [Table("cobros")]
    public class Cobro
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("modalidad")]
        public string? Modalidad { get; set; }
        [Column("pedido_id")]
        public int PedidoId { get; set; }
        [Column("sede_id")]
        public int? SedeId { get; set; }
        [JsonIgnore]
        public Pedido? Pedido { get; set; }
        [Column("importe")]
        public decimal Importe { get; set; } = 0;
        [Column("turno")]
        public string? Turno { get; set; }
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }  // Puede ser nulo               
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaModificacion { get; set; }  // Puede ser nulo        
        [Column("creador_id")]
        public int CreadorId { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [Column("modificador_id")]
        public int ModificadorId { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
    }

}
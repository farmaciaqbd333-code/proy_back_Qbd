using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{

    [Table("laboratorios")]
    public class Laboratorio
    {
        [Column("id")]
        public int? Id { get; set; }
        [Column("fecha_emision")]
        public DateOnly FechaEmision { get; set; }
        [Column("fecha_vcto")]
        public DateOnly FechaVcto { get; set; }
        [Column("elaborado")]
        public int? Elaborado { get; set; }
        [Column("autorizado")]
        public int? Autorizado { get; set; }
        [Column("procedimiento")]
        public string? Procedimiento { get; set; }
        [Column("empaque_id")]
        public int? EmpaqueId { get; set; }
        [Column("sede_id")]
        public int? SedeId { get; set; }
        [Column("cod_adicional")]
        public string? CodAdicional { get; set; }
        [Column("canti_termo")]
        public int? CantiTermo { get; set; }
        [Column("aspecto")]
        public string? Aspecto { get; set; }
        [Column("color")]
        public string? Color { get; set; }
        [Column("olor")]
        public string? Olor { get; set; }
        [Column("ph")]
        public string? Ph { get; set; }
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }           // Fecha de creación del pedido
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaModificacion { get; set; }       // Fecha de la última modificación del pedido
        [Column("creador_id")]
        public int CreadorId { get; set; }
        [Column("modificador_id")]
        public int ModificadorId { get; set; }
        [JsonIgnore]
        public Usuario ElaboradoU { get; set; }
        [JsonIgnore]
        public Usuario AutorizadoU { get; set; }
        [JsonIgnore]
        public Empaque? Empaque { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [JsonIgnore]
        public Formula? Formula { get; set; }

    }

}
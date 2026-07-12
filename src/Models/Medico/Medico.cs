using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{
    [Table("medicos")]
    public class Medico
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int? Id { get; set; }  // Puede ser nulo
        [Column("especialidad_id")]
        public int? EspecialidadId { get; set; }  // Puede ser nulo     
        [JsonIgnore]
        public Especialidad? Especialidad { get; set; }
        [Column("numero_especialidad")]
        public string? NumeroEspecialidad { get; set; }
        [Column("persona_id")]
        public int? PersonaId { get; set; }  // Puede ser nulo
        [JsonIgnore]
        public Persona? Persona { get; set; }  // Puede ser nulo
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
        [Column("sede_id")]
        public int SedeId { get; set; }
        [NotMapped]
        public Sede? Sede { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [Column("cmp")]
        public string? Cmp { get; set; }
        [JsonIgnore]
        public List<Pedido>? Pedidos { get; set; }

    }

}
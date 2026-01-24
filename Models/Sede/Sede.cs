using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{

    [Table("sedes")]
    public class Sede
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }  // Puede ser nulo
        [Column("nombre")]
        public string? Nombre { get; set; }
        [Column("direccion")]
        public string? Direccion { get; set; }  // Puede ser nulo               
        [Column("encargado")]
        public string? Encargado { get; set; }  // Puede ser nulo                        
        [Column("telefono")]
        public string? Telefono { get; set; }  // Puede ser nulo               
        [Column("msg_terminado")]
        public string? MsgTerminado { get; set; }  // Puede ser nulo                        
        [Column("msg_gpt")]
        public string? MsgGpt { get; set; }  // Puede ser nulo               
        [Column("msg_cumple")]
        public string? MsgCumple { get; set; }
        [Column("msg_seguimiento")]
        public string? MsgSeguimiento { get; set; }
        [Column("msg_enproceso")]
        public string? MsgEnProceso { get; set; }
        [Column("meta")]
        public int? Meta { get; set; }  // Puede ser nulo                        
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }  // Puede ser nulo               
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaModificacion { get; set; }  // Puede ser nulo               
        [Column("creador_id")]
        public int? CreadorId { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [Column("modificador_id")]
        public int? ModificadorId { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [JsonIgnore]
        public List<Asistencia>? Asistencias { get; set; }
        [JsonIgnore]
        public List<Medico>? Medicos { get; set; }
        [JsonIgnore]
        public List<Paciente>? Pacientes { get; set; }
    }

}
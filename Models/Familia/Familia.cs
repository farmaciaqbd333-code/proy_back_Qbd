using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("familias")]
    public class Familia
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public required string Nombre { get; set; }

        [Column("abreviatura")]
        public required string Abreviatura { get; set; }

        [JsonIgnore]
        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("id_creador")]
        public int IdCreador { get; set; }

        [JsonIgnore]
        [ForeignKey("IdCreador")]
        public Usuario? Creador { get; set; }
    }
}

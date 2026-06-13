using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("nota_salida")]
    public class NotaSalida
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")] public int Id { get; set; }
        [Column("fecha")] public DateTime Fecha { get; set; }
        [Column("destino")] public required string Destino { get; set; }
        [Column("observacion")] public string? Observacion { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_modificacion")] public DateTime FechaModificacion { get; set; }
        [Column("id_creador")] public int IdCreador { get; set; }
        [Column("id_modificador")] public int IdModificador { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
    }
}
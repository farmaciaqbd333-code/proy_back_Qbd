using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Proy_back_QBD.Models
{
    [Table("empaques")]
    public class Empaque
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("descripcion")]
        public string? Descripcion { get; set; }
        [Column("fundaId")]
        [ForeignKey("Funda")]
        public int? FundaId { get; set; }
        [JsonIgnore]
        public Empaque? Funda { get; set; }
        [Column("cajaId")]
        [ForeignKey("Caja")]
        public int? CajaId { get; set; }
        [JsonIgnore]
        public Empaque? Caja { get; set; }
        [Column("etiqueta_id1")]
        [ForeignKey("Etiqueta1")]
        public int? EtiquetaId1 { get; set; }
        [JsonIgnore]
        public Empaque? Etiqueta1 { get; set; }
        [Column("etiqueta_id2")]
        [ForeignKey("Etiqueta2")]
        public int? EtiquetaId2 { get; set; }
        [JsonIgnore]
        public Empaque? Etiqueta2 { get; set; }
        [Column("tara")]
        public string? Tara { get; set; }
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaModificacion { get; set; }  // Puede ser nulo        
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }  // Puede ser nulo        
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
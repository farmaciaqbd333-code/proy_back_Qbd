using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Proy_back_QBD.Models
{
    [Table("insumosR")]
    public class InsumoR
    {
        [Column("formulaR_id")]
        public int FormulaRId { get; set; }  // Puede ser nulo    
        [JsonIgnore]
        public required FormulaR FormulaR { get; set; }  // Puede ser nulo    
        [Column("insumo_id")]
        public int InsumoId { get; set; }  // Puede ser nulo  
        [JsonIgnore]
        public required Insumo Insumo { get; set; }  // Puede ser nulo    
        [Column("porcentaje")]
        public decimal Porcentaje { get; set; }  // Puede ser nulo    
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? FechaCreacion { get; set; }  // Puede ser nulo
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? FechaModificacion { get; set; }  // Puede ser nulo
        [Column("creador_id")]
        public int? CreadorId { get; set; }  // Puede ser nulo
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [Column("modificador_id")]
        public int? ModificadorId { get; set; }  // Puede ser nulo
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
    }
}
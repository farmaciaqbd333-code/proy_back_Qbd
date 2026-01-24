using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{

    [Table("formulas")]
    public class Formula
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                          // ID único del pedido
        [Column("costo")]
        public decimal Costo { get; set; }                    // Costo del pedido
        [Column("cantidad")]
        public int Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        [Column("formula_magistral")]
        public string? FormulaMagistral { get; set; }          // Descripción de la fórmula magistral
        [Column("forma_farmaceutica")]
        public string? FormaFarmaceutica { get; set; }           // Descripción de la fórmula de farmacia
        [Column("g/ml")]
        public string GPorMl { get; set; }                   // g/ml (gramos por mililitro)
        [Column("unidad_medida")]
        public string? UnidadMedida { get; set; }              // Unidad de medida (ej. "ml", "mg", "g", etc.)
        [Column("lote")]
        public string? Lote { get; set; }
        [Column("diagnostico")]
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        [Column("zona_aplicacion")]
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        [Column("estado")]
        public string? Estado { get; set; }
        [Column("reportado")]
        public string? Reportado { get; set; }                   // Si ha sido reportado o no (valor booleano)
        [Column("inserto")]
        public string? Inserto { get; set; }                   // Si ha sido reportado o no (valor booleano)
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }           // Fecha de creación del pedido
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaModificacion { get; set; }       // Fecha de la última modificación del pedido
        [Column("creador_id")]
        public int CreadorId { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [Column("modificador_id")]
        public int ModificadorId { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [Column("pedido_id")]
        public int? PedidoId { get; set; }
        [Column("sede_id")]
        public int? SedeId { get; set; }
        [JsonIgnore]
        public Pedido? Pedido { get; set; }
        [JsonIgnore]
        public Laboratorio? Laboratorio { get; set; }
        [JsonIgnore]
        public List<FormulaCC>? FormulaCC { get; set; }

    }

}
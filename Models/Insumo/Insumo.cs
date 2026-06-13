using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.ElaboracionBase;

namespace Proy_back_QBD.Models
{

    [Table("insumos")]
    public class Insumo
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("descripcion")]
        public required string Descripcion { get; set; }
        [Column("um")]
        public required string UnidadMedida { get; set; }
        [Column("fc")]
        public required string FactorCorreccion { get; set; }
        [Column("dil")]
        public string? Dilucion { get; set; } = "";
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
        [Column("id_familia")]
        public int? IdFamilia { get; set; }
        [Column("costo")]
        public decimal? Costo { get; set; }
        [Column("forma_f")]
        public string? FormaFarmaceutica { get; set; } = "";
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public Familia? Familia { get; set; }
        public List<FormulaCC>? FormulasCC { get; set; }
        public List<CompraInsumos>? DetalleCompras { get; set; }
        public List<ElaboracionBase>? ElaboracionBases { get; set; }

    }

}
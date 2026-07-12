using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{

    [Table("pedidos")]
    public class Pedido
    {
        [Column("paciente_id")]
        public int? PacienteId { get; set; }
        [JsonIgnore]
        public Paciente? Paciente { get; set; }
        [Column("recibo")]
        public string? Recibo { get; set; }
        [Column("img1")]
        public string? Img1 { get; set; }
        [Column("img2")]    
        public string? Img2 { get; set; }
        [Column("img3")]
        public string? Img3 { get; set; }
        [Column("img4")]
        public string? Img4 { get; set; }
        [Column("img5")]
        public string? Img5 { get; set; }
        [Column("img6")]
        public string? Img6 { get; set; }
        [Column("estado")]
        public string? Estado { get; set; }
        [Column("adelanto")]
        public decimal Adelanto { get; set; } = 0;
        [Column("saldo")]
        public decimal Saldo { get; set; } = 0;
        [Column("total")]
        public decimal Total { get; set; } = 0;
        [Column("comprobante_electronico")]
        public string? ComprobanteElectronico { get; set; }
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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }  // Puede ser nulo
        [Column("fecha_entrega")]
        public DateTime FechaEntrega { get; set; }  // Puede ser nulo
        [Column("medico_id")]
        public int MedicoId { get; set; }
        [JsonIgnore]
        public Medico? Medico { get; set; }
        [JsonIgnore]
        public List<Formula>? Formulas { get; set; }
        [JsonIgnore]
        public List<ProdTerm>? ProdTerms { get; set; }
        [JsonIgnore]
        public List<Cobro>? Cobros { get; set; }
        [Column("sede_id")]
        public int? SedeId { get; set; }  // Puede ser nulo
        [JsonIgnore]
        public Sede? Sede { get; set; }  // Puede ser nulo
    }

}
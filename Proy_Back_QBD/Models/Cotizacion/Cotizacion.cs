using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{
    [Table("cotizaciones")]
    public class Cotizacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("numero")]
        public string Numero { get; set; } = string.Empty;

        [Column("fecha")]
        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        [Column("sede_id")]
        public int? SedeId { get; set; }

        [JsonIgnore]
        public Sede? Sede { get; set; }

        [Column("paciente_id")]
        public int? PacienteId { get; set; }

        [JsonIgnore]
        public Paciente? Paciente { get; set; }

        [Column("ruc_dni")]
        public string RucDni { get; set; } = string.Empty;

        [Column("denominacion")]
        public string Denominacion { get; set; } = string.Empty;

        [Column("direccion")]
        public string? Direccion { get; set; }

        [Column("telefono")]
        public string? Telefono { get; set; }

        [Column("correo")]
        public string? Correo { get; set; }

        [Column("moneda")]
        public string Moneda { get; set; } = "S/";

        [Column("descuento_global_porc")]
        public decimal DescuentoGlobalPorc { get; set; } = 0;

        [Column("descuento_global")]
        public decimal DescuentoGlobal { get; set; } = 0;

        [Column("descuento_item")]
        public decimal DescuentoItem { get; set; } = 0;

        [Column("descuento_total")]
        public decimal DescuentoTotal { get; set; } = 0;

        [Column("op_exonerada")]
        public decimal OpExonerada { get; set; } = 0;

        [Column("op_inafecta")]
        public decimal OpInafecta { get; set; } = 0;

        [Column("op_gravada")]
        public decimal OpGravada { get; set; } = 0;

        [Column("igv")]
        public decimal Igv { get; set; } = 0;

        [Column("op_gratuita")]
        public decimal OpGratuita { get; set; } = 0;

        [Column("otros_cargos")]
        public decimal OtrosCargos { get; set; } = 0;

        [Column("total")]
        public decimal Total { get; set; } = 0;

        [Column("estado")]
        public string Estado { get; set; } = "Pendiente";

        [Column("enviado_cliente")]
        public bool EnviadoCliente { get; set; } = false;

        [Column("cpe_relacionado")]
        public string? CpeRelacionado { get; set; }

        [Column("validez_dias")]
        public int ValidezDias { get; set; } = 15;

        [Column("forma_pago")]
        public string? FormaPago { get; set; } = "Al Contado";

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("creador_id")]
        public int CreadorId { get; set; }

        [JsonIgnore]
        public Usuario? Creador { get; set; }

        [Column("modificador_id")]
        public int? ModificadorId { get; set; }

        [JsonIgnore]
        public Usuario? Modificador { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; } = DateTime.Now;

        public List<CotizacionDetalle> Detalles { get; set; } = new();
    }
}

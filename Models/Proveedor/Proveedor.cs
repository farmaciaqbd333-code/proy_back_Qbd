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
    [Table("proveedores")]
    public class Proveedor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id_proveedor")] public int IdProveedor { get; set; }
        [Column("numero_prov")] public required string CodigoProv { get; set; }
        [Column("datos")] public required string Datos { get; set; }
        [Column("direccion")] public string Direccion { get; set; } = "";
        [Column("telefono")] public string Telefono { get; set; } = "";
        [Column("referencia")] public string Referencia { get; set; } = "";
        [Column("codigo_provedor")] public string? CodigoProvedor { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("creador")] public required int IdCreador { get; set; }
        [JsonIgnore]
        public List<OrdenCompra>? OrdenCompras { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }

    }
    public class ProveedorCreateDto
    {
        public required string CodigoProv { get; set; } 
        public required string Datos { get; set; }
        public string Direccion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Referencia { get; set; } = "";
        public string? CodigoProvedor { get; set; }
        public required int IdCreador { get; set; }
    }

    // DTO para actualizar un proveedor
    public class ProveedorUpdateDto
    {
        public required string CodigoProv { get; set; }
        public required string Datos { get; set; }
        public string Direccion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Referencia { get; set; } = "";
        public string? CodigoProvedor { get; set; }
    }

    // DTO para respuesta (lectura)
    public class ProveedorDto
    {
        public int IdProveedor { get; set; }
        public string CodigoProv { get; set; } = string.Empty;
        public string Datos { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string? CodigoProvedor { get; set; }
        public DateTime FechaCreacion { get; set; }
        public required int IdCreador { get; set; }
    }
}
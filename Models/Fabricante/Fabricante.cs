using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("fabricantes")]
    public class Fabricante
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo")]
        public string? Codigo { get; set; }

        [Column("nombre")]
        public required string Nombre { get; set; }

        [Column("pais")]
        public string? Pais { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; }

        [Column("id_creador")]
        public required int IdCreador { get; set; }

        [Column("fecha_modificacion")]
        public DateTime? FechaModificacion { get; set; }

        [Column("id_modificador")]
        public int? IdModificador { get; set; }

        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }

        // Relación Muchos a Muchos con Proveedor (Opción B)
        public List<Proveedor>? Proveedores { get; set; }

        // Relación con los productos/detalles registrados
        public List<DetalleCompra>? DetalleCompras { get; set; }
    }
}

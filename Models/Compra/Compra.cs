using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("compra")]
    public class Compra
    {
        [Key]
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("fecha")] public DateTime Fecha { get; set; }
        [Column("factura")] public string Factura { get; set; }
        [Column("guia")] public string Guia { get; set; }
        [Column("cod_fac")] public string CodFac { get; set; }
        [Column("id_orden_compra")] public int IdOrdenCompra { get; set; }
        [Column("numero_oc")] public string NumeroOc { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("id_usuario")] public int IdUsuario { get; set; }
        [Column("fecha_factura")] public DateTime FechaFactura { get; set; }
        public required Usuario Usuario { get; set; }
        public required OrdenCompra OrdenCompra { get; set; }
    }
}
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
        [Column("id_compra")] public int? IdCompra { get; set; }
        [Column("cod_fac")] public required string CodFactura { get; set; }
        [Column("guia")] public required string Guia { get; set; }
        [Column("cod_fac_qbd")] public required string CodFacturaQBD { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Column("fecha_factura")] public required DateTime FechaFactura { get; set; }
        [Column("img_factura")] public string? ImgFactura { get; set; }
        public Usuario? Usuario { get; set; }
        public OrdenCompra? OrdenCompra { get; set; }
    }
    public class CompraCreateReq
    {
        public required string CodFactura { get; set; }
        public required string Guia { get; set; }
        public required string CodFacturaQBD { get; set; }
        public int IdOrdenCompra { get; set; }
        public int IdUsuario { get; set; }
        public DateTime FechaFactura { get; set; }
        public string? ImgFactura { get; set; }
        public required List<CompraCreateReq2> Detalle { get; set; }
    }
    public class CompraCreateReq2
    {
        public required string Lote { get; set; }
        public required DateTime FechaFab { get; set; }
        public required DateTime FechaVec { get; set; }
        public string? Coa { get; set; }
        public string? RegistroSanitario { get; set; }
        public string? Conformidad { get; set; }
        public required int IdCreador { get; set; }
    }
}
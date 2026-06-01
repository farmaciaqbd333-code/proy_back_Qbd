using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("compra_otros")]
    public class CompraOtros
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("clasificacion")] public string Clasificacion { get; set; } = "";
        [Column("cantidad_solicitada")] public decimal CantidadSolicitada { get; set; }
        [Column("um")] public required string UnidadMedida { get; set; }
        [Column("costo_unitario")] public decimal CostoUnitario { get; set; }
        [Column("costo_total")] public decimal CostoTotal { get; set; }
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("id_creador")] public int IdCreador { get; set; }
        [Column("id_modificador")] public int? IdModificador { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }
        [Column("descripcion_factura")] public string? DescripcionFactura { get; set; } = "";
        [Column("conformidad")] public bool? Conformidad { get; set; }
        [Column("cantidad_recibida")]public decimal? CantidadRecibida { get; set; }
        public Compra? Compra { get; set; }
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
    }
}
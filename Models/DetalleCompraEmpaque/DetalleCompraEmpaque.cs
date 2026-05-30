using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("detalle_compra_empaque")]
    public class DetalleCompraEmpaque
    {
        [Column("id")] public int Id { get; set; }
        [Column("id_empaque")] public int IdEmpaque { get; set; }
        [Column("cantidad_solicitada")] public decimal CantidadSolicitada { get; set; }
        [Column("cantidad_recibida")]public decimal? CantidadRecibida { get; set; }
        [Column("costo_unitario")] public decimal CostoUnitario { get; set; }
        [Column("costo_total")] public decimal CostoTotal { get; set; }
        [Column("um")] public string? Um { get; set; }
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Column("id_modificador")] public int? IdModificador { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }
        [Column("conformidad")] public bool? Conformidad { get; set; }
        [Column("descripcion_factura")] public string? DescripcionFactura { get; set; } = "";
        [Column("id_fabricante")] public int? IdFabricante { get; set; }
        [Column("coa")] public bool? Coa { get; set; }
        [Column("lote")] public string? Lote { get; set; }
        [Column("fecha_fabricacion")] public DateTime? FechaFabricacion { get; set; }
        [Column("fecha_vencimiento")] public DateTime? FechaVencimiento { get; set; }
        public Compra? Compra { get; set; }
        public Empaque? Empaque { get; set; }
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
    }
}
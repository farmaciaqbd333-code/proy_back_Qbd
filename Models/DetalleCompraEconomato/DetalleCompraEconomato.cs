using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("detalle_compra_economato")]
    public class DetalleCompraEconomato
    {
        [Column("id")] public int Id { get; set; }
        [Column("id_economato")] public int IdEconomato { get; set; }
        [Column("cantidad_solicitada")] public decimal CantidadSolicitada { get; set; }
        [Column("costo_unitario")] public decimal CostoUnitario { get; set; }
        [Column("costo_total")] public decimal CostoTotal { get; set; }
        [Column("um")] public string? Um { get; set; }
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Column("id_modificador")] public int? IdModificador { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }
        [Column("conformidad")] public bool Conformidad { get; set; }
        [Column("cantidad_recibida")] public decimal CantidadRecibida { get; set; }
        [Column("descripcion_factura")] public string? DescripcionFactura { get; set; } = "";
        [Column("id_fabricante")] public int? IdFabricante { get; set; }
        public Compra? Compra { get; set; }
        public Economato? Economato { get; set; }
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
    }
}
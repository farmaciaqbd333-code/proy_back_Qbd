using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    [Table("detalle_compra")]
    public class DetalleCompra
    {
        [Column("id")] public int Id { get; set; }
        [Column("clasificacion")] public string Clasificacion { get; set; } = string.Empty;
        [Column("cantidad_solicitada")] public decimal CantidadSolicitada { get; set; }
        [Column("costo_unitario")] public decimal CostoUnitario { get; set; }
        [Column("costo_total")] public decimal CostoTotal { get; set; }
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("um")] public required string UnidadMedida { get; set; }
        public Compra? Compra { get; set; }
    }
}
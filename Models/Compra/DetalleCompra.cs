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
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("id_insumo")] public int IdInsumo { get; set; }
        [Column("cantidad")] public decimal Cantidad { get; set; }
        [Column("costototal")] public decimal CostoTotal { get; set; }
        [Column("denominacion_fact")] public string DenominacionFact { get; set; }
        [Column("cantidad_paquete")] public decimal CantidadPaquete { get; set; }
        [Column("lote")] public string Lote { get; set; }
        [Column("potencia")] public string Potencia { get; set; }
        [Column("fechafab")] public DateTime FechaFab { get; set; }
        [Column("fechavec")] public DateTime FechaVec { get; set; }
        [Column("coa")] public string Coa { get; set; }
        [Column("registro")] public string Registro { get; set; }
        [Column("conformidad")] public string Conformidad { get; set; }
        [Column("rega")] public string Rega { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("usuario")] public string Usuario { get; set; }
    }
}
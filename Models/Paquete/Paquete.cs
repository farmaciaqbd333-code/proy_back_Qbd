using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models.Paquete
{
    [Table("paquete")]
    public class Paquete
    {
        [Column("id")] public int Id { get; set; }
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("id_insumo")] public int IdInsumo { get; set; }
        [Column("cantidad_paquete")] public decimal CantidadPaquete { get; set; }
        [Column("cantidad")] public decimal Cantidad { get; set; }
        [Column("tara")] public decimal Tara { get; set; }
        [Column("lote")] public string Lote { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("usuario")] public string Usuario { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models.NotaSalida
{
    [Table("detalle_nota_salida")]
    public class DetalleNotaSalida
    {
        [Column("id_nota_salida")] public int IdNotaSalida { get; set; }
        [Column("familia")] public string Familia { get; set; }
        [Column("registro")] public string Registro { get; set; }
        [Column("id_insumo")] public int IdInsumo { get; set; }
        [Column("cantidad")] public decimal Cantidad { get; set; }
        [Column("um")] public string Um { get; set; }
        [Column("lote")] public string Lote { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("usuario")] public string Usuario { get; set; }
    }
}
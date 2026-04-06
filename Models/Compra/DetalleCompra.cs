using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("detalle_compra")]
    public class DetalleCompra
    {
        [Column("id_compra")] public int IdCompra { get; set; }
        [Column("cantidad_paquete")] public decimal? CantidadPaquete { get; set; }
        [Column("lote")] public required string Lote { get; set; }
        [Column("potencia")] public string? Potencia { get; set; }
        [Column("fechafab")] public required DateTime FechaFab { get; set; }
        [Column("fechavec")] public required DateTime FechaVec { get; set; }
        [Column("coa")] public string? Coa { get; set; }
        [Column("registro_sanitario")] public string? RegistroSanitario { get; set; }
        [Column("conformidad")] public string? Conformidad { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("fecha_modificacion")] public DateTime FechaModificacion { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        public Usuario? Creador { get; set; }
    }
}
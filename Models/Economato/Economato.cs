using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    [Table("economato")]
    public class Economato
    {
        [Column("id")] public int Id { get; set; }
        [Column("descripcion")] public string Descripcion { get; set; } = string.Empty;
        [Column("unidad_medida")] public string? UnidadMedida { get; set; }
        public List<CompraEconomatos>? DetalleCompraEconomatos { get; set; }
    }
}
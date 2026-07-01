using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace proy_back_Qbd.Models
{

    [Table("ajuste_insumo")]
    public class AjusteInsumo
    {
        [Key] [Column("id")] public int Id { get; set; }

        [Column("ajuste")] public decimal Ajuste { get; set; }

        [Column("id_compra_insumo")] public int IdCompraInsumo { get; set; }

        [Column("fecha_creacion")] public DateTimeOffset FechaCreacion { get; set; }= DateTimeOffset.Now;

        [Column("id_creador")] public int IdCreador { get; set; }

        [ForeignKey(nameof(IdCompraInsumo))] public CompraInsumos? CompraInsumos { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace proy_back_Qbd.Models
{

    [Table("ajuste_empaque")]
    public class AjusteEmpaque
    {
        [Key][Column("id")] public int Id { get; set; }

        [Column("ajuste")] public decimal Ajuste { get; set; }
        [Column("stock_anterior")] public decimal StockAnterior { get; set; }
        [Column("stock_nuevo")] public decimal StockNuevo { get; set; }
        [Column("observacion")] public string? Observacion { get; set; }

        [Column("id_compra_empaque")] public int IdCompraEmpaque { get; set; }

        [Column("fecha_creacion")] public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.Now;

        [Column("id_creador")] public int IdCreador { get; set; }

        [ForeignKey(nameof(IdCompraEmpaque))] public CompraEmpaques? CompraEmpaques { get; set; }
        [ForeignKey(nameof(IdCreador))] public Usuario? Creador { get; set; }
    }
}
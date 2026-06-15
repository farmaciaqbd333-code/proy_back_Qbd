using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("detalle_nota_salida_empaque")]
    public class DetalleNotaSalidaEmpaque
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")] public int Id { get; set; }

        [Column("id_empaque")] public int IdEmpaque { get; set; }

        [Column("cantidad")] public decimal Cantidad { get; set; }

        [Column("um")] public string? Um { get; set; }

        [Column("lote")] public string? Lote { get; set; }

        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }

        [Column("fecha_modificacion")] public DateTime FechaModificacion { get; set; }

        [Column("id_creador")] public int IdCreador { get; set; }

        [Column("id_modificador")] public int IdModificador { get; set; }

        [ForeignKey(nameof(IdCreador))] public Usuario? Creador { get; set; }

        [ForeignKey(nameof(IdEmpaque))] public Empaque? Empaque { get; set; }

        [ForeignKey(nameof(IdModificador))] public Usuario? Modificador { get; set; }

    }
}
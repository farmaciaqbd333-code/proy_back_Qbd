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
    [Table("detalle_nota_salida_insumo")]
    public class DetalleNotaSalidaInsumo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")] public int Id { get; set; }

        [Column("id_insumo")] public int IdInsumo { get; set; }

        [Column("cantidad")] public decimal Cantidad { get; set; }

        [Column("um")] public string? Um { get; set; }

        [Column("lote")] public string? Lote { get; set; }

        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }

        [Column("fecha_modificacion")] public DateTime FechaModificacion { get; set; }

        [Column("id_creador")] public int IdCreador { get; set; }

        [Column("id_modificador")] public int IdModificador { get; set; }

        [ForeignKey(nameof(IdCreador))] public Usuario? Creador { get; set; }

        [ForeignKey(nameof(IdInsumo))] public Insumo? Insumo { get; set; }

        [ForeignKey(nameof(IdModificador))] public Usuario? Modificador { get; set; }

    }
}
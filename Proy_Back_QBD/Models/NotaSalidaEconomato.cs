using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("nota_salida_economato")]
    public class NotaSalidaEconomato
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")] public int Id { get; set; }

        [Column("id_economato")] public int IdEconomato { get; set; }

        [Column("cantidad")] public decimal Cantidad { get; set; }

        [Column("um")] public string? Um { get; set; }

        [Column("lote")] public string? Lote { get; set; }

        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }

        [Column("fecha_modificacion")] public DateTime FechaModificacion { get; set; }

        [Column("id_creador")] public int IdCreador { get; set; }

        [Column("id_modificador")] public int IdModificador { get; set; }

        [ForeignKey(nameof(IdCreador))] public Usuario? Creador { get; set; } = null!;

        [ForeignKey(nameof(IdEconomato))] public Economato? Economato { get; set; } = null!;

        [ForeignKey(nameof(IdModificador))] public Usuario? Modificador { get; set; } = null!;

    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

public class CompraInsumoProductoIntermedio
{
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }
    [Column("id_insumo_producto_intermedio")] public int IdInsumoProductoIntermedio { get; set; }
    [Column("cantidad")] public decimal Cantidad { get; set; }
    [Column("unidad_medida")] public string UnidadMedida { get; set; } = "G";
    [Column("fecha_creacion"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTimeOffset FechaCreacion { get; set; }
    [Column("id_creador")] public int IdCreador { get; set; }
    [Column("id_compra_insumo")] public int IdCompraInsumo { get; set; }
    [Column("id_modificador")] public int? IdModificador { get; set; }
    [Column("fecha_modificacion")] public DateTimeOffset? FechaModificacion { get; set; }
    [ForeignKey(nameof(IdInsumoProductoIntermedio))] public required InsumoProductoIntermedio InsumoProductoIntermedio { get; set; }
    [ForeignKey(nameof(IdCompraInsumo))] public required CompraInsumos CompraInsumo { get; set; }
    [ForeignKey(nameof(IdCreador))] public required Usuario Creador { get; set; }
    [ForeignKey(nameof(IdModificador))] public Usuario? Modificador { get; set; }
}
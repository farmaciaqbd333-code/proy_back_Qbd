using System.ComponentModel.DataAnnotations.Schema;
using proy_back_Qbd.Models.ProductoIntermedio;
using Proy_back_QBD.Models;

[Table("insumo_producto_intermedio")]
public class InsumoProductoIntermedio
{
    [Column("id")] public int Id { get; set; }

    [Column("id_insumo")] public int IdInsumo { get; set; }

    [Column("porcentaje")] public decimal Porcentaje { get; set; }

    [Column("v")] public required string V { get; set; }

    [Column("cantidad_unidad")] public decimal CantidadUnidad { get; set; }

    [Column("factor_correcion")] public decimal FactorCorrecion { get; set; }

    [Column("dilucion")] public decimal Dilucion { get; set; }

    [Column("unidad_medida")] public required string UnidadMedida { get; set; }

    [Column("cantidad_lote")] public decimal CantidadLote { get; set; }

    [Column("practica")] public decimal Practica { get; set; }

    [Column("csp")] public bool Csp { get; set; }

    [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }

    [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }

    [Column("creador")] public Usuario? Creador { get; set; }

    [Column("modificador")] public Usuario? Modificador { get; set; }
    [Column("id_producto_intermedio")] public int IdProductoIntermedio { get; set; }
    [ForeignKey(nameof(IdInsumo))] public Insumo? Insumo { get; set; }
    [ForeignKey(nameof(IdProductoIntermedio))] public ProductoIntermedio? ProductoIntermedio { get; set; }
}
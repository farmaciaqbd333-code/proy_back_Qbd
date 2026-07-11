using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using proy_back_Qbd.Models;
using Proy_back_QBD.Models;

[Table("ajuste_producto_terminado")]
public class AjusteProductoTerminado
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("ajuste")]
    public decimal Ajuste { get; set; }

    [Column("id_compra_producto"), ForeignKey(nameof(CompraProducto))]
    public int IdCompraProducto { get; set; }

    [Column("fecha_creacion")]
    public DateTimeOffset? FechaCreacion { get; set; }

    [Column("fecha_modificacion")]
    public DateTimeOffset? FechaModificacion { get; set; }

    [Column("id_creador"), ForeignKey(nameof(Creador))]
    public int IdCreador { get; set; }

    [Column("id_modificador")]
    public int? IdModificador { get; set; }

    [Column("stock_anterior")]
    public decimal StockAnterior { get; set; }

    [Column("stock_nuevo")]
    public decimal StockNuevo { get; set; }

    [Column("observacion")]
    public string? Observacion { get; set; }

    public virtual CompraProductos CompraProducto { get; set; } = null!;
    public virtual Usuario? Creador { get; set; } = null!;
}
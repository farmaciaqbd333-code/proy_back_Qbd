using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Models
{
    [Table("compra")]
    public class Compra
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")] public int Id { get; set; }
        [Column("id_proveedor")] public int IdProveedor { get; set; }
        [Column("moneda")] public required string Moneda { get; set; }
        [Column("tipo_cambio")] public required decimal TipoCambio { get; set; }
        [Column("fecha_cotizacion")] public required DateTime FechaCotizacion { get; set; }
        [Column("modalidad_pago")] public required string Modalidad { get; set; }
        [Column("observaciones")] public required string Observaciones { get; set; }
        [Column("id_sede")] public required int IdSede { get; set; }
        [Column("estado_compra")] public required string EstadoCompra { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Column("igv")] public required bool Igv { get; set; }
        [Column("id_modificador")] public int? IdModificador { get; set; }
        [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }
        [Column("serie_comprobante")] public string? SerieComprobante { get; set; }
        [Column("numero_comprobante")] public string? NumeroComprobante { get; set; }
        [Column("guia")] public string? Guia { get; set; }
        [Column("cod_fac_qbd")] public string? CodFacQBD { get; set; }
        [Column("fecha_factura")] public DateTime? FechaFactura { get; set; }
        [Column("img_factura")] public string? ImgFactura { get; set; }
        [Column("tipo_comprobante")] public string? TipoComprobante { get; set; }
        [Column("isc")] public decimal Isc { get; set; }
        [Column("icbp")] public decimal Icbp { get; set; }
        [Column("valor")] public decimal Valor { get; set; }
        [Column("total")] public decimal Total { get; set; }
        [Column("familia")] public required string Familia { get; set; }
        

        public Proveedor? Proveedor { get; set; }
        public Sede? Sede { get; set; }
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public List<CompraOtros>? CompraOtros { get; set; }
        public List<CompraInsumos>? CompraInsumos { get; set; }
        public List<CompraEconomatos>? CompraEconomatos { get; set; }
        public List<CompraEmpaques>? CompraEmpaques { get; set; }
        public List<CompraProductos>? CompraProductos { get; set; }
    }

}
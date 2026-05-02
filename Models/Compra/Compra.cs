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
        [Column("estado_compra")] public required decimal EstadoCompra { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Column("igv")] public required decimal Igv { get; set; }
        [Column("id_modificador")] public int? IdModificador { get; set; }
        [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }
        [Column("estado_meson")] public string? EstadoMeson { get; set; }
        [Column("serie_comprobante")] public string? SerieComprobante { get; set; }
        [Column("numero_comprobante")] public string? NumeroComprobante { get; set; }
        [Column("guia")] public string? Guia { get; set; }
        [Column("cod_fac_qbd")] public string? CodFacQBD { get; set; }
        [Column("fecha_factura")] public DateTime? FechaFactura { get; set; }
        [Column("img_factura")] public string? ImgFactura { get; set; }
        [Column("tipo_comprobante")] public string? TipoComprobante { get; set; }
        [Column("isc")] public int Isc { get; set; }
        [Column("icbp")] public int Icbp { get; set; }
        [Column("valor")] public int Valor { get; set; }
        [Column("total")] public int Total { get; set; }

        public Proveedor? Proveedor { get; set; }
        public Sede? Sede { get; set; }
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public List<DetalleCompra>? DetalleOrdenCompras { get; set; }
    }


    public class ListadoOrdenCompra
    {
        public int Id { get; set; }
        public required string CUO { get; set; }
        public required string Fecha { get; set; }
        public required string Serie { get; set; }
        public required string Numero { get; set; }
        public required string RUC { get; set; }
        public required string Denominacion { get; set; }
        public required string Valor { get; set; }
        public required string Total { get; set; }
        public required string Moneda { get; set; }
        public required string Estado { get; set; }
        public required string CodFac { get; set; }
        public required string Familia { get; set; }
        public required string Factura { get; set; }
        public string? Modalidad { get; set; }
        public required string EstadoOrdenCompra { get; set; }
        public required string Usuario { get; set; }
        public string? EstadoMeson { get; set; }
    }
    public class OrdenCompraCreateReq
    {
        public required int IdProveedor { get; set; }
        public required string Modalidad { get; set; }
        public required string Moneda { get; set; }
        public required decimal TipoCambio { get; set; }
        public required decimal Impuesto { get; set; }
        public required DateTime FechaEmision { get; set; }
        public required string Observaciones { get; set; }
        public int IdFamilia { get; set; }
        public required int IdSede { get; set; }
        public required int CreadorId { get; set; }
        public required string TipoTributario { get; set; }
        public string? TipoOperacion { get; set; }
        public bool IncluyeImpuesto { get; set; }
        public required int ModificadorId { get; set; }
        public required List<OrdenCompraCreateReq2> Detalle { get; set; }
    }
    public class OrdenCompraCreateReq2
    {
        public required int IdInsumo { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string UM { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
    }
    public class OrdenCompraUpdateReq
    {
        public required int IdProveedor { get; set; }
        public required string Modalidad { get; set; }
        public required string Moneda { get; set; }
        public required decimal TipoCambio { get; set; }
        public required decimal Impuesto { get; set; }
        public required DateTime FechaEmision { get; set; }
        public required string Observaciones { get; set; }
        public int IdFamilia { get; set; }
        public required int IdSede { get; set; }
        public string? TipoOperacion { get; set; }
        public bool IncluyeImpuesto { get; set; }
        public required string TipoTributario { get; set; }
        public int ModificadorId { get; set; }
    }
    public class PatchMesonDto
    {
        public string EstadoMeson { get; set; }
    }
    public class PatchPagoDto
    {
        public string EstadoPago { get; set; }
    }
}
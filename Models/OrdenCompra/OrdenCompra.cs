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
    [Table("orden_compra")]
    public class OrdenCompra
    {
        [Key]
        [Column("id_orden_compra")] public int IdOrdenCompra { get; set; }
        [Column("id_proveedor")] public int IdProveedor { get; set; }
        [Column("moneda")] public required string Moneda { get; set; }
        [Column("tipo_cambio")] public required decimal TipoCambio { get; set; }
        [Column("impuesto")] public required decimal Impuesto { get; set; }
        [Column("fecha_emision")] public required DateTime FechaEmision { get; set; }
        [Column("modalidad")] public required string Modalidad { get; set; }
        [Column("observaciones")] public required string Observaciones { get; set; }
        [Column("familia")] public required string Familia { get; set; }
        [Column("id_sede")] public required int IdSede { get; set; }
        [Column("estado")] public required string Estado { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Column("tipo_tributario")] public required string TipoTributario { get; set; }
        [JsonIgnore]
        public Proveedor? Proveedor { get; set; }
        [JsonIgnore]
        public Sede? Sede { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        public List<DetalleOrdenCompra>? DetalleOrdenCompras { get; set; }
        [JsonIgnore]
        public Compra? Compra { get; set; }
    }
    public class ListadoOrdenCompra
    {
        public required string CUO { get; set; }
        public required string Fecha { get; set; }
        public required string RUC { get; set; }
        public required string Denominacion { get; set; }
        public required string Valor { get; set; }
        public required string Total { get; set; }
        public required string Moneda { get; set; }
        public required string Estado { get; set; }
        public required string CodFac { get; set; }
        public required string Familia { get; set; }
        public required string Factura { get; set; }
        public required string EstadoOrdenCompra { get; set; }
        public required string Usuario { get; set; }
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
        public required string Familia { get; set; }
        public required int IdSede { get; set; }
        public required int IdCreador { get; set; }
        public required string TipoTributario { get; set; }
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
}
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
    [Table("detalle_orden_compra")]
    public class DetalleOrdenCompra
    {
        [Column("id_orden_compra")] public required int IdOrdenCompra { get; set; }
        [Column("id_insumo")] public required  int IdInsumo { get; set; }
        [Column("descripcion_fac")] public required  string DescripcionFac { get; set; }
        [Column("cantidad")] public required  decimal Cantidad { get; set; }
        [Column("um")] public required string Um { get; set; }
        [Column("costo_unitario")] public required decimal CostoUnitario { get; set; }
        [Column("costo_total")] public required decimal CostoTotal { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("fecha_modificacion")] public DateTime FechaModificacion { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Column("id_modificador")] public int IdModificador { get; set; }
        [JsonIgnore]
        public Usuario? Usuario { get; set; }
        [JsonIgnore]
        public Insumo? Insumo { get; set; }
        [JsonIgnore]
        public OrdenCompra? OrdenCompra { get; set; }
    }
    public class DetalleOrdenCompraRes
    {
        public required string Modalidad { get; set; }
        public required string TC { get; set; }
        public required string Destino { get; set; }
        public required string Direccion { get; set; }
        public List<DetalleOrdenCompra2>? DetalleOrdenCompras { get; set; }
    }
    public class DetalleOrdenCompra2
    {
        public required string Codigo { get; set; }
        public required string DescripcionQBD { get; set; }
        public required string DescripcionFactura { get; set; }
        public required string Cantidad { get; set; }
        public required string UM { get; set; }
        public required string CUnitario { get; set; }
        public required string CTotal { get; set; }
    }
    
}
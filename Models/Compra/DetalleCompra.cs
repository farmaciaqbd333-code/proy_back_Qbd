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
    [Table("detalle_compra")]
    public class DetalleCompra
    {

        [Column("id_compra")] public required int IdCompra { get; set; }
        [Column("id_insumo")] public required int IdInsumo { get; set; }
        [Column("cantidad_solicitada")] public required decimal CantidadSolicitada { get; set; }
        [Column("lote")] public string? Lote { get; set; }
        [Column("potencia")] public decimal Potencia { get; set; }
        [Column("fecha_fabricacion")] public DateTime FechaFabricacion { get; set; }
        [Column("fecha_vencimiento")] public DateTime FechaVencimiento { get; set; }
        [Column("coa")] public bool Coa { get; set; }
        [Column("registro_sanitario")] public string? RegistroSanitario { get; set; }
        [Column("conformidad")] public bool? Conformidad { get; set; }
        [Column("fecha_creacion")] public DateTime FechaCreacion { get; set; }
        [Column("id_creador")] public required int IdCreador { get; set; }
        [Key][Column("id")] public int Id { get; set; }
        [Column("fecha_modificacion")] public DateTime? FechaModificacion { get; set; }
        [Column("id_modificador")] public int? IdModificador { get; set; }
        [Column("condicion_almacenamiento")] public string? CondicionAlmacenamiento { get; set; }
        [Column("costo_unitario")] public decimal CostoUnitario { get; set; }
        [Column("costo_total")] public decimal CostoTotal { get; set; }
        [Column("um")] public required string Um { get; set; }
        [Column("id_familia")] public int? IdFamilia { get; set; }
        [Column("descripcion_fac")] public required string DescripcionFac { get; set; }

        public Familia? Familia { get; set; }
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public Insumo? Insumo { get; set; }
        public Compra? Compra { get; set; }
    }

}
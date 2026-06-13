using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using proy_back_Qbd.Models;

namespace Proy_back_QBD.Models
{

    [Table("productos")]
    public class Producto
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }                          // ID único del pedido
        [Column("descripcion")]
        public string? Descripcion { get; set; }                    // Costo del pedido
        [Column("costo")]
        public decimal? Costo { get; set; }                   // g/ml (gramos por mililitro)
        [Column("url_imagen")]
        public string? UrlImagen { get; set; }
        [Column("id_familia")]
        public int IdFamilia { get; set; }                   // g/ml (gramos por mililitro)
        [Column("fecha_creacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }           // Fecha de creación del pedido
        [Column("fecha_modificacion")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaModificacion { get; set; }       // Fecha de la última modificación del pedido
        [Column("creador_id")]
        public int? CreadorId { get; set; }
        [JsonIgnore]
        public Usuario? Creador { get; set; }
        [Column("modificador_id")]
        public int? ModificadorId { get; set; }
        [JsonIgnore]
        public Usuario? Modificador { get; set; }
        [JsonIgnore]
        public List<ProdTerm>? ProdTerm { get; set; }
        [JsonIgnore]
        public Familia? Familia { get; set; }
        public List<CompraProductos>? DetalleCompraProductos { get; set; }
    }

}
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
        [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }
        [Column("descripcion")] public string? Descripcion { get; set; }
        [Column("costo")] public decimal? Costo { get; set; }
        [Column("url_imagen")] public string? UrlImagen { get; set; }
        [Column("id_familia")] public int IdFamilia { get; set; }
        [Column("fecha_creacion"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTime FechaCreacion { get; set; }
        [Column("fecha_modificacion"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTime FechaModificacion { get; set; }
        [Column("creador_id")] public int? CreadorId { get; set; }
        [Column("modificador_id")] public int? ModificadorId { get; set; }
        [JsonIgnore] public Usuario? Creador { get; set; } = null!;
        [JsonIgnore] public Usuario? Modificador { get; set; } = null!;
        [JsonIgnore] public Familia? Familia { get; set; } = null!;
        [JsonIgnore] public List<ProdTerm>? ProdTerm { get; set; } = new List<ProdTerm>();
        [JsonIgnore] public List<CompraProductos>? CompraProductos { get; set; } = new List<CompraProductos>();
    }

}
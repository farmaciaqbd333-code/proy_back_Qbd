using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.ProductoIntermedio;

namespace Proy_back_QBD.Models
{
    [Table("empaques")]
    public class Empaque
    {
        [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }
        [Column("descripcion")] public string? Descripcion { get; set; }
        [Column("fundaId")] public int? IdFunda { get; set; }
        [Column("cajaId")] public int? IdCaja { get; set; }
        [Column("etiqueta_id1")] public int? IdEtiqueta1 { get; set; }
        [Column("etiqueta_id2")] public int? IdEtiqueta2 { get; set; }
        [Column("codigo")] public string? Codigo { get; set; }
        [Column("costo")] public decimal? Costo { get; set; }
        [Column("tara")] public string? Tara { get; set; }
        [Column("imagen_url")] public string? ImagenUrl { get; set; }
        [Column("fecha_modificacion"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTime FechaModificacion { get; set; }
        [Column("fecha_creacion"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTime FechaCreacion { get; set; }
        [Column("creador_id")] public int CreadorId { get; set; }
        [Column("id_familia")] public int IdFamilia { get; set; }
        [Column("modificador_id")] public int ModificadorId { get; set; }
        [Column("codigo_ubicacion")] public string? CodigoUbicacion { get; set; }
        public Empaque? Funda { get; set; }
        public Empaque? Caja { get; set; }
        public Empaque? Etiqueta1 { get; set; }
        public Empaque? Etiqueta2 { get; set; }
        public Usuario? Creador { get; set; }
        public Usuario? Modificador { get; set; }
        public Familia? Familia { get; set; }
        public List<CompraEmpaque>? CompraEmpaques { get; set; }
        public List<Empaque>? ListaCajas { get; set; }
        public List<Empaque>? ListaFundas { get; set; }
        public List<Empaque>? ListaEtiquetas1 { get; set; }
        public List<Empaque>? ListaEtiquetas2 { get; set; }
        public List<NotaSalidaEmpaque>? DetalleNotaSalidaEmpaques { get; set; }
        public List<EmpaqueProductoIntermedio>? EmpaqueProductoIntermedios { get; set; }
    }
}
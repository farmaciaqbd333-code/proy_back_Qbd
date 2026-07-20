using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.ProductoIntermedio;

namespace Proy_back_QBD.Models
{

    [Table("insumos")]
    public class Insumo
    {
        [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int Id { get; set; }
        [Column("descripcion")] public required string Descripcion { get; set; }
        [Column("um")] public required string UnidadMedida { get; set; }
        [Column("fc")] public required string FactorCorreccion { get; set; }
        [Column("dil")] public string? Dilucion { get; set; } = "";
        [Column("fecha_creacion"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTime FechaCreacion { get; set; }
        [Column("fecha_modificacion"), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public DateTime FechaModificacion { get; set; }
        [Column("creador_id")] public int CreadorId { get; set; }
        [Column("modificador_id")] public int ModificadorId { get; set; }
        [Column("id_familia")] public int? IdFamilia { get; set; }
        [Column("costo")] public decimal? Costo { get; set; }
        [Column("forma_f")] public string? FormaFarmaceutica { get; set; } = "";
        [Column("numero_cas")] public string? NumeroCas { get; set; }
        [Column("base")] public decimal? Base { get; set; }
        [Column("uso_min")] public decimal? UsoMin { get; set; }
        [Column("sal")] public decimal? Sal { get; set; }
        [Column("uso_max")] public decimal? UsoMax { get; set; }
        [Column("factor_e")] public decimal? FactorE { get; set; }
        [Column("precio_costo")] public decimal? PrecioCosto { get; set; }
        [Column("precio_venta")] public decimal? PrecioVenta { get; set; }
        [Column("higroscopico")] public bool? Higroscopico { get; set; } = false;
        [Column("fotosensible")] public bool? Fotosensible { get; set; } = false;
        [Column("refrigerado")] public bool? Refrigerado { get; set; } = false;
        [Column("pdf")] public string? Pdf { get; set; }
        [Column("densidad")] public decimal? Densidad { get; set; }
        [Column("codigo_ubicacion")] public string? CodigoUbicacion { get; set; }
        [Column("clasificacion")] public string? Clasificacion { get; set; } = "MP";
        [Column("tipo")] public string? Tipo { get; set; }
        public Usuario Creador { get; set; } = null!;
        public Usuario? Modificador { get; set; } = null!;
        public Familia Familia { get; set; } = null!;
        public List<FormulaCC> FormulasCC { get; set; } = new();
        public List<CompraInsumos> CompraInsumos { get; set; } = new();
        public List<ProductoIntermedio> ProductoIntermedio { get; set; } = new();
        public List<InsumoProductoIntermedio> InsumoProductoIntermedio { get; set; } = new();
        public List<NotaSalidaInsumo> NotaSalidaInsumos { get; set; } = new();

    }

}
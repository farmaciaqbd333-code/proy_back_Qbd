using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Models
{
    /// <summary>
    /// Lista de precios (Excel PREC. AV). UNA SOLA TABLA (lista_precios_av): cada producto tiene 4 listas,
    /// cada una con su propio código y su propio precio:
    ///  - Lista S → código S / precio P
    ///  - Lista A → código A / precio MED (médico)
    ///  - Lista B → código B / precio TERC (tercero)
    ///  - Lista C → código C / precio PUB (público)
    /// </summary>
    [Table("lista_precios_av")]
    public class ListaPrecio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo_s")]
        public string? CodigoS { get; set; }

        [Column("codigo_a")]
        public string? CodigoA { get; set; }

        [Column("codigo_b")]
        public string? CodigoB { get; set; }

        [Column("codigo_c")]
        public string? CodigoC { get; set; }

        /// <summary>Nombre del producto (columna PROD.).</summary>
        [Column("producto")]
        public string Producto { get; set; } = "";

        /// <summary>Ingredientes farmacéuticos activos (columna IFAS).</summary>
        [Column("ifas")]
        public string? Ifas { get; set; }

        /// <summary>Precio P (lista S).</summary>
        [Column("precio_s")]
        public decimal? PrecioS { get; set; }

        /// <summary>Precio MED (lista A).</summary>
        [Column("precio_a")]
        public decimal? PrecioA { get; set; }

        /// <summary>Precio TERC (lista B).</summary>
        [Column("precio_b")]
        public decimal? PrecioB { get; set; }

        /// <summary>Precio PUB (lista C).</summary>
        [Column("precio_c")]
        public decimal? PrecioC { get; set; }

        /// <summary>Categoría: FM (fórmula magistral), FMVET (fórmula veterinaria), OFI (oficinal) u otra.</summary>
        [Column("categoria")]
        public string Categoria { get; set; } = "FM";

        [Column("especialidad1")]
        public string? Especialidad1 { get; set; }

        [Column("especialidad2")]
        public string? Especialidad2 { get; set; }

        [Column("especialidad3")]
        public string? Especialidad3 { get; set; }

        /// <summary>Enfermedades para las que se usa (ej: ACNÉ, PSORIASIS, HERPES), separadas por coma.</summary>
        [Column("enfermedades")]
        public string? Enfermedades { get; set; }

        [Column("indicaciones")]
        public string? Indicaciones { get; set; }

        /// <summary>Forma farmacéutica (crema, gel, jarabe, cápsulas...).</summary>
        [Column("forma_farmaceutica")]
        public string? FormaFarmaceutica { get; set; }

        /// <summary>Modo de uso / posología (ej: 2 VECES POR SEMANA...).</summary>
        [Column("modo_uso")]
        public string? ModoUso { get; set; }

        /// <summary>Precauciones o contraindicaciones.</summary>
        [Column("precauciones")]
        public string? Precauciones { get; set; }

        /// <summary>Observaciones adicionales.</summary>
        [Column("observaciones")]
        public string? Observaciones { get; set; }

        [Column("presentacion")]
        public string? Presentacion { get; set; }

        [Column("fecha_ingreso")]
        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        [Column("fecha_modificacion")]
        public DateTime? FechaModificacion { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }
}

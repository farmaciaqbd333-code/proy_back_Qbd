using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Models
{
    /// <summary>
    /// Cálculo de pago de un colaborador en un mes. Las marcas de la tabla asistencias no se modifican:
    /// las correcciones del administrador se guardan aquí, en Datos.
    /// </summary>
    [Table("calculos_pago")]
    public class CalculoPago
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("usuario_id")]
        public int UsuarioId { get; set; }
        [Column("anio")]
        public int Anio { get; set; }
        [Column("mes")]
        public int Mes { get; set; }
        [Column("datos")]
        public string Datos { get; set; } = "{}";
        [Column("pago_final")]
        public decimal? PagoFinal { get; set; }
        [Column("modificador_id")]
        public int ModificadorId { get; set; }
        [Column("fecha_modificacion")]
        public DateTime FechaModificacion { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Request
{
    public class AsistenciaCreateReq
    {
        public string? Tipo { get; set; }  // Puede ser nulo
        public string? Observacion { get; set; }  // Puede ser nulo
        public int? CreadorId { get; set; }  // Puede ser nulo
        public int? SedeId { get; set; }  // Puede ser nulo
        // Quien tiene la sesión; lo asigna el controlador, nunca se toma del cliente
        [System.Text.Json.Serialization.JsonIgnore]
        public int? RegistradoPorId { get; set; }
    }
    public class CalculoPagoReq
    {
        [Required]
        public string Datos { get; set; } = string.Empty;
        public decimal? PagoFinal { get; set; }
    }
    public class AsistenciaByIdReq
    {
        [Required]
        public int Año { get; set; }
        [Required]
        public int Mes { get; set; }
    }
}
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
    }
    public class AsistenciaByIdReq
    {
        [Required]
        public int Año { get; set; }
        [Required]
        public int Mes { get; set; }
    }
}
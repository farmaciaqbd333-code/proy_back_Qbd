using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{
    public class FormulaCCUpdReqP
    {
        public required List<FormulaCCUpdReq> FormulaCCs { get; set; }
        public required string Procedimiento { get; set; }
        public required int EmpaqueId { get; set; }

    }
    public class FormulaCCUpdReq
    {

        public required int InsumoId { get; set; }
        public decimal Porcentaje { get; set; }
        public required string? Variable { get; set; }
        public decimal CantidadU { get; set; }
        public decimal CantidadL { get; set; }
        public decimal Practica { get; set; }
        public bool? CSP { get; set; }
        public int CreadorId { get; set; }
        public int ModificadorId { get; set; }
    }

}
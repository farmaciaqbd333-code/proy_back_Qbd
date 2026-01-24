using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Auxiliares
{
    public class FormulaRCreReq
    {

        public required FormularRReq FormulaR { get; set; }
        public required List<InsumoRCreateReq> InsumosR { get; set; }

    }
    public class FormularRReq
    {
        public required string Descripcion { get; set; }  // Puede ser nulo
        public string? Empaque { get; set; }  // Puede ser nulo
        public string? Procedimiento { get; set; }  // Puede ser nulo
        public string? Aspecto { get; set; }  // Puede ser nulo
        public string? Color { get; set; }  // Puede ser nulo
        public string? Olor { get; set; }  // Puede ser nulo
        public string? Ph { get; set; }  // Puede ser nulo
        public int CreadorId { get; set; }
        public int? SedeId { get; set; }  // Puede ser nulo
    }
    public class FormularRUpdTReq
    {
        public required string Descripcion { get; set; }  // Puede ser nulo
        public string? Empaque { get; set; }  // Puede ser nulo
        public string? Procedimiento { get; set; }  // Puede ser nulo
        public string? Aspecto { get; set; }  // Puede ser nulo
        public string? Color { get; set; }  // Puede ser nulo
        public string? Olor { get; set; }  // Puede ser nulo
        public string? Ph { get; set; }  // Puede ser nulo
        public int ModificadorId { get; set; }
        public int? SedeId { get; set; }  // Puede ser nulo
    }
    public class FormulaRUpdReq
    {
        public required FormularRUpdTReq FormulaR { get; set; }
        public required List<InsumoRUpdateReq> InsumosR { get; set; }
        // public required string Descripcion { get; set; }  // Puede ser nulo
        // public string? Empaque { get; set; }  // Puede ser nulo
        // public string? Procedimiento { get; set; }  // Puede ser nulo
        // public string? Aspecto { get; set; }  // Puede ser nulo
        // public string? Color { get; set; }  // Puede ser nulo
        // public string? Olor { get; set; }  // Puede ser nulo
        // public string? Ph { get; set; }  // Puede ser nulo
        // public int ModificadorId { get; set; }
        // public int? SedeId { get; set; }  // Puede ser nulo

    }
    public class FormularRReq2
    {
        public int Id { get; set; }  // Puede ser nulo
        public required string Descripcion { get; set; }  // Puede ser nulo
        public string? Empaque { get; set; }  // Puede ser nulo
        public string? Procedimiento { get; set; }  // Puede ser nulo
        public string? Aspecto { get; set; }  // Puede ser nulo
        public string? Color { get; set; }  // Puede ser nulo
        public string? Olor { get; set; }  // Puede ser nulo
        public string? Ph { get; set; }  // Puede ser nulo
        public int ModificadorId { get; set; }
        public int? SedeId { get; set; }  // Puede ser nulo
    }
}
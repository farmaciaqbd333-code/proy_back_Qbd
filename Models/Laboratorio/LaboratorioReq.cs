using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Dto.Productos
{
    public class FormLabIns
    {

        public required LabCreReq Lab { get; set; }
        public required List<InsumsCreReq> Ins { get; set; }

    }

    public class LabCreReq
    {
        public int FormulaId { get; set; }
        public int SedeId { get; set; }
        public DateOnly FechaEmision { get; set; }
        public DateOnly FechaVcto { get; set; }
        public int? Elaborado { get; set; }
        public int? Autorizado { get; set; }
        public string? Procedimiento { get; set; }
        public int? EmpaqueId { get; set; }
        public int? CantiTermo { get; set; }
        public string? CodAdicional { get; set; }
        public string? Etiqueta { get; set; }
        public string? Etiqueta2 { get; set; }
        public string? Aspecto { get; set; }
        public string? Color { get; set; }
        public string? Olor { get; set; }
        public string? Ph { get; set; }
        public int CreadorId { get; set; }
    }
    public class InsumsCreReq
    {
        public required int InsumoId { get; set; }
        public required string Porcentaje { get; set; }
        public required string? Variable { get; set; }
        public required string CantidadU { get; set; }
        public required string CantidadL { get; set; }
        public string? Practica { get; set; }
        public bool? CSP { get; set; }
        public int CreadorId { get; set; }
    }
}
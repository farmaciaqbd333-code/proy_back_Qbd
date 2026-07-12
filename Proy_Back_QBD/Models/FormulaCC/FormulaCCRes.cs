using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Proy_back_QBD.Models
{
    public class FormulaCCLabRes
    {
        public string CodigoPedido { get; set; }
        public string DniPaciente { get; set; }
        public string NombreCompleto { get; set; }
        public string EdadPaciente { get; set; }
        public string CMP { get; set; }
        public string NombreCompletoMed { get; set; }
        public int FormulaId { get; set; }
        public string FormulaMagistral { get; set; }
        public string FormaFarmaceutica { get; set; }
        public string Lote { get; set; }
        public string NroReg { get; set; }
        public DateOnly FechaEmision { get; set; }
        public DateOnly FechaVcto { get; set; }
        public int Cantidad { get; set; }
        public string GPorMl { get; set; }
        public int? Elaborado { get; set; }
        public int? Autorizado { get; set; }
        public string UnidadMedida { get; set; }
        public decimal CostoTotal { get; set; }
        public List<FormulaCCLabSubRes> insumos { get; set; }
        public int? EmpaqueId { get; set; }
        public string? Procedimiento { get; set; }
        public string? Diagnostico { get; set; }
        public string? ZonaAplicacion { get; set; }
    }
    public class FormulaCCLabSubRes
    {

        public int InsumoId  { get; set; }
        public string Porcentaje { get; set; }
        public string Variable { get; set; }
        public string Practica { get; set; }
        public bool? CSP { get; set; }

    }

}
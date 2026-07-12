using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Dto.Productos
{
    public class PedidoLab
    {
        public int? LabId { get; set; }                          // ID único del pedido
        public int? PedidoId { get; set; }                          // ID único del pedido
        public DateTime? Fecha { get; set; }                    // Costo del pedido
        public string? DNI { get; set; }                     // Cantidad de unidades solicitadas
        public string? Paciente { get; set; }                   // g/ml (gramos por mililitro)
        public string? FormulaMagistral { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Registro { get; set; }               // Diagnóstico relacionado al pedido
        public string? Elaborado { get; set; }
    }

    public class LabFindPedIdRes
    {
        public string? DNI { get; set; }
        public string? Paciente { get; set; }
        public string? Edad { get; set; }
        public string? CMP { get; set; }
        public string? Medico { get; set; }
        public List<LabForm>? Formulas { get; set; }

    }

    public class LabForm()
    {
        public int? Id { get; set; }
        public string? FormulaM { get; set; }
        public string? FormaF { get; set; }
        public string? Lote { get; set; }
        public int? Cantidad { get; set; }
        public string? GPorMl { get; set; }
        public string? UnidadMedida { get; set; }
        public string? Registro { get; set; }
        public string? Diagnostico { get; set; }
        public string? ZonaAplicacion { get; set; }
        public decimal? CostoTotal { get; set; }
    }
    

}
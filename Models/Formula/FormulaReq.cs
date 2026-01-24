using System.Text.Json.Serialization;

namespace Proy_back_QBD.Dto.Request
{
    public class FormulaCreatePedido
    {
        public decimal? Costo { get; set; }                    // Costo del pedido
        public int? Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public string? FormulaMagistral { get; set; }          // Descripción de la fórmula magistral
        public string? FormaFarmaceutica { get; set; }           // Descripción de la fórmula de farmacia
        public string? GPorMl { get; set; }                   // g/ml (gramos por mililitro)
        public string? UnidadMedida { get; set; }              // Unidad de medida (ej. "ml", "mg", "g", etc.)
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Reportado { get; set; }                   // Si ha sido reportado o no (valor booleano)
        public int CreadorId { get; set; }
    }
    
        public class FormulaCreateReq
    {
        public int? PedidoId { get; set; }                    // Costo del pedido
        public int? SedeId { get; set; }                    // Costo del pedido
        public decimal? Costo { get; set; }          
        public int? Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public string? FormulaMagistral { get; set; }
        public string? FormaFarmaceutica { get; set; }           // Descripción de la fórmula de farmacia
        public string? GPorMl { get; set; }                   // g/ml (gramos por mililitro)
        public string? UnidadMedida { get; set; }              // Unidad de medida (ej. "ml", "mg", "g", etc.)
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Reportado { get; set; }                   // Si ha sido reportado o no (valor booleano)
        public int CreadorId { get; set; }
    }

    public class FormulaUpdateReq
    {
        public decimal Costo { get; set; }                    // Costo del pedido
        public int Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public string? FormulaMagistral { get; set; }          // Descripción de la fórmula magistral
        public string? FormaFarmaceutica { get; set; }           // Descripción de la fórmula de farmacia
        public string? GPorMl { get; set; }                   // g/ml (gramos por mililitro)
        public string? UnidadMedida { get; set; }              // Unidad de medida (ej. "ml", "mg", "g", etc.)
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Estado { get; set; }
        public string? Reportado { get; set; }                   // Si ha sido reportado o no (valor booleano)
        public int ModificadorId { get; set; }
    }
    public class FormulaUpdLabReq
    {
        public decimal Costo { get; set; }                    // Costo del pedido
        public int Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public string? FormulaMagistral { get; set; }
        public string? FormaFarmaceutica { get; set; }
        public string GPorMl { get; set; }
        public string Diagnostico { get; set; }
        public string ZonaAplicacion { get; set; }
        public string? UnidadMedida { get; set; }
        public int? Autorizado { get; set; }
        public int? Elaborado { get; set; }
        public DateOnly FechaEmision { get; set; }
        public DateOnly FechaVcto { get; set; }
        public int ModificadorId { get; set; }
    }
    public class FormulaCambiarTipo
    {
                 // Cantidad de unidades solicitadas
        public required string Tipo { get; set; }
        public required List<int> Lista { get; set; }


    }
}

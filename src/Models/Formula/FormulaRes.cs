using System.Text.Json.Serialization;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Response
{
    public class FormulaFindAllResponse
    {
        public string? Cuo { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string? Dni { get; set; }
        public string? Paciente { get; set; }
        public string? Celular { get; set; }
        public string? Medico { get; set; }
        public string? Usuario { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? BolFaC { get; set; }
    }
    public class FormulasByPedido
    {
        public int? Id { get; set; }
        public string? Codigo { get; set; }
        public decimal? Costo { get; set; }                    // Costo del pedido
        public int? Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public string? FormulaMagistral { get; set; }          // Descripción de la fórmula magistral
        public string? FormaFarmaceutica { get; set; }           // Descripción de la fórmula de farmacia
        public string? GPorMl { get; set; }                   // g/ml (gramos por mililitro)
        public string? UnidadMedida { get; set; }              // Unidad de medida (ej. "ml", "mg", "g", etc.)
        public string? Lote { get; set; }                      // Lote del producto
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Estado { get; set; }
        public string? Reportado { get; set; }                   // Si ha sido reportado o no (valor booleano)
        public string? Inserto { get; set; }                   // Si ha sido reportado o no (valor booleano)
    }
    public class FormulaCreateResponse
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Formula? FormulaRes { get; set; }
    }
    public class FormulaUpdateResponse
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Formula? FormulaRes { get; set; }
    }

    public class FormulaFindIdResponse
    {
        public int? Id { get; set; }  // Puede ser nulo
        public int? Apoderado { get; set; }
        public string? DniApoderado { get; set; }
        public PersonaRes? PersonaFk { get; set; }  // Puede ser nulo
        public bool? CondicionFecha { get; set; }
    }
    public class FormulasLab
    {
        public string? Paciente { get; set; }  // Puede ser nulo
        public string? DNI { get; set; }
        public string? Edad { get; set; }
        public string? Medico { get; set; }
        public string? CMP { get; set; }
        public List<FormulasLab2>? Formulas { get; set; }
        public int? Items { get; set; }
        public decimal? CantidadTotal { get; set; }
        public decimal? CostoTotal { get; set; }
    }
    public class InsertoRes
    {
        public string? Inserto { get; set; }  // Puede ser nulo
    }
    public class FormulasLab2
    {
        public decimal? Costo { get; set; }  // Puede ser nulo
        public int? Cantidad { get; set; }
        public string? FormulaMagistral { get; set; }
        public string? GPorMl { get; set; }
        public string? NReg { get; set; }
        public DateOnly? FechaEmision { get; set; }
        public DateOnly? FechaVcto { get; set; }
        public string? Lote { get; set; }
        public string? Diagnostico { get; set; }
        public string? Zona { get; set; }
    }
    public class RecetaRes
    {
        public int? FormulasId { get; set; }
        public string? Medico { get; set; }  // Puede ser nulo
        public DateOnly? Fecha { get; set; }
        public string? Prescripcion { get; set; }
        public string? Gram { get; set; }  // Puede ser nulo
        public int? Cant { get; set; }
        public string? Mili { get; set; }
        public string? Gotas { get; set; }
        public string? Observacion { get; set; }
        public decimal? Precio { get; set; }
        public string? Tipo { get; set; }
    }
    public class EtiquetaRes
    {
        public string? NReg { get; set; }
        public string? DNI { get; set; }
        public string? Paciente { get; set; }
        public string? FormulaMagistral { get; set; }
        public string? FechaEmision { get; set; }
        public string? FechaVencimiento { get; set; }
        public string? CMP { get; set; }
        public string? CQFP { get; set; }
        public string? Medico { get; set; }
        public string? Direccion { get; set; }
        public string? AutorizadoPor { get; set; }
    }
    public class DetallesRes
    {
        public string? Paciente { get; set; }
        public string? Edad { get; set; }
        public string? Diagnostico { get; set; }
        public string? QFDT { get; set; }
        public string? QFBD { get; set; }
        public string? CQFP_DT { get; set; }
        public string? CQFP_BD { get; set; }
        public string? Formula { get; set; }
        public string? Registro { get; set; }
        public string? Cantidad { get; set; }
        public int? EmpaqueId { get; set; }
        public string? CMP { get; set; }
        public string? Medico { get; set; }
        public decimal CostoTotal { get; set; }
        public List<DetallesRes2>? Insumos { get; set; }
        public int? Items { get; set; }
        public decimal Total { get; set; }
        public decimal TotalCantXUND { get; set; }
        public decimal TotalCantXLOT { get; set; }
    }
    public class DetallesRes2
    {
        public string? CODQBD { get; set; }
        public decimal Porcentaje { get; set; }
        public string? Descripcion { get; set; }
        public string? Variable { get; set; }
        public decimal CantUND { get; set; }
        public string? FC { get; set; }
        public string? Dilucion { get; set; }
        public string? UM { get; set; }
        public decimal CantLot { get; set; }
        public decimal? Pract { get; set; }

    }
}

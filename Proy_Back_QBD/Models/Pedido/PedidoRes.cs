using System.Text.Json.Serialization;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Response
{
    public class PedidoFindAllResponse
    {
        public int? Id { get; set; }
        public string? Cuo { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string? Recibo { get; set; }
        public string? Dni { get; set; }
        public string? DniApoderado { get; set; }
        public string? Paciente { get; set; }
        public string? Especialidad { get; set; }
        public string? Edad { get; set; }
        public int? PacienteId { get; set; }
        public string? Celular { get; set; }
        public string? Medico { get; set; }
        public string? Cmp { get; set; }
        public decimal? Total { get; set; }
        public decimal? Adelanto { get; set; }
        public decimal? Saldo { get; set; }
        public string? Estado { get; set; }
        public DateTime FechaEntrega { get; set; }
        public string? Usuario { get; set; }
        public string? ComprobanteElectronico { get; set; }
    }
    public class PedidoLabFindAllRes
    {
        public int? Id { get; set; }
        public string? Cuo { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public string? Dni { get; set; }
        public string? Paciente { get; set; }
        public string? FormulaMagistral { get; set; }
        public string? Registro { get; set; }
        public string? Elaborado { get; set; }
    }
    public class PedidoLabFindAllRes2
    {
        public int? Id { get; set; }
        public string? Cuo { get; set; }
    }

    public class PedidoCreateRes
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Pedido? PedidoRes { get; set; }
    }
    public class PedidoListaRes
    {
        public int? Id { get; set; }  // Puede ser nulo      
        public string? Codigo { get; set; }
    }
    public class PedidoUpdateResponse
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Pedido? PedidoRes { get; set; }
    }

    public class PedidoFindIdResponse
    {
        public int? Id { get; set; }
        public string? Periodo { get; set; }
        public string? Recibo { get; set; }
        public string? Img1 { get; set; }
        public string? Img2 { get; set; }
        public string? Img3 { get; set; }
        public string? Img4 { get; set; }
        public string? Img5 { get; set; }
        public string? Img6 { get; set; }
        public string? ComprobanteElectronico { get; set; }
        public string? FechaEntrega { get; set; }
        public int? MedicoId { get; set; }
        public int? PacienteId { get; set; }        
        public List<FormulasByPedido>? Formulas { get; set; }
        public List<ProdTermPedido>? ProdTerms { get; set; }
    }



}
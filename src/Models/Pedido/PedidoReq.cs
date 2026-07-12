using System.Text.Json.Serialization;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Request
{
    public class PedidoCreateReq
    {

        public int PacienteId { get; set; }
        public int MedicoId { get; set; }
        public int SedeId { get; set; }
        public string? Recibo { get; set; }
        public string? Img1 { get; set; }
        public string? Img2 { get; set; }
        public string? Img3 { get; set; }
        public string? Img4 { get; set; }
        public string? Img5 { get; set; }
        public string? Img6 { get; set; }
        public string? ComprobanteElectronico { get; set; }
        public int CreadorId { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public List<FormulaCreatePedido>? Formulas { get; set; }
        public List<ProdTermPedidoReq>? ProductosTerminados { get; set; }
    }
    public class PedidoEstadoUpdate
    {
        public string? Estado { get; set; }
    }
    public class PedidoUpdateReq
    {
        public int PacienteId { get; set; }
        public int MedicoId { get; set; }    
        public string? Recibo { get; set; }    
        public string? Img1 { get; set; }
        public string? Img2 { get; set; }
        public string? Img3 { get; set; }
        public string? Img4 { get; set; }
        public string? Img5 { get; set; }
        public string? Img6 { get; set; }
        public string? ComprobanteElectronico { get; set; }
        public int ModificadorId { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaEntrega { get; set; }

    }
}

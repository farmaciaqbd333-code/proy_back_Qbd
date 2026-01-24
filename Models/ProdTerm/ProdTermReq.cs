using System.Text.Json.Serialization;

namespace Proy_back_QBD.Dto.Request
{
    public class ProdTermPedidoReq
    {
        public decimal? Costo { get; set; }                    // Costo del pedido
        public int? Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public int? ProductoId { get; set; }                     // Cantidad de unidades solicitadas
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public int CreadorId { get; set; }
    }
    public class ProdTermCreateReq
    {
        public decimal Costo { get; set; }                    // Costo del pedido
        public int Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public int? ProductoId { get; set; }                     // Cantidad de unidades solicitadas
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public int CreadorId { get; set; }
        public int PedidoId { get; set; }
        public int SedeId { get; set; }
    }
    public class ProdTermUpdateReq
    {
        public decimal Costo { get; set; }                    // Costo del pedido
        public int Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public int? ProductoId { get; set; }                     // Cantidad de unidades solicitadas
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public string? Estado { get; set; }                    // Estado del pedido (pendiente, procesado, entregado, etc.)
        public int ModificadorId { get; set; }
    }
}

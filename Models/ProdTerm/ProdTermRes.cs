using System.Text.Json.Serialization;

namespace Proy_back_QBD.Dto.Response
{
    public class ProdTermPedido()
    {
        public int? Id { get; set; }                          // ID único del pedido
        public decimal? Costo { get; set; }                    // Costo del pedido
        public int? Cantidad { get; set; }                     // Cantidad de unidades solicitadas
        public ProductoRes? Producto { get; set; }                   // g/ml (gramos por mililitro)
        public string? ZonaAplicacion { get; set; }              // Zona donde se aplica el tratamiento (si aplica)
        public string? Diagnostico { get; set; }               // Diagnóstico relacionado al pedido
        public string? Estado { get; set; }
    }         
}

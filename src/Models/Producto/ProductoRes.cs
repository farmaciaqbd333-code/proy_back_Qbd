using System.Text.Json.Serialization;

namespace Proy_back_QBD.Dto.Response
{
    public class ProductoRes()
    {
        public int? Id { get; set; }                          // ID único del pedido
        public string? Codigo { get; set; }   
        public string? Descripcion { get; set; }                     // Cantidad de unidades solicitadas
        public decimal? Costo { get; set; }
        public string? UrlImagen { get; set; }
    }         
}

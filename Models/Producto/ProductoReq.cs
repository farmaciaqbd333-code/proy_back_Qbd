using System.Text.Json.Serialization;

namespace Proy_back_QBD.Dto.Request
{
    public class ProductoReq
    {
        public string? Descripcion { get; set; }
        public decimal? Costo { get; set; }
        public int CreadorId { get; set; }
        public int ModificadorId { get; set; }
    }
}

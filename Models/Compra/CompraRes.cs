using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class ObtenerOrdenOCompraRes2
    {
        public required string TC { get; set; }
        public required string Destino { get; set; }
        public required string Direccion { get; set; }
        public List<DetInsumoRes2>? DetalleOrdenCompras { get; set; }
        public required int IdProveedor { get; set; }
        public bool IncluyeImpuesto { get; set; }
        public string? Observaciones { get; set; }
        public string? Familia { get; set; }
        public string? Responsable { get; set; }
        public int ISC { get; set; }
        public int ICBP { get; set; }
        public string? Guia { get; set; }
        public string? Modalidad { get; set; }
    }
}
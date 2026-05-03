using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class DetalleOrdenCompraRes
    {
        public required string Modalidad { get; set; }
        public required string Familia { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public required string TC { get; set; }
        public required string Moneda { get; set; } // Nuevo campo
        public required string Destino { get; set; }
        public required string Direccion { get; set; }
        public required string CodigoProveedor { get; set; }
        public string? Ruc { get; set; }
        public string? RazonSocial { get; set; }
        public string? Responsable { get; set; }
        public string? TipoOperacion { get; set; }
        public string? Observaciones { get; set; } // Nuevo campo requerido por el usuario
        public bool IncluyeImpuesto { get; set; }
        public List<DetalleOrdenCompra2>? DetalleOrdenCompras { get; set; }
    }
    public class DetalleOrdenCompra2
    {
        public int Id { get; set; } // ID primario de la fila
        public required int IdInsumo { get; set; }
        public required string Codigo { get; set; }
        public required string DescripcionQBD { get; set; }
        public required string DescripcionFactura { get; set; }
        public required string Cantidad { get; set; }
        public required string UM { get; set; }
        public required string CUnitario { get; set; }
        public required string CTotal { get; set; }
    }
}
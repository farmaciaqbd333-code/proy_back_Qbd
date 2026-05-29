using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class MesonModalRes
    {
        public int Id { get; set; }
        public required string CUO { get; set; }
        public DateTime? FechaEmision { get; set; }
        public string? SerieComprobante { get; set; }
        public string? NumeroComprobante { get; set; }
        public string? Guia { get; set; }
        public string? CodFacQBD { get; set; }
        public string? NombreProveedor { get; set; }
        public int? IdProveedor { get; set; }
        public required string Familia { get; set; }
        public List<DetalleMesonInsumoRes>? ListaInsumos { get; set; }
    }
    public class MesonDetalleRes
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
        public decimal ISC { get; set; }
        public decimal ICBP { get; set; }
        public string? Guia { get; set; }
        public string? Modalidad { get; set; }
    }
    public class MesonListaRes
    {
        public int Id { get; set; }
        public required string CUO { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public DateTime? FechaFactura { get; set; }
        public required string NombreProveedor { get; set; }
        public string? CodFacQbd { get; set; }
        public required string Familia { get; set; }
        public string? Factura { get; set; }
        public string? ImgFactura { get; set; }
        public string? Guia { get; set; }
        public required string EstadoCompra { get; set; }
    }
}
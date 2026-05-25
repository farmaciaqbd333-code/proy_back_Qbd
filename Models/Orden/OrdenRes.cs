using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class OrdenesYComprasRes
    {
        public int Id { get; set; }
        public required string CUO { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public required string NumProvedor { get; set; }
        public required string NombreProveedor { get; set; }
        public required decimal Valor { get; set; }
        public required decimal Total { get; set; }
        public required string Moneda { get; set; }
        public required string EstadoCompra { get; set; }
        public string? CodFacQbd { get; set; }
        public required string Familia { get; set; }
        public string? Factura { get; set; }
        public string? RutaFactura { get; set; }
        public required string EstadoPago { get; set; }
        public required string Usuario { get; set; }
    }
    public class OrdenCompraGetRes
    {
        public required string TC { get; set; }
        public required string Destino { get; set; }
        public required string Direccion { get; set; }
        public List<DetalleInsumosRes>? DetalleCompraInsumos { get; set; }
        public List<DetalleEmpaquesRes>? DetalleEmpaques { get; set; }
        public List<DetalleProductosRes>? DetalleProductos { get; set; }
        public List<DetalleEconomatosRes>? DetalleEconomatos { get; set; }
        public List<DetalleComprasRes>? DetalleCompras { get; set; }
        public required int IdProveedor { get; set; }
        public bool IncluyeImpuesto { get; set; }
        public string? Observaciones { get; set; }
        public string? Familia { get; set; }
        public string? Responsable { get; set; }
        public int ISC { get; set; }
        public int ICBP { get; set; }
        public string? Guia { get; set; }
        public string? Modalidad { get; set; }
        public string? EstadoCompra { get; set; }
    }
    public class DescripcionFacturaRes
    {
        public required string[] DescripcionFactura { get; set; }
        public Dictionary<int, string>? DescripcionPorInsumo { get; set; }
    }
    public class OrdenesEnviadasRes
    {
        public int Id { get; set; }
        public required string CUO { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public DateTime? FechaFactura { get; set; }
        public string? SerieComprobante { get; set; }
        public string? NumeroComprobante { get; set; }
        public string? Factura { get; set; }
        public required string Guia { get; set; }
        public string? CodFacQbd { get; set; }
        public required string NumProvedor { get; set; }
        public required string NombreProveedor { get; set; }
        public required string EstadoCompra { get; set; }
        public required string Usuario { get; set; }
        public required string Familia { get; set; }
        public string? RutaFactura { get; set; }
    }
    public class OrdenMesonRes
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
        public List<DetalleOrdenMesonRes>? Lista { get; set; }
    }
}
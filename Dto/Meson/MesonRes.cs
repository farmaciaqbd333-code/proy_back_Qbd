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
        public string? Destino { get; set; }
        public string? Direccion { get; set; }
        public List<DetalleMesonOtrosRes> ListaOtros { get; set; } = [];
        public List<DetalleMesonInsumoRes> ListaInsumos { get; set; } = [];
        public List<DetalleMesonEmpaquesRes> ListaEmpaques { get; set; } = [];
        public List<DetalleMesonProductosRes> ListaProductos { get; set; } = [];
        public List<DetalleMesonEconomatosRes> ListaEconomatos { get; set; } = [];
    }
    public class MesonDetalleRes
    {
        public required string TC { get; set; }
        public required string Destino { get; set; }
        public required string Direccion { get; set; }
        public List<CompraInsumoRes2>? DetalleOrdenCompras { get; set; }
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
        public string? CUO { get; set; }
        public DateTime? FechaCotizacion { get; set; }
        public DateTime? FechaFactura { get; set; }
        public string? NombreProveedor { get; set; }
        public string? CodFacQbd { get; set; }
        public string? Familia { get; set; }
        public string? Factura { get; set; }
        public string? ImgFactura { get; set; }
        public string? Guia { get; set; }
        public string? EstadoCompra { get; set; }
    }
    public class DetalleMesonInsumoRes
    {
        public int Id { get; set; }
        public string? Reg { get; set; }
        public string? Codigo { get; set; }
        public string? DescripcionQBD { get; set; }
        public string? DescripcionFactura { get; set; }
        public decimal? CantidadRecibida { get; set; }
        public string? Um { get; set; }
        public bool? Coa { get; set; }
        public string? Lote { get; set; }
        public string? RegistroSanitario { get; set; }
        public DateTime? FechaFabricacion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public bool? Conformidad { get; set; }
        public int? IdFabricante { get; set; }
        public string? Familia { get; set; }
    }
    public class DetalleMesonOtrosRes
    {
        public int Id { get; set; }
        public string? Reg { get; set; }
        public string? Familia { get; set; }
        public string? DescripcionFactura { get; set; }
        public decimal? CantidadRecibida { get; set; }
        public string? Um { get; set; }
        public bool? Conformidad { get; set; }
    }
    public class DetalleMesonEmpaquesRes
    {
        public int Id { get; set; }
        public string? Reg { get; set; }
        public string? Codigo { get; set; }
        public string? DescripcionQbd { get; set; }
        public string? DescripcionFactura { get; set; }
        public int? IdFabricante { get; set; }
        public decimal? CantidadRecibida { get; set; }
        public string? Um { get; set; }
        public bool? Coa { get; set; }
        public string? Lote { get; set; }
        public DateTime? FechaFabricacion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public bool? Conformidad { get; set; }
        public string? Familia { get; set; }
    }
    public class DetalleMesonEconomatosRes
    {
        public int Id { get; set; }
        public string? Reg { get; set; }
        public string? Codigo { get; set; }
        public string? DescripcionQBD { get; set; }
        public string? DescripcionFactura { get; set; }
        public int? IdFabricante { get; set; }
        public decimal? CantidadRecibida { get; set; }
        public string? Um { get; set; }
        public bool? Conformidad { get; set; }
        public string? Familia { get; set; }
    }
    public class DetalleMesonProductosRes
    {
        public int Id { get; set; }
        public string? Reg { get; set; }
        public string? Codigo { get; set; }
        public string? DescripcionQbd { get; set; }
        public string? DescripcionFactura { get; set; }
        public int? IdFabricante { get; set; }
        public decimal? CantidadRecibida { get; set; }
        public string? Um { get; set; }
        public string? Lote { get; set; }
        public string? RegistroSanitario { get; set; }
        public DateTime? FechaFabricacion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public bool? Conformidad { get; set; }
        public string? Familia { get; set; }
    }
}
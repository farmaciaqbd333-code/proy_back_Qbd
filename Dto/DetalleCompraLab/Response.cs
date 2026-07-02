using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public class ObtenerCompraLabRes
{
    public required string CodigoProveedor { get; set; }
    public string? Ruc { get; set; }
    public string? NumProvedor { get; set; }
    public List<CompraLabInsumoModalRes>? DetalleInsumos { get; set; }
    public List<CompraLabEmpaqueModalRes>? DetalleEmpaques { get; set; }
    public string? CodFacQbd { get; set; }
}
public class CompraLabInsumoModalRes
{
    public required int Id { get; set; }
    public required string Familia { get; set; }
    public required string Reg { get; set; }
    public required string Codigo { get; set; }
    public required string DescripcionQBD { get; set; }
    public required bool Coa { get; set; }
    public required string Lote { get; set; }
    public required string Um { get; set; }
    public required decimal CantidadRecibida { get; set; }
    public required decimal Potencia { get; set; }
    public DateTimeOffset? FechaFabricacion { get; set; }
    public DateTimeOffset? FechaVencimiento { get; set; }
    public required string CondicionALmacenamiento { get; set; }
    public required decimal TotalPaquetes { get; set; }
    public required decimal TotalPeso { get; set; }
    public string? Fabricante { get; set; }
    public decimal? Densidad { get; set; }
    public string? DescripcionFactura { get; set; }
}
public class CompraLabEmpaqueModalRes
{
    public required int Id { get; set; }
    public required string Familia { get; set; }
    public required string Reg { get; set; }
    public required string Codigo { get; set; }
    public required string DescripcionQBD { get; set; }
    public bool? Coa { get; set; }
    public required string Lote { get; set; }
    public required string Um { get; set; }
    public required decimal CantidadRecibida { get; set; }
    public DateTimeOffset? FechaFabricacion { get; set; }
    public DateTimeOffset? FechaVencimiento { get; set; }
    public required string CondicionALmacenamiento { get; set; }
    public required decimal TotalPaquetes { get; set; }
    public required decimal TotalPeso { get; set; }
    public string? Fabricante { get; set; }
    public string? DescripcionFactura { get; set; }
}
public class CompraLabDetIdRes
{
    public required string CodigoProveedor { get; set; }
    public string? Ruc { get; set; }
    public string? NumProvedor { get; set; }
    public List<CompraLabDetInsumosRes>? ListaInsumos { get; set; } = [];
    public List<CompraLabDetEmpRes>? ListaEmpaques { get; set; } = [];
    public string? CodFacQbd { get; set; }
}
public class CompraLabDetInsumosRes
{
    public required int Id { get; set; }
    public required string Conformidad { get; set; }
    public required string Familia { get; set; }
    public required string Reg { get; set; }
    public required string CodigoInsumo { get; set; }
    public required string DescripcionQBD { get; set; }
    public required bool Coa { get; set; }
    public required string Lote { get; set; }
    public required string Um { get; set; }
    public required decimal CantidadRecibida { get; set; }
    public required decimal Potencia { get; set; }
    public DateTimeOffset? FechaFabricacion { get; set; }
    public DateTimeOffset? FechaVencimiento { get; set; }
    public required decimal CantidadPaquetes { get; set; }
    public decimal? Densidad { get; set; }
    public string? DescripcionFactura { get; set; }
    public string? Fabricante { get; set; }
    public string? CondicionAlmacenamiento { get; set; }
}
public class CompraLabDetEmpRes
{
    public required int Id { get; set; }
    public required string Conformidad { get; set; }
    public required string Familia { get; set; }
    public required string Reg { get; set; }
    public required string Codigo { get; set; }
    public required bool Coa { get; set; }
    public required string DescripcionQBD { get; set; }
    public required string Lote { get; set; }
    public required string Um { get; set; }
    public DateTimeOffset? FechaFabricacion { get; set; }
    public DateTimeOffset? FechaVencimiento { get; set; }
    public required decimal CantidadPaquetes { get; set; }
    public required decimal CantidadRecibida { get; set; }
    public string? DescripcionFactura { get; set; }
    public string? Fabricante { get; set; }
    public string? CondicionAlmacenamiento { get; set; }
}
public class EtiquetaCompra
{
    public required string Familia { get; set; }
    public decimal? Tara { get; set; }
}
public class LabListaRes
{
    public int Id { get; set; }
    public required DateTime FechaCotizacion { get; set; }
    public string? Factura { get; set; }
    public string? Guia { get; set; }
    public string? CodFacQbd { get; set; }
    public required string CUO { get; set; }
    public DateTime? FechaFactura { get; set; }
    public required string NombreProveedor { get; set; }
    public required string Familia { get; set; }
    public string? ImgFactura { get; set; }
    public required string EstadoCompra { get; set; }
    public required string Ruc { get; set; }
    public required string NumProvedor { get; set; }
    public required string Usuario { get; set; }
    public DateTime? FechaLab { get; set; }
}
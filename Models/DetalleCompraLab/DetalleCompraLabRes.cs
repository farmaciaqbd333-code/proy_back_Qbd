using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public class ObtenerCompraLabRes
{
    public required string CodigoProveedor { get; set; }
    public List<ObtenerDetalleCompraLabRes>? Detalles { get; set; }
}
public class ObtenerDetalleCompraLabRes
{
    public required int Id { get; set; }
    public required string Reg { get; set; }
    public required string Codigo { get; set; }
    public required string DescripcionQBD { get; set; }
    public required bool Coa { get; set; }
    public required string Lote { get; set; }
    public required string Um { get; set; }
    public required decimal CantidadSolicitada { get; set; }
    public required decimal Potencia { get; set; }
    public required DateTime FechaFabricacion { get; set; }
    public required DateTime FechaVencimiento { get; set; }
    public required string CondicionALmacenamiento { get; set; }
    public required decimal TotalPaquetes { get; set; }
    public required decimal TotalPeso { get; set; }
}
public class ObtenerCompraLab2Res
{
    public required string CodigoProveedor { get; set; }
    public List<ObtenerDetalleCompraLab2Res>? Detalles { get; set; }
}
public class ObtenerDetalleCompraLab2Res
{
    public required string Conformidad { get; set; }
    public required string Reg { get; set; }
    public required string CodigoInsumo { get; set; }
    public required string DescripcionQBD { get; set; }
    public required bool Coa { get; set; }
    public required string Lote { get; set; }
    public required string Um { get; set; }
    public required decimal CantidadSolicitada { get; set; }
    public required decimal Potencia { get; set; }
    public required DateTime FechaFabricacion { get; set; }
    public required DateTime FechaVencimiento { get; set; }
    public required decimal CantidadPaquetes { get; set; }
    public required decimal CantidadRecibida { get; set; }
}

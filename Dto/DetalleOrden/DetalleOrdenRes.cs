using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{

    public class DetalleInsumosRes
    {
        public int Id { get; set; } // ID primario de la fila
        public required int IdInsumo { get; set; }
        public required string Codigo { get; set; }
        public required string DescripcionQBD { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required string UM { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
        public bool Coa { get; set; }
        public string? Lote { get; set; }
        public string? RegistroSanitario { get; set; }
        public bool Conforme { get; set; }
        public string? Familia { get; set; }
        public int? IdFabricante { get; set; }
        public string? NombreFabricante { get; set; }
        public string? CodigoFabricante { get; set; }
    }
    public class DetalleEmpaquesRes
    {
        public int Id { get; set; } // ID primario de la fila
        public required int IdEmpaque { get; set; }
        public required string Codigo { get; set; }
        public required string DescripcionQBD { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
        public string? UM { get; set; }
    }
    public class DetalleProductosRes
    {
        public int Id { get; set; }
        public required int IdProducto { get; set; }
        public required string Codigo { get; set; }
        public required string DescripcionQBD { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
        public string? UM { get; set; }
    }
    public class DetalleEconomatosRes
    {
        public int Id { get; set; } // ID primario de la fila
        public required int IdEconomato { get; set; }
        public required string Codigo { get; set; }
        public required string DescripcionQBD { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
        public string? UM { get; set; }
    }
    public class DetalleComprasOtrosRes
    {
        public int Id { get; set; } // ID primario de la fila
        public int IdFamilia { get; set; }
        public required string Codigo { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
        public string? UM { get; set; }
    }
    public class IdFamiliasRes
    {
        public int IdFamilia { get; set; } // ID primario de la fila
        public int Cantidad { get; set; }
    }
    public class IdFamiliasMaxRes
    {
        public int IdFamilia { get; set; } // ID primario de la fila
        public int Ultimo { get; set; }
    }
}
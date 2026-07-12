using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class DetalleOrdenPatchReq
    {
        public int Id { get; set; } // ID primario de la fila en la DB
        public int? IdInsumo { get; set; } // ID del insumo (opcional si se quiere cambiar)

        public string? DescripcionQbd { get; set; }
        public string? DescripcionFac { get; set; }
        public decimal? Cantidad { get; set; }
        public string? Um { get; set; }
        public DateTime? FechaElaboracion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int ModificadorId { get; set; }
        public bool Coa { get; set; }
        public string? Lote { get; set; }
        public string? RegistroSanitario { get; set; }
        public int? IdFabricante { get; set; }
    }
    public class DetalleOtrosReq
    {
        public int IdFamilia { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }
    public class DetalleInsumosCreateReq
    {
        public int IdInsumo { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
        public int? IdFabricante { get; set; }
    }
    public class DetalleEmpaquesCreateReq
    {
        public int IdEmpaque { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }
    public class DetalleProductosCreateReq
    {
        public int IdProducto { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }
    public class DetalleEconomatosCreateReq
    {
        public int IdEconomato { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }

    public class DetalleInsumosUpdateReq
    {
        public int Id { get; set; }
        public int IdInsumo { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
        public int? IdFamilia { get; set; }
        public int? IdFabricante { get; set; }
    }
    public class DetalleUpdateReq
    {
        public int Id { get; set; }
        public int IdFamilia { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }
    public class DetalleEmpaquesUpdateReq
    {
        public int Id { get; set; }
        public int IdEmpaque { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }
    public class DetalleProductosUpdateReq
    {
        public int Id { get; set; }
        public int IdProducto { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }
    public class DetalleEconomatosUpdateReq
    {
        public int Id { get; set; }
        public int IdEconomato { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
    }


}
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
    public class DetalleOrdenCreateReq
    {
        public int IdInsumo { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
        public required int IdCreador { get; set; }
        public int? IdFamilia { get; set; }
        public int? IdFabricante { get; set; }
    }
    public class DetalleOrdenUpdateReq
    {
        public int Id { get; set; }
        public int IdInsumo { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
        public int? IdFamilia { get; set; }
        public int? IdFabricante { get; set; }
    }
    public class OrdenCompraMesonReq
    {
        public required string Estado { get; set; }
        public int ModificadorId { get; set; }
    }
    public class ConvertirDetalleCompraReq
    {
        public int IdDetalleCompra { get; set; }
        public required string DescripcionFactura { get; set; }
        public required int Cantidad { get; set; }
        public required string Um { get; set; }
        public required DateTime FechaFabricacion { get; set; }
        public required DateTime FechaVencimiento { get; set; }
        public required bool Coa { get; set; }
        public required string Lote { get; set; }
        public required string RegistroSanitario { get; set; }
        public required bool Conformidad { get; set; }
        public int? IdFabricante { get; set; }
    }
    public class DetalleOrdenMesonRes
    {
        public int Id { get; set; }
        public string? Reg { get; set; }
        public required string Codigo { get; set; }
        public required string Descripcion { get; set; }
        public required string DescripcionFactura { get; set; }
        public decimal Cantidad { get; set; }
        public string? Um { get; set; }
        public bool Coa { get; set; }
        public string? Lote { get; set; }
        public string? RegistroSanitario { get; set; }
        public DateTime? FechaFabricacion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public bool? Conformidad { get; set; }
        public int? IdFabricante { get; set; }
        public string? NombreFabricante { get; set; }
        public string? CodigoFabricante { get; set; }
    }
}
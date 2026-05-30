namespace proy_back_Qbd.Models
{
    public class MesonConvertirReq
    {
        public required string SerieComprobante { get; set; }
        public required string NumeroComprobante { get; set; }
        public required string Guia { get; set; }
        public required string CodFacQBD { get; set; }
        public required DateTime FechaFactura { get; set; }
        public required int IdModificador { get; set; }
        public List<MesonDetInsumoConvReq>? DetallesInsumos { get; set; } = [];
        public List<MesonDetProductoConvReq>? DetallesProductos { get; set; } = [];
        public List<MesonDetEconomatoConvReq>? DetallesEconomatos { get; set; } = [];
        public List<MesonDetEmpaqueConvReq>? DetallesEmpaques { get; set; } = [];
        public List<MesonDetOtrosConvReq>? DetallesOtros { get; set; }
    }
    public class MesonDetInsumoConvReq
    {
        public int IdDetalleInsumo { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadRecibida { get; set; }
        public required DateTime FechaFabricacion { get; set; }
        public required DateTime FechaVencimiento { get; set; }
        public required bool Coa { get; set; }
        public required string Lote { get; set; }
        public required string RegistroSanitario { get; set; }
        public required bool Conformidad { get; set; }
        public int? IdFabricante { get; set; }
    }
    public class MesonDetProductoConvReq
    {
        public int IdDetalleProducto { get; set; }
        public required string DescripcionFactura { get; set; }
        public int? IdFabricante { get; set; }
        public required decimal CantidadRecibida { get; set; }
        public required string Lote { get; set; }
        public required string RegistroSanitario { get; set; }
        public required DateTime FechaFabricacion { get; set; }
        public required DateTime FechaVencimiento { get; set; }
        public required bool Conformidad { get; set; }
    }
    public class MesonDetEconomatoConvReq
    {
        public int IdDetalleEconomato { get; set; }
        public required string DescripcionFactura { get; set; }
        public int? IdFabricante { get; set; }
        public required decimal CantidadRecibida { get; set; }
        public required bool Conformidad { get; set; }
    }
    public class MesonDetEmpaqueConvReq
    {
        public int IdDetalleEmpaque { get; set; }
        public required string DescripcionFactura { get; set; }
        public int? IdFabricante { get; set; }
        public required decimal CantidadRecibida { get; set; }
        public required bool Coa { get; set; }
        public required string Lote { get; set; }
        public required DateTime FechaFabricacion { get; set; }
        public required DateTime FechaVencimiento { get; set; }
        public required bool Conformidad { get; set; }
    }
    public class MesonDetOtrosConvReq
    {
        public int IdDetalleOtro { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadRecibida { get; set; }
        public required bool Conformidad { get; set; }
    }
}
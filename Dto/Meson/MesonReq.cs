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
        public required IEnumerable<MesonConvertirDetalleReq> Detalles { get; set; }
    }
    public class MesonConvertirDetalleReq
    {
        public int IdDetalleCompra { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required DateTime FechaFabricacion { get; set; }
        public required DateTime FechaVencimiento { get; set; }
        public required bool Coa { get; set; }
        public required string Lote { get; set; }
        public required string RegistroSanitario { get; set; }
        public required bool Conformidad { get; set; }
        public int? IdFabricante { get; set; }
    }
}
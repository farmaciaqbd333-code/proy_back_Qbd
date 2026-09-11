namespace proy_back_Qbd.Dto.NotaSalida
{
    public class NotaSalidaListaRes
    {
        public int IdNotaSalida { get; set; }
        public required string Codigo { get; set; }
        public required DateTimeOffset FechaCreacion { get; set; }
        public required string Destino { get; set; }
        public required string Responsable { get; set; }
        public string? Observacion { get; set; }
        public string? Estado { get; set; }
        public string? Origen { get; set; }
        public int? IdSedeOrigen { get; set; }
        public int? IdSedeDestino { get; set; }
    }

    public class RegistrosListaRes
    {
        public int IdArticulo { get; set; }
        public required string DescripcionArticulo { get; set; }
        public required string CodigoArticulo { get; set; }
        public string? Lote { get; set; }
        public string? Um { get; set; }
        public decimal? StockDisponible { get; set; }
    }

    public class RegistrosRes
    {
        public int IdRegistro { get; set; }
        public required string CodRegistro { get; set; }
        public string? Descripcion { get; set; }
        public string? CodigoArticulo { get; set; }
        public string? Lote { get; set; }
        public string? Um { get; set; }
        public decimal? StockDisponible { get; set; }
    }
}
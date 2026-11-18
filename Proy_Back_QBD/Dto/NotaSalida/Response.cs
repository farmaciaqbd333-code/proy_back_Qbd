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
        public string? Familia { get; set; }
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

    /// <summary>Línea de PI de una nota de salida sin producto intermedio asignado.</summary>
    public class LineaPiSinAsignarRes
    {
        public int IdNotaSalidaInsumo { get; set; }
        public int IdNotaSalida { get; set; }
        public string? Lote { get; set; }
        public decimal Cantidad { get; set; }
        public string? Um { get; set; }
        public int IdSedeOrigen { get; set; }
        public int? IdSedeDestino { get; set; }
        public DateTime? FechaSalida { get; set; }
        /// <summary>AMBIGUA: el lote coincide con varios PI. SIN_COINCIDENCIA: con ninguno.</summary>
        public string Motivo { get; set; } = "";
        public List<PiCandidatoRes> Candidatos { get; set; } = new();
    }

    public class PiCandidatoRes
    {
        public int IdProductoIntermedio { get; set; }
        public int IdInsumo { get; set; }
        public string Descripcion { get; set; } = "";
        public string Lote { get; set; } = "";
        public int IdSede { get; set; }
        public string Sede { get; set; } = "";
        public DateTime? FechaVencimiento { get; set; }
    }
}
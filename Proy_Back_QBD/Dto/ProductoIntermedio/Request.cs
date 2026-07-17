namespace Proy_back_QBD.Request
{
    public class ActualizarProductoIntermedioReq
    {
        public required string Lote { get; set; }
        public int IdInsumo { get; set; }
        public required string LoteEstandar { get; set; }
        public required string Tipo { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public required string Um { get; set; } = string.Empty;
        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public required int IdElaborado { get; set; }
        public int? IdAutorizado { get; set; }
        public string? Procedimiento { get; set; } = string.Empty;
        public List<int> IdEmpaques { get; set; } = new();
        public string? Aspecto { get; set; } = string.Empty;
        public string? Color { get; set; } = string.Empty;
        public string? Olor { get; set; } = string.Empty;
        public decimal Ph { get; set; }
        public int IdModificador { get; set; }
        public List<InsumoProductoIntermedioReq> Insumos { get; set; } = new();
    }
    public class CrearProductoIntermedioReq
    {
        public required string Lote { get; set; }
        public int IdInsumo { get; set; }
        public required string LoteEstandar { get; set; }
        public required string Tipo { get; set; } = string.Empty;
        public string? TipoUso { get; set; }
        public decimal Cantidad { get; set; }
        public required string Um { get; set; } = string.Empty;
        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public required int IdElaborado { get; set; }
        public int? IdAutorizado { get; set; }
        public string? Procedimiento { get; set; } = string.Empty;
        public List<int> IdEmpaques { get; set; } = new();
        public string? Aspecto { get; set; } = string.Empty;
        public string? Color { get; set; } = string.Empty;
        public string? Olor { get; set; } = string.Empty;
        public decimal Ph { get; set; }
        public int IdCreador { get; set; }
        public List<InsumoProductoIntermedioReq> Insumos { get; set; } = new();
    }
    public class InsumoProductoIntermedioReq
    {
        public int IdInsumo { get; set; }
        public decimal Porcentaje { get; set; }
        public required string Variable { get; set; }
        public decimal CantidadUnidad { get; set; }
        public decimal FactorCorrecion { get; set; }
        public decimal Dilucion { get; set; }
        public required string UnidadMedida { get; set; }
        public decimal CantidadLote { get; set; }
        public decimal? Practica { get; set; }
        public bool Csp { get; set; }
    }
}
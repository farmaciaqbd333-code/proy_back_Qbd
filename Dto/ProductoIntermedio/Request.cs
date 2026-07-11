namespace Proy_back_QBD.Request
{
    public class CrearProductoIntermedioReq
    {
        public required string Registro { get; set; }
        public required string Lote { get; set; }
        public int IdInsumo { get; set; }
        public required string LoteEstandar { get; set; }
        public required string Tipo { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public required string Um { get; set; } = string.Empty;
        public DateTimeOffset? FechaEmision { get; set; }
        public DateTimeOffset? FechaVencimiento { get; set; }
        public required int IdElaborado { get; set; }
        public int? IdAutorizado { get; set; }
        public string? Procedimiento { get; set; } = string.Empty;
        public int? IdEmpaque { get; set; }
        public int? IdEtiqueta { get; set; }
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
        public string? Practica { get; set; }
        public bool Csp { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public int IdCreador { get; set; }
        public int IdProductoIntermedio { get; set; }
    }
}
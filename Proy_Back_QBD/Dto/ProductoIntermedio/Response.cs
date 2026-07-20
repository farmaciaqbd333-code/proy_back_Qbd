namespace Proy_back_QBD.Dto
{
    public class TablaPIRes
    {
        public int Id { get; set; }

        public string? Registro { get; set; }

        public string? Lote { get; set; }

        public string? Codigo { get; set; }

        public string? Descripcion { get; set; }

        public string? LoteEstandar { get; set; }

        public decimal? PesoUnidad { get; set; }

        public decimal? LoteEstTotal { get; set; }

        public string? Tipo { get; set; }

        public decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public string? TipoUso { get; set; }
        public DateTime FechaEmision { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        public string? Elaborado { get; set; }
    }
    public class ConsumoPIRes
    {
        public string? Codigo { get; set; }
        public decimal? Porcentaje { get; set; }
        public string? Descripcion { get; set; }
        public string? V { get; set; } // Variable
        public string? Lote { get; set; }
        public string? Registro { get; set; }
        public decimal CantidadUnidad { get; set; }
        public decimal? FactorCorreccion { get; set; }
        public decimal? Dilucion { get; set; }
        public string? Um { get; set; } // Unidad de medida
        public decimal CantidadLote { get; set; }
        public decimal? Practica { get; set; } 
        public bool? CSP { get; set; }
    }

    public class MasterPIRes
    {
        public int IdInsumo { get; set; }
        public string? Codigo { get; set; }
        public string? Descripcion { get; set; }
        public string? TipoUso { get; set; }
        public string? Um { get; set; }
        public int? UltimoProductoIntermedioId { get; set; }
    }
}
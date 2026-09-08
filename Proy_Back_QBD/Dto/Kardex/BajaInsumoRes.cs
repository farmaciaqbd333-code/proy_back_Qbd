using System;

namespace proy_back_Qbd.Models.Kardex
{
    public class BajaInsumoRes
    {
        public int IdCompraInsumo { get; set; }
        public string Registro { get; set; } = "";
        public int IdInsumo { get; set; }
        public string Codigo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Lote { get; set; } = "";
        public decimal CantidadBaja { get; set; }
        public string Um { get; set; } = "";
        public DateTime? FechaFabricacion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int DiasVencido { get; set; }
        public string Estado { get; set; } = "VENCIDO";
        public string? Proveedor { get; set; }
        public string? SedeOrigen { get; set; }
        public string? DocumentoOrigen { get; set; }
        public string? Observacion { get; set; }
    }
}

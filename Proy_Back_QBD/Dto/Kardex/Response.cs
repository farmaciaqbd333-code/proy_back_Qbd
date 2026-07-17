using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models.Kardex
{

    public class StockRes
    {
        public required string Codigo { get; set; }
        public required string Descripcion { get; set; }
        public required string Um { get; set; }
        public decimal? Entradas { get; set; }
        public decimal? Salidas { get; set; }
        public decimal? Ajustes { get; set; }
        public decimal? Baja { get; set; }
        public string? Tipo { get; set; }
    }
        public class DetalleInsumoRes
    {
        public string Registro { get; set; } = "";
        public required string Lote { get; set; }
        public decimal? Saldo { get; set; }
        public DateTime? FechaCompra { get; set; }
        public DateTimeOffset? FechaFabricacion { get; set; }
        public DateTimeOffset? FechaVencimiento { get; set; }
        public string? Observacion { get; set; }
    }
    public class DetalleEmpaqueRes
    {
        public string Registro { get; set; } = "";
        public required string Lote { get; set; }
        public decimal? Saldo { get; set; }
        public DateTime? FechaCompra { get; set; }
        public DateTimeOffset? FechaFabricacion { get; set; }
        public DateTimeOffset? FechaVencimiento { get; set; }
        public string? Observacion { get; set; }
    }

    public class ComprasVencidasRes
    {
        public required string Registro { get; set; }
        public required string Codigo { get; set; }
        public required string Descripcion { get; set; }
        public required string Lote { get; set; }
        public required decimal Cantidad { get; set; }
        public DateTimeOffset? FechaFabricacion { get; set; }
        public DateTimeOffset? FechaVencimiento { get; set; }
        public decimal? Saldo { get; set; }
        public required string Estado { get; set; }
    }
}
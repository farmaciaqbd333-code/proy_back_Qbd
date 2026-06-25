using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models.Stock
{
    public class StockGetRes
    {
        public List<StockMPRes> MateriaPrimas { get; set; } = [];
        public List<StockMERes> Empaques { get; set; } = [];
    }
    public class StockMPRes
    {
        public required string Codigo { get; set; }
        public required string Descripcion { get; set; }
        public required string Um { get; set; }
        public decimal? Entradas { get; set; }
        public decimal? Salidas { get; set; }
        public decimal? Ajustes { get; set; }
        public decimal? Baja { get; set; }
    }
    public class DetalleInsumoRes
    {
        public required string Familia { get; set; }
        public required string Registro { get; set; }
        public required string Lote { get; set; }
        public required string Saldo { get; set; }
        public required decimal FechaFabricacion { get; set; }
        public required decimal FechaVencimiento { get; set; }
    }
    public class StockMERes
    {
        public required string Codigo { get; set; }
        public required string Descripcion { get; set; }
        public required string Um { get; set; }
        public required decimal Entradas { get; set; }
        public required decimal Salidas { get; set; }
        public required decimal Ajustes { get; set; }
        public required decimal Baja { get; set; }
    }
}
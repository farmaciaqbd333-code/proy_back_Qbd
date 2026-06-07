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
        public required decimal Entradas { get; set; }
        public required decimal Salidas { get; set; }
        public required decimal Ajustes { get; set; }
        public required decimal Baja { get; set; }
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
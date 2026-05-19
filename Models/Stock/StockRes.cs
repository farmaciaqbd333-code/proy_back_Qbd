using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models.Stock
{
    public class StockRes
    {
        public required string Codigo { get; set; }
        public required string Descripcion { get; set; }
        public required string Um { get; set; }
        public required decimal Entradas { get; set; }
        public required decimal Salidas { get; set; }
        public required decimal Ajustes { get; set; }
    }
}
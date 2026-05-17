using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    
    public class ObtenerDetalleOrdenOCompraRes
    {
        public int Id { get; set; } // ID primario de la fila
        public required int IdInsumo { get; set; }
        public required string Codigo { get; set; }
        public required string DescripcionQBD { get; set; }
        public required string DescripcionFactura { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required string UM { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
        public bool Coa { get; set; }
        public string? Lote { get; set; }
        public string? RegistroSanitario { get; set; }
        public bool Conforme { get; set; }
        public string? Familia { get; set; }
    }
    public class IdFamiliasRes
    {
        public int IdFamilia { get; set; } // ID primario de la fila
        public int Cantidad { get; set; }
    }
    public class IdFamiliasMaxRes
    {
        public int IdFamilia { get; set; } // ID primario de la fila
        public int Ultimo { get; set; }
    }
}
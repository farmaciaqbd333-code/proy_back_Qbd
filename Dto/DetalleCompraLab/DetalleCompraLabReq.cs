using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class ActualizarDetCompraLabReq
    {
        public int IdDetalle { get; set; }
        public bool Coa { get; set; }
        public required string  Lote { get; set; }
        public required decimal CantidadSolicitada { get; set; }
        public required decimal Potencia { get; set; }
        public DateTime FechaFabricacion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public required string CondicionAlmacenamiento { get; set; }
        public string? JustificacionDiferencia { get; set; }
    }
}
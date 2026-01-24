using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Empaque
{
    public class EmpaqueCreateReq
    {
        public string? Descripcion { get; set; }
        public int? FundaId { get; set; }
        public int? CajaId { get; set; }
        public int? EtiquetaId1 { get; set; }
        public int? EtiquetaId2 { get; set; }
        public string? Tara { get; set; }
        public int CreadorId { get; set; }
    }
    public class EmpaqueUpdateReq
    {
        public string? Descripcion { get; set; }
        public int? FundaId { get; set; }
        public int? CajaId { get; set; }
        public int? EtiquetaId1 { get; set; }
        public int? EtiquetaId2 { get; set; }
        public string? Tara { get; set; }
        public int ModificadorId { get; set; }
    }
}
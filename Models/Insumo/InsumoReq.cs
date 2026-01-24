using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Insumo
{
    public class InsumoCreateReq
    {
        public required string Descripcion { get; set; }
        public required string UnidadMedida { get; set; }
        public required string FactorCorreccion { get; set; }
        public required string Dilucion { get; set; }
        public int CreadorId { get; set; }
    }
    public class InsumoUpdateReq
    {
        public required string Descripcion { get; set; }
        public required string UnidadMedida { get; set; }
        public required string FactorCorreccion { get; set; }
        public required string Dilucion { get; set; }
        public int ModificadorId { get; set; }
    }
}
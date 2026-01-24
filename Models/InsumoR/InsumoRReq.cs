using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Dto.Auxiliares
{
    public class InsumoRCreateReq
    {
        public int InsumoId { get; set; }  // Puede ser nulo  
        public decimal? Porcentaje { get; set; }  // Puede ser nulo    
        public int? CreadorId { get; set; }  // Puede ser nulo
    }
    public class InsumoRUpdateReq
    {
        public int InsumoId { get; set; }  // Puede ser nulo  
        public decimal? Porcentaje { get; set; }  // Puede ser nulo    
        public int? ModificadorId { get; set; }  // Puede ser nulo
    }
}
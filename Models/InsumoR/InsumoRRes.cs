using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Dto.Auxiliares
{
    public class InsumoRFindAllRes
    {
        public int InsumoId { get; set; }  // Puede ser nulo  
        public string? Descripcion { get; set; }  // Puede ser nulo    
        public decimal? Porcentaje { get; set; }  // Puede ser nulo    
    }
    public class InsumoRFindIdRes
    {
        public int InsumoId { get; set; }  // Puede ser nulo  
        public decimal? Porcentaje { get; set; }  // Puede ser nulo    
    }
    
}
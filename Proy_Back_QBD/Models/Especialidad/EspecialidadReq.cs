using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Dto.Auxiliares
{
    public class EspecialidadCreateReq
    {
        public string? Nombre { get; set; }  // Puede ser nulo        
        public int CreadorId { get; set; }
    }
    public class EspecialidadUpdateReq
    {
        public string? Nombre { get; set; }  // Puede ser nulo        
        public int ModificadorId { get; set; }
    }
}
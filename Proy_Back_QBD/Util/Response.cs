using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Util
{
    public class Response
    {
        public bool Success { get; set; }
        public required string Mensaje { get; set; }
    }
}
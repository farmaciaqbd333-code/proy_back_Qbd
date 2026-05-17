using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class CrearPaqueteReq
    {
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
        public int IdCreador { get; set; }
        public int IdDetalleCompra { get; set; }
    }
    public class ModificarPaqueteReq
    {
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
        public int IdModificador { get; set; }
    }
}
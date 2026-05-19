using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class DetallePaqueteRes
    {
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
        public List<ListaDetallePaqueteRes>? Lista { get; set; }
    }
    public class ListaDetallePaqueteRes
    {
        public required string IdInsumo { get; set; }
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
    }
}
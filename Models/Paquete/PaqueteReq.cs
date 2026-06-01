using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class PaqueteInsumoCrearReq
    {
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
        public int IdCreador { get; set; }
        public int IdCompraInsumo { get; set; }
    }
    public class PaqueteEmpaqueCrearReq
    {
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
        public int IdCreador { get; set; }
        public int IdCompraEmpaque { get; set; }
    }
    public class PaqueteInsumoModificarReq
    {
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
        public int IdModificador { get; set; }
    }
    public class PaqueteEmpaqueModificarReq
    {
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
        public int IdModificador { get; set; }
    }
}
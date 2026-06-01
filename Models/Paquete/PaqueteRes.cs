using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class PaqueteInsumoDetalleRes
    {
        public List<PaqueteInsumoListRes>? ListaInsumos { get; set; } = [];
        public List<PaqueteEmpaqueListRes>? ListaEmpaques { get; set; } = [];
    }
    public class PaqueteInsumoListRes
    {
        public int IdPaquete { get; set; }
        public string? CodigoCompraInsumo { get; set; }
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
    }
    public class PaqueteEmpaqueListRes
    {
        public int IdPaquete { get; set; }
        public string? CodigoCompraEmpaque { get; set; }
        public decimal CantidadPaquete { get; set; }
        public decimal PesoUnitario { get; set; }
        public decimal Tara { get; set; }
    }
}
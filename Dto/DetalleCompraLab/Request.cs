using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models.DetalleCompraLab
{
    public class ActualizarDetCompraLabReq
    {
        public List<ActualizarInsumoReq> Insumos { get; set; } = [];
        public List<ActualizarEmpaqueReq> Empaques { get; set; } = [];
    }
    public class ActualizarInsumoReq
    {
        public int IdCompraInsumo { get; set; }
        public required decimal Potencia { get; set; }
        public DateTime FechaFabricacion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public required string CondicionAlmacenamiento { get; set; }
        public string? Observacion { get; set; }
    }
    public class ActualizarEmpaqueReq
    {
        public int IdCompraEmpaque { get; set; }
        public DateTime FechaFabricacion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public required string CondicionAlmacenamiento { get; set; }
        public string? Observacion { get; set; }
    }
}
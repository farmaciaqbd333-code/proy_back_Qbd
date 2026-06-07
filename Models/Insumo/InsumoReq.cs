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
        public decimal Costo { get; set; }
        public int FamiliaId { get; set; }
        public string FormaFarmaceutica { get; set; } = "";
        public string? NumeroCas { get; set; }
        public decimal? Base { get; set; }
        public decimal? UsoMin { get; set; }
        public decimal? Sal { get; set; }
        public decimal? UsoMax { get; set; }
        public decimal? FactorE { get; set; }
        public decimal? PrecioCosto { get; set; }
        public decimal? PrecioVenta { get; set; }
        public bool? Higroscopico { get; set; }
        public bool? Fotosensible { get; set; }
        public bool? Refrigerado { get; set; }
        public int CreadorId { get; set; }
    }
    public class InsumoUpdateReq
    {
        public required string Descripcion { get; set; }
        public required string UnidadMedida { get; set; }
        public required string FactorCorreccion { get; set; }
        public required string Dilucion { get; set; }
        public decimal Costo { get; set; }
        public int FamiliaId { get; set; }
        public string FormaFarmaceutica { get; set; } = "";
        public string? NumeroCas { get; set; }
        public decimal? Base { get; set; }
        public decimal? UsoMin { get; set; }
        public decimal? Sal { get; set; }
        public decimal? UsoMax { get; set; }
        public decimal? FactorE { get; set; }
        public decimal? PrecioCosto { get; set; }
        public decimal? PrecioVenta { get; set; }
        public bool? Higroscopico { get; set; }
        public bool? Fotosensible { get; set; }
        public bool? Refrigerado { get; set; }
        public int ModificadorId { get; set; }
    }
}
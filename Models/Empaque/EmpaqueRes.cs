using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Dto.Empaque
{

    public class EmpaqueFindAllRes
    {
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public int? FundaId { get; set; }
        public string? Funda { get; set; }
        public int? CajaId { get; set; }
        public string? Caja { get; set; }
        public int? EtiquetaId1 { get; set; }
        public string? Etiqueta1 { get; set; }
        public int? EtiquetaId2 { get; set; }
        public string? Etiqueta2 { get; set; }
        public string? Codigo { get; set; }
        public decimal? Costo { get; set; }
        public string? Tara { get; set; }
        public string? ImagenUrl { get; set; }
        public int FamiliaId { get; set; }
        public string? CodigoUbicacion { get; set; }
    }
    public class EmpaqueFindIdRes
    {
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public int? FundaId { get; set; }
        public string? Funda { get; set; }
        public int? CajaId { get; set; }
        public string? Caja { get; set; }
        public int? EtiquetaId1 { get; set; }
        public string? Etiqueta1 { get; set; }
        public int? EtiquetaId2 { get; set; }
        public string? Etiqueta2 { get; set; }
        public string? Codigo { get; set; }
        public decimal? Costo { get; set; }
        public string? Tara { get; set; }
        public string? ImagenUrl { get; set; }
        public int FamiliaId { get; set; }
    }
}
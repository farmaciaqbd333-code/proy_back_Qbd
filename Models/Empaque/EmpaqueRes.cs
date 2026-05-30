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
        public string? Funda { get; set; }
        public string? Caja { get; set; }
        public string? Etiqueta1 { get; set; }
        public string? Etiqueta2 { get; set; }
        public string? Tara { get; set; }
    }
    public class EmpaqueFindIdRes
    {
        public int Id { get; set; }
        public string? Descripcion { get; set; }
        public int? IdFunda { get; set; }
        public string? Funda { get; set; }
        public int? IdCaja { get; set; }
        public string? Caja { get; set; }
        public int? IdEtiqueta1 { get; set; }
        public string? Etiqueta1 { get; set; }
        public int? IdEtiqueta2 { get; set; }
        public string? Etiqueta2 { get; set; }
        public string? Tara { get; set; }
    }
}
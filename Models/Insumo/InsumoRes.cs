using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proy_back_QBD.Dto.Insumo
{
    public class InsumoLabRes
    {
        public int? Id { get; set; }
        public required string Descripcion { get; set; }
        public required string FactorCorreccion { get; set; }
        public required string Dilucion { get; set; }
        public required string UnidadMedida { get; set; }
    }
    public class InsumoFindAllRes
    {
        public int Id { get; set; }
        public required string Descripcion { get; set; }
        public required string FactorCorreccion { get; set; }
        public required string Dilucion { get; set; }
        public required string UnidadMedida { get; set; }
    }
    public class InsumoFindIdRes
    {
        public int? Id { get; set; }
        public required string Descripcion { get; set; }
        public required string UnidadMedida { get; set; }
        public required string FactorCorreccion { get; set; }
        public required string Dilucion { get; set; }
    }
    public class InsumoFormR
    {
        public int? Id { get; set; }
        public string? Codigo { get; set; }
        public decimal Porcentaje { get; set; }
        public required string Descripcion { get; set; }
        public required string UnidadMedida { get; set; }
        public required string FactorCorreccion { get; set; }
        public required string Dilucion { get; set; }
    }
}
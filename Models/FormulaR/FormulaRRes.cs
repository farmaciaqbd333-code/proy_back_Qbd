using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Dto.Insumo;

namespace Proy_back_QBD.Dto.Auxiliares
{
    public class FormulaRRes
    {
        public int Id { get; set; }
        public required string Descripcion { get; set; }
        public int? EmpaqueId { get; set; }  // Puede ser nulo
        public string? Procedimiento { get; set; }  // Puede ser nulo
        public string? Aspecto { get; set; }  // Puede ser nulo
        public string? Color { get; set; }  // Puede ser nulo
        public string? Olor { get; set; }  // Puede ser nulo
        public string? Ph { get; set; }  // Puede ser nulo
        public List<InsumoFormR>? Insumos { get; set; }  // Puede ser nulo
    }
}
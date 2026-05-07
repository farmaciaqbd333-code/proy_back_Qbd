using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class DetalleOrdenCompraPatchReq
    {
        public int Id { get; set; } // ID primario de la fila en la DB
        public int? IdInsumo { get; set; } // ID del insumo (opcional si se quiere cambiar)

        public string? DescripcionQbd { get; set; }
        public string? DescripcionFac { get; set; }
        public decimal? Cantidad { get; set; }
        public string? Um { get; set; }
        public DateTime? FechaElaboracion { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int ModificadorId { get; set; }
        public bool Coa { get; set; }
        public string? Lote { get; set; }
        public string? RegistroSanitario { get; set; }
    }
    public class DetalleOrdenCompraCreateReq
    {
        public int IdInsumo { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
        public required int IdCreador { get; set; }
        public required int IdFamilia { get; set; }
    }
    public class DetalleOrdenCompraUpdateReq
    {
        public int Id { get; set; }
        public int IdInsumo { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string Um { get; set; }
        public required decimal CostoUnitario { get; set; }
        public required decimal CostoTotal { get; set; }
        public required int IdFamilia { get; set; }
    }
    public class OrdenCompraMesonReq
    {
        public required string Estado { get; set; }
        public int ModificadorId { get; set; }
    }
}
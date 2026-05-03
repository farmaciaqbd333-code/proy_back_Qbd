using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    
    public class OrdenCompraCreateReq
    {
        public required int IdProveedor { get; set; }
        public required string Modalidad { get; set; }
        public required string Moneda { get; set; }
        public required decimal TipoCambio { get; set; }
        public required decimal Impuesto { get; set; }
        public required DateTime FechaEmision { get; set; }
        public required string Observaciones { get; set; }
        public int IdFamilia { get; set; }
        public required int IdSede { get; set; }
        public required int CreadorId { get; set; }
        public required string TipoTributario { get; set; }
        public string? TipoOperacion { get; set; }
        public bool IncluyeImpuesto { get; set; }
        public required int ModificadorId { get; set; }
        public required List<OrdenCompraCreateReq2> Detalle { get; set; }
    }
    public class OrdenCompraCreateReq2
    {
        public required int IdInsumo { get; set; }
        public required string DescripcionFac { get; set; }
        public required decimal Cantidad { get; set; }
        public required string UM { get; set; }
        public required decimal CUnitario { get; set; }
        public required decimal CTotal { get; set; }
    }
    public class OrdenCompraUpdateReq
    {
        public required int IdProveedor { get; set; }
        public required string Modalidad { get; set; }
        public required string Moneda { get; set; }
        public required decimal TipoCambio { get; set; }
        public required decimal Impuesto { get; set; }
        public required DateTime FechaEmision { get; set; }
        public required string Observaciones { get; set; }
        public int IdFamilia { get; set; }
        public required int IdSede { get; set; }
        public string? TipoOperacion { get; set; }
        public bool IncluyeImpuesto { get; set; }
        public required string TipoTributario { get; set; }
        public int ModificadorId { get; set; }
    }
    public class PatchMesonDto
    {
        public string EstadoMeson { get; set; }
    }
    public class PatchPagoDto
    {
        public string EstadoPago { get; set; }
    }

}
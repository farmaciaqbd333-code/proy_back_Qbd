using System;
using System.Collections.Generic;

namespace Proy_back_QBD.Models
{
    public class CotizacionCreateReq
    {
        public string? Numero { get; set; }
        public DateOnly? Fecha { get; set; }
        public int? SedeId { get; set; }
        public int? PacienteId { get; set; }
        public string RucDni { get; set; } = string.Empty;
        public string Denominacion { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Moneda { get; set; } = "S/";
        public decimal DescuentoGlobalPorc { get; set; } = 0;
        public decimal DescuentoGlobal { get; set; } = 0;
        public decimal DescuentoItem { get; set; } = 0;
        public decimal DescuentoTotal { get; set; } = 0;
        public decimal OpExonerada { get; set; } = 0;
        public decimal OpInafecta { get; set; } = 0;
        public decimal OpGravada { get; set; } = 0;
        public decimal Igv { get; set; } = 0;
        public decimal OpGratuita { get; set; } = 0;
        public decimal OtrosCargos { get; set; } = 0;
        public decimal Total { get; set; } = 0;
        public string? Estado { get; set; } = "Pendiente";
        public string? CpeRelacionado { get; set; }
        public int ValidezDias { get; set; } = 15;
        public string? FormaPago { get; set; } = "Al Contado";
        public string? Observaciones { get; set; }
        public List<CotizacionDetalleReq> Detalles { get; set; } = new();
    }

    public class CotizacionUpdateReq : CotizacionCreateReq
    {
        public int Id { get; set; }
    }

    public class CotizacionDetalleReq
    {
        public int? ProductoId { get; set; }
        public int? FormulaId { get; set; }
        public string? Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Unidad { get; set; } = "NIU";
        public decimal Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; } = 0;
        public decimal Descuento { get; set; } = 0;
        public decimal Subtotal { get; set; } = 0;
    }

    public class CotizacionEstadoReq
    {
        public string Estado { get; set; } = string.Empty;
    }

    public class CotizacionCpeReq
    {
        public string CpeRelacionado { get; set; } = string.Empty;
    }
}

using System;
using System.Collections.Generic;

namespace Proy_back_QBD.Models
{
    public class CotizacionRes
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public int? SedeId { get; set; }
        public string? SedeNombre { get; set; }
        public int? PacienteId { get; set; }
        public string RucDni { get; set; } = string.Empty;
        public string Denominacion { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string Moneda { get; set; } = "S/";
        public decimal TotalOnerosa { get; set; }
        public decimal TotalGratuita { get; set; }
        public bool EnviadoCliente { get; set; }
        public string? CpeRelacionado { get; set; }
        public string Estado { get; set; } = "Pendiente";
        public decimal DescuentoGlobalPorc { get; set; }
        public decimal DescuentoGlobal { get; set; }
        public decimal DescuentoItem { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal OpExonerada { get; set; }
        public decimal OpInafecta { get; set; }
        public decimal OpGravada { get; set; }
        public decimal Igv { get; set; }
        public decimal OpGratuita { get; set; }
        public decimal OtrosCargos { get; set; }
        public decimal Total { get; set; }
        public int ValidezDias { get; set; }
        public string? FormaPago { get; set; }
        public string? Observaciones { get; set; }
        public int CreadorId { get; set; }
        public string? UsuarioCreador { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<CotizacionDetalleRes> Detalles { get; set; } = new();
    }

    public class CotizacionDetalleRes
    {
        public int Id { get; set; }
        public int CotizacionId { get; set; }
        public int? ProductoId { get; set; }
        public int? FormulaId { get; set; }
        public string? Codigo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Unidad { get; set; } = "NIU";
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
    }
}

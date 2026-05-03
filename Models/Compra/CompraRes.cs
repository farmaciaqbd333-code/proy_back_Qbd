using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{
    public class ListaOrdenesYComprasRes
    {
        public int Id { get; set; }
        public required string CUO { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public string? SerieComprobante { get; set; }
        public string? NumeroComprobante { get; set; }
        public required string NumProvedor { get; set; }
        public required string NombreProveedor { get; set; }
        public required decimal Valor { get; set; }
        public required decimal Total { get; set; }
        public required string Moneda { get; set; }
        public required string EstadoCompra { get; set; }
        public string? CodFacQbd { get; set; }
        public required string Familia { get; set; }
        public string? Factura { get; set; }
        public string? Modalidad { get; set; }
        public required string EstadoPago { get; set; }
        public required string Usuario { get; set; }
    }
}
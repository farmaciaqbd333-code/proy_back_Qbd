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
        public required bool Igv { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public required string Observaciones { get; set; }
        public required string Familia { get; set; }
        public required int IdSede { get; set; }
        public required int IdCreador { get; set; }
        public required IEnumerable<DetalleOrdenCompraCreateReq> Detalle { get; set; }
    }

    public class OrdenCompraUpdateReq
    {
        public required int IdProveedor { get; set; }
        public required string Modalidad { get; set; }
        public required string Moneda { get; set; }
        public required decimal TipoCambio { get; set; }
        public required bool Igv { get; set; }
        public required decimal Isc { get; set; }
        public required decimal Icbp { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public required string Observaciones { get; set; }
        public required string Familia { get; set; }
        public required int IdSede { get; set; }
        public required int IdModificadorCreador { get; set; }
        public IEnumerable<int>? DetallesEliminados { get; set; }
        public IEnumerable<DetalleOrdenCompraCreateReq>? DetallesNuevos { get; set; }
        public IEnumerable<DetalleOrdenCompraUpdateReq>? Detalles { get; set; }
    }
    public class ConvertirCompraReq
    {
        public required string SerieComprobante { get; set; }
        public required string NumeroComprobante { get; set; }
        public required string Guia { get; set; }
        public required string CodFacQBD { get; set; }
        public required string RutaFactura { get; set; }
        public required DateTime FechaFactura { get; set; }
        public required int IdModificador { get; set; }
        public required IEnumerable<ConvertirDetalleCompraReq> Detalles { get; set; }
    }



    public class CambiarEstadoReq
    {
        public required string Estado { get; set; }
        public int IdModificador { get; set; }
    }

}
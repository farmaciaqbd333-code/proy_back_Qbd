using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proy_back_Qbd.Models
{

    public class OrdenCreateReq
    {
        public required int IdProveedor { get; set; }
        public required string Modalidad { get; set; }
        public required string Moneda { get; set; }
        public required decimal TipoCambio { get; set; }
        public required bool Igv { get; set; }
        public required DateTime FechaCotizacion { get; set; }
        public required string Observaciones { get; set; }
        public required int IdSede { get; set; }
        public int IdCreador { get; set; }
        public required decimal Isc { get; set; }
        public required decimal Icbp { get; set; }
        public IEnumerable<DetalleCreateReq>? DetalleCompras { get; set; }
        public IEnumerable<DetalleInsumosCreateReq>? DetalleCompraInsumos { get; set; }
        public IEnumerable<DetalleEmpaquesCreateReq>? DetalleCompraEmpaques { get; set; }
        public IEnumerable<DetalleProductosCreateReq>? DetalleCompraProductos { get; set; }
        public IEnumerable<DetalleEconomatosCreateReq>? DetalleCompraEconomatos { get; set; }
    }

    public class OrdenUpdateReq
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

        // Nuevos arrays de creación durante actualización
        public IEnumerable<DetalleCreateReq>? DetalleComprasNuevos { get; set; }
        public IEnumerable<DetalleInsumosCreateReq>? DetalleCompraInsumosNuevos { get; set; }
        public IEnumerable<DetalleEmpaquesCreateReq>? DetalleCompraEmpaquesNuevos { get; set; }
        public IEnumerable<DetalleProductosCreateReq>? DetalleCompraProductosNuevos { get; set; }
        public IEnumerable<DetalleEconomatosCreateReq>? DetalleCompraEconomatosNuevos { get; set; }

        // Nuevos arrays de edición durante actualización (con IDs)
        public IEnumerable<DetalleUpdateReq>? DetalleCompras { get; set; }
        public IEnumerable<DetalleInsumosUpdateReq>? DetalleCompraInsumos { get; set; }
        public IEnumerable<DetalleEmpaquesUpdateReq>? DetalleCompraEmpaques { get; set; }
        public IEnumerable<DetalleProductosUpdateReq>? DetalleCompraProductos { get; set; }
        public IEnumerable<DetalleEconomatosUpdateReq>? DetalleCompraEconomatos { get; set; }
    }
    public class ConvertirACompraReq
    {
        public required string SerieComprobante { get; set; }
        public required string NumeroComprobante { get; set; }
        public required string Guia { get; set; }
        public required string CodFacQBD { get; set; }
        public required DateTime FechaFactura { get; set; }
        public required int IdModificador { get; set; }
        public required IEnumerable<ConvertirDetalleCompraReq> Detalles { get; set; }
    }

    public class CambiarEstadoReq
    {
        public required string Estado { get; set; }
        public int IdModificador { get; set; }
    }

    public class UpdateRutaFacturaReq
    {
        public required string RutaFactura { get; set; }
    }
}
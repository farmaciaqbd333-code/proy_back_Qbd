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
        public required IEnumerable<DetalleCreateReq> DetalleCompras { get; set; }
        public required IEnumerable<DetalleInsumosCreateReq> DetalleCompraInsumos { get; set; }
        public required IEnumerable<DetalleEmpaquesCreateReq> DetalleCompraEmpaques { get; set; }
        public required IEnumerable<DetalleProductosCreateReq> DetalleCompraProductos { get; set; }
        public required IEnumerable<DetalleEconomatosCreateReq> DetalleCompraEconomatos { get; set; }
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
        public required IEnumerable<int> DetallesEliminados { get; set; }
        public required IEnumerable<int> DetallesCompraInsumosEliminados { get; set; }
        public required IEnumerable<int> DetalleCompraEmpaquesEliminados { get; set; }
        public required IEnumerable<int> DetalleCompraProductosEliminados { get; set; }
        public required IEnumerable<int> DetalleCompraEconomatosEliminados { get; set; }

        // Nuevos arrays de creación durante actualización
        public required IEnumerable<DetalleCreateReq> DetalleComprasNuevos { get; set; }
        public required IEnumerable<DetalleInsumosCreateReq> DetalleCompraInsumosNuevos { get; set; }
        public required IEnumerable<DetalleEmpaquesCreateReq> DetalleCompraEmpaquesNuevos { get; set; }
        public required IEnumerable<DetalleProductosCreateReq> DetalleCompraProductosNuevos { get; set; }
        public required IEnumerable<DetalleEconomatosCreateReq> DetalleCompraEconomatosNuevos { get; set; }

        // Nuevos arrays de edición durante actualización (con IDs)
        public required IEnumerable<DetalleUpdateReq> DetalleComprasUpd { get; set; }
        public required IEnumerable<DetalleInsumosUpdateReq> DetalleCompraInsumosUpd { get; set; }
        public required IEnumerable<DetalleEmpaquesUpdateReq> DetalleCompraEmpaquesUpd { get; set; }
        public required IEnumerable<DetalleProductosUpdateReq> DetalleCompraProductosUpd { get; set; }
        public required IEnumerable<DetalleEconomatosUpdateReq> DetalleCompraEconomatosUpd { get; set; }
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
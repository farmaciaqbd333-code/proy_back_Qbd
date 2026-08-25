using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface IOrdenCompraService
    {
        Task<List<OrdenesYComprasRes>> ListaOrdenesDeCompras(int idSede);
        Task<OrdenesYComprasRes?> ObtenerOrdenOCompra(int id);
        Task<OrdenCompraGetRes?> ObtenerOrdenCompra(int id);
        Task<int> CrearOrdenDeCompra(OrdenCreateReq request);
        Task<string?> EliminarOrdenOCompraOCompra(int id);
        Task<OrdenesYComprasRes?> ActualizarOrdenDeCompra(int id, OrdenUpdateReq request);
        Task<DescripcionFacturaRes> DescripcionFactura(int proveedorI);
        Task<bool> ActualizarEstadoCompra(int IdOrdenCompra, CambiarEstadoReq estado);
        Task<bool> ActualizarEstadoPago(int IdOrdenCompra, CambiarEstadoReq request);
        Task<bool> ActualizarRutaFactura(int id, UpdateRutaFacturaReq request);
        Task<List<OrdenesYComprasRes>> ListaFacturasPorFamilia(string familia, int idSede);
        Task<bool> ActualizarDetallePdf(string familia, int id, string? pdf);
    }
}
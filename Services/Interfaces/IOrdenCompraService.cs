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
        Task<List<OrdenesYComprasRes>> ListaOrdenesYCompras();
        Task<OrdenesYComprasRes?> ObtenerOrdenOCompra(int id);
        Task<ObtenerOrdenOCompraRes?> ObtenerDetalleOrdenOCompra(int id);
        Task<int?> CrearOrdenDeCompra(OrdenCompraCreateReq request);
        Task<string?> EliminarOrdenOCompraOCompra(int id);
        Task<OrdenesYComprasRes?> ActualizarOrdenDeCompra(int id, OrdenCompraUpdateReq request);
        Task<OrdenesYComprasRes?> ConvertirCompra(int ordenCompraId, ConvertirCompraReq request);
        Task<DescripcionFacturaRes> DescripcionFactura(int proveedorI);
        Task<bool> ActualizarEstadoCompra(int OrdenCompraId, CambiarEstadoReq estado);
        Task<OrdenMesonRes?> ObtenerCompraMeson(int ordenCompraId);
    }
}
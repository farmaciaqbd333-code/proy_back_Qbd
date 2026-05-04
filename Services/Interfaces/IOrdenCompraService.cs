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
        Task<string?> EliminarOrdenOCompraOCompra(int id);
        Task<OrdenesYComprasRes?> CrearOrdenDeCompra(OrdenCompraCreateReq request);
    }
}
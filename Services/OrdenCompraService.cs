using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public OrdenCompraService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<List<ListaOrdenesYComprasRes>> ListarOrdenesYCompras()
        {
            List<ListaOrdenesYComprasRes> response = await _context.Compras
                            .Select(s => new ListaOrdenesYComprasRes
                            {
                                Id = s.Id,
                                CUO = "OC-" + s.Id,
                                FechaCotizacion = s.FechaCotizacion,
                                SerieComprobante = s.SerieComprobante,
                                NumeroComprobante = s.NumeroComprobante,
                                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                                Valor = s.DetalleCompras != null ? s.Valor  : 0,
                                Total = s.DetalleCompras != null ? s.Total : 0,
                                Moneda = s.Moneda,
                                CodFacQbd = s.CodFacQBD,
                                Familia = s.Familia,
                                Factura = s.Factura,
                                EstadoPago = s.Modalidad,
                                Usuario = s.Modificador != null ? (s.Modificador.Codigo ?? s.Modificador.Id.ToString()) : "N/A",
                                EstadoCompra = s.EstadoCompra
                            })
                            .OrderByDescending(o => o.Id)
                            .ToListAsync();

            return response;
        }
    }
}
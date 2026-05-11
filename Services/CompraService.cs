using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class CompraService : ICompraService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public CompraService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<OrdenesEnviadasRes>> ListaOrdenesEnviadas(string cadena)
        {
            List<OrdenesEnviadasRes> ordenesEnviadasRes = await _context.Compras
            .Where(w => w.EstadoCompra == cadena)
            .Select(s => new OrdenesEnviadasRes
            {
                Id = s.Id,
                CUO = "BDCO" + s.Id,
                FechaCotizacion = s.FechaCotizacion,
                SerieComprobante = s.SerieComprobante,
                NumeroComprobante = s.NumeroComprobante,
                Factura = (s.SerieComprobante ?? "") + (string.IsNullOrEmpty(s.SerieComprobante) || string.IsNullOrEmpty(s.NumeroComprobante) ? "" : "-") + (s.NumeroComprobante ?? ""),
                Guia = s.Guia ?? "",
                CodFacQbd = s.CodFacQBD,
                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                EstadoCompra = s.EstadoCompra,
                Familia = s.Familia,
                Usuario = s.Creador != null && s.Creador.Codigo != null ? s.Creador.Codigo : "",
                RutaFactura = s.ImgFactura,
            })
            .OrderByDescending(o => o.FechaCotizacion)
            .ToListAsync();
            if (ordenesEnviadasRes.Count() == 0) return new List<OrdenesEnviadasRes>();

            return ordenesEnviadasRes;
        }
    }
}
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
    public class CompraLaboratorioService : ICompraLaboratorioService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public CompraLaboratorioService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ObtenerCompraLabRes?> ObtenerCompraLaboratorio(int idCompra)
        {
            ObtenerCompraLabRes? obtenerDetalleCompraLabReq = await _context.Compras
            .Where(w => w.Id == idCompra)
            .Select(s => new ObtenerCompraLabRes()
            {
                CodigoProveedor = s.Proveedor != null && s.Proveedor.CodigoProvedor != null ? s.Proveedor.CodigoProvedor : "",
                Detalles = s.DetalleCompras != null ? s.DetalleCompras.Select(s2 => new ObtenerDetalleCompraLabRes()
                {
                    Id = s2.Id,
                    Reg = s2.Reg != null ? s2.Reg.Value : 0,
                    Codigo = s2.Insumo != null ? "MP-QBD-" + s2.IdInsumo.ToString("D4") : "",
                    DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
                    Coa = s2.Coa,
                    Lote = s2.Lote ?? "",
                    Um = s2.Um,
                    CantidadSolicitada = s2.CantidadSolicitada,
                    Potencia = s2.Potencia,
                    FechaFabricacion = s2.FechaFabricacion,
                    FechaVencimiento = s2.FechaVencimiento,
                    CondicionALmacenamiento = s2.CondicionAlmacenamiento ?? "",
                    TotalPaquetes = s2.Paquete != null ? s2.Paquete.CantidadPaquete : 0,
                    TotalPeso = s2.Paquete != null ? s2.Paquete.CantidadPaquete * s2.Paquete.PesoUnitario : 0
                }).ToList() : null
            }).FirstOrDefaultAsync();

            if (obtenerDetalleCompraLabReq == null) return null;

            return obtenerDetalleCompraLabReq;
        }
    }
}
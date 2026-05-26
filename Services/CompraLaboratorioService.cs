using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
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

        public async Task<int?> UpdateDetalleLab(int idCompra, List<ActualizarDetCompraLabReq> request)
        {
            IEnumerable<int> ids = request.Select(s => s.IdDetalle).ToList();
            List<DetalleCompraInsumo> detalleCompras = await _context.DetalleComprasInsumos
            .Where(w => w.IdCompra == idCompra && ids.Contains(w.Id)).ToListAsync();
            if (detalleCompras.Count == 0) return null;

            foreach (var item in detalleCompras)
            {
                ActualizarDetCompraLabReq? req = request.FirstOrDefault(f => f.IdDetalle == item.Id);
                if (req != null)
                    _mapper.Map(req, item);
            }
            return 1;
        }

        public async Task<ObtenerCompraLabRes?> GetCompraLab(int idCompra)
        {
            ObtenerCompraLabRes? obtenerDetalleCompraLabReq = await _context.Compras
            .Where(w => w.Id == idCompra)
            .Select(s => new ObtenerCompraLabRes()
            {
                CodigoProveedor = s.Proveedor != null && s.Proveedor.CodigoProvedor != null ? s.Proveedor.CodigoProvedor : "",
                // Detalles = s.DetalleCompras != null ? s.DetalleCompras.Select(s2 => new ObtenerDetalleCompraLabRes()
                // {
                //     Id = s2.Id,
                //     Reg = s2.Reg != null ? Alfanumerico.ConvertToBase36(s2.Reg.Value).PadLeft(4, '0') : "",
                //     Codigo = s2.Insumo != null ? "MP-QBD-" + s2.IdInsumo.ToString("D4") : "",
                //     DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
                //     Coa = s2.Coa,
                //     Lote = s2.Lote ?? "",
                //     Um = "g",
                //     CantidadSolicitada = s2.CantidadSolicitada * 1000,
                //     Potencia = s2.Potencia,
                //     FechaFabricacion = s2.FechaFabricacion,
                //     FechaVencimiento = s2.FechaVencimiento,
                //     CondicionALmacenamiento = s2.CondicionAlmacenamiento ?? "",
                //     TotalPaquetes = s2.Paquetes != null ? s2.Paquetes.Sum(s => s.CantidadPaquete) : 0,
                //     TotalPeso = s2.Paquetes != null ? s2.Paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario) : 0
                // }).ToList() : null
            }).FirstOrDefaultAsync();

            if (obtenerDetalleCompraLabReq == null) return null;

            return obtenerDetalleCompraLabReq;
        }

        public async Task<ObtenerCompraLab2Res> GetDetalleCompraLab(int IdCompra)
        {
            ObtenerCompraLab2Res? response = await _context.Compras
           .Where(w => w.Id == IdCompra)
           .Select(s => new ObtenerCompraLab2Res()
           {
               CodigoProveedor = s.Proveedor != null && s.Proveedor.CodigoProvedor != null ? s.Proveedor.CodigoProvedor : "",
               //    Detalles = s.DetalleCompras != null ? s.DetalleCompras.Select(s2 => new ObtenerDetalleCompraLab2Res()
               //    {
               //        Conformidad = s2.Conformidad == true ? "SI" : "NO",
               //        Reg = s2.Reg != null ? Alfanumerico.ConvertToBase36(s2.Reg.Value).PadLeft(4, '0') : "",
               //        CodigoInsumo = s2.Insumo != null ? "MP-QBD-" + s2.IdInsumo.ToString("D4") : "",
               //        DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
               //        Coa = s2.Coa,
               //        Lote = s2.Lote ?? "",
               //        Um = "g",
               //        CantidadSolicitada = s2.CantidadSolicitada * 1000,
               //        Potencia = s2.Potencia,
               //        FechaFabricacion = s2.FechaFabricacion,
               //        FechaVencimiento = s2.FechaVencimiento,
               //        CantidadPaquetes = s2.Paquetes != null ? s2.Paquetes.Sum(s => s.CantidadPaquete) : 0m,
               //        CantidadRecibida = s2.Paquetes != null ? s2.Paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario) : 0m,
               //    }).ToList() : null
           }).FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontró la compra");

            return response;
        }

        public async Task<EtiquetaCompraLabRes> GetEtiquetaCompraLab(int idCompra)
        {
            EtiquetaCompraLabRes? response = await _context.DetalleComprasInsumos
            .Where(w => w.Id == idCompra)
            .Select(s => new EtiquetaCompraLabRes()
            {
                Familia = (s.Insumo != null && s.Insumo.Familia != null) ? s.Insumo.Familia.Abreviatura : "",
                Tara = s.Paquetes != null ? s.Paquetes.Sum(s => s.Tara) : 0m
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontro Detalle Compra");

            return response;
        }
    }
}
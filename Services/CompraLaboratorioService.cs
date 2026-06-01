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

        public async Task<int> UpdateDetalleLab(int idCompra, List<ActualizarDetCompraLabReq> request)
        {
            IEnumerable<int> ids = request.Select(s => s.IdDetalle).ToList();
            List<CompraInsumos> detalleCompras = await _context.CompraInsumos
            .Where(w => w.IdCompra == idCompra && ids.Contains(w.Id)).ToListAsync();

            if (detalleCompras.Count == 0) throw new NotFoundException("No se encontro");

            foreach (var item in detalleCompras)
            {
                ActualizarDetCompraLabReq? req = request.FirstOrDefault(f => f.IdDetalle == item.Id);
                if (req != null)
                    _mapper.Map(req, item);
            }
            return 1;
        }

        // public async Task<ObtenerCompraLabRes?> GetCompraLab(int idCompra)
        // {
        //     ObtenerCompraLabRes? obtenerDetalleCompraLabReq = await _context.Compras
        //     .Where(w => w.Id == idCompra)
        //     .Select(s => new ObtenerCompraLabRes()
        //     {
        // CodigoProveedor = s.Proveedor != null && s.Proveedor.CodigoProvedor != null ? s.Proveedor.CodigoProvedor : "",
        // DetalleInsumos = s.DetalleCompraInsumos != null ? s.DetalleCompraInsumos.Select(s2 => new CompraLabDetInsumoRes()
        // {
        //     Id = s2.Id,
        //     Reg = Alfanumerico.ConvertToBase36(s2.Id).PadLeft(4, '0'),
        //     Codigo = s2.Insumo != null ? "MP-QBD-" + s2.IdInsumo.ToString("D4") : "",
        //     DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
        //     Coa = s2.Coa,
        //     Lote = s2.Lote ?? "",
        //     Um = "g",
        //     CantidadRecibida = s2.CantidadSolicitada * 1000,
        //     Potencia = s2.Potencia,
        //     FechaFabricacion = s2.FechaFabricacion,
        //     FechaVencimiento = s2.FechaVencimiento,
        //     CondicionALmacenamiento = s2.CondicionAlmacenamiento ?? "",
        //     TotalPaquetes = s2.Paquetes != null ? s2.Paquetes.Sum(s => s.CantidadPaquete) : 0,
        //     TotalPeso = s2.Paquetes != null ? s2.Paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario) : 0
        // }).ToList() : [],
        // DetalleEmpaques = s.DetalleCompraEmpaques != null ? s.DetalleCompraEmpaques.Select(s3 => new CompraLabDetEmpaquesRes()
        // {
        //     Id = s3.Id,
        //     Reg = Alfanumerico.ConvertToBase36(s3.Id).PadLeft(4, '0'),
        //     Codigo = s3.Empaque != null ? "" + s3.IdEmpaque.ToString("D4") : "",
        //     DescripcionQBD = s3.Empaque != null ? s3.Empaque.Descripcion ?? "" : "",
        //     Coa = s3.Coa,
        //     Lote = s3.Lote ?? "",
        //     Um = "g",
        //     CantidadRecibida = s3.CantidadSolicitada * 1000,
        //     FechaFabricacion = s3.FechaFabricacion,
        //     FechaVencimiento = s3.FechaVencimiento,
        //     CondicionALmacenamiento = s3.CondicionAlmacenamiento ?? "",
        //     TotalPaquetes = s3.Paquetes != null ? s3.Paquetes.Sum(s => s.CantidadPaquete) : 0,
        //     TotalPeso = s3.Paquetes != null ? s3.Paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario) : 0
        // }) : []
        //     }).FirstOrDefaultAsync();

        //     if (obtenerDetalleCompraLabReq == null) return null;

        //     return obtenerDetalleCompraLabReq;
        // }

        public async Task<CompraLabIdRes> GetDetalleCompraLab(int IdCompra)
        {
            CompraLabIdRes? response = await _context.Compras
           .Where(w => w.Id == IdCompra)
           .Select(s => new CompraLabIdRes()
           {
               CodigoProveedor = s.Proveedor != null && s.Proveedor.CodigoProvedor != null ? s.Proveedor.CodigoProvedor : "",
               ListaInsumos = s.CompraInsumos != null ? s.CompraInsumos.Select(s2 => new CompraLabDetInsumosRes()
               {
                   Id = s2.Id,
                   Familia = (s2.Insumo != null && s2.Insumo.Familia != null) ? s2.Insumo.Familia.Abreviatura : "",
                   Conformidad = s2.Conformidad == true ? "SI" : "NO",
                   Reg = Alfanumerico.ConvertToBase36(s2.Id).PadLeft(4, '0'),
                   CodigoInsumo = s2.Insumo != null ? "MP-QBD-" + s2.IdInsumo.ToString("D4") : "",
                   DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
                   Coa = s2.Coa,
                   Lote = s2.Lote ?? "",
                   Um = "g",
                   CantidadSolicitada = s2.CantidadSolicitada * 1000,
                   Potencia = s2.Potencia,
                   FechaFabricacion = s2.FechaFabricacion,
                   FechaVencimiento = s2.FechaVencimiento,
                   CantidadPaquetes = s2.PaqueteInsumos != null ? s2.PaqueteInsumos.Sum(s => s.Paquete != null ? s.Paquete.CantidadPaquete : 0) : 0m,
                   CantidadRecibida = s2.PaqueteInsumos != null ? s2.PaqueteInsumos.Sum(s => s.Paquete != null ? s.Paquete.PesoUnitario : 0) : 0m,
               }).ToList() : null,
               ListaEmpaques = s.CompraEmpaques != null ? s.CompraEmpaques.Select(s2 => new CompraLabDetEmpRes()
               {
                   Id = s2.Id,
                   Familia = (s2.Empaque != null && s2.Empaque.Familia != null) ? s2.Empaque.Familia.Abreviatura : "",
                   Conformidad = s2.Conformidad == true ? "SI" : "NO",
                   Reg = Alfanumerico.ConvertToBase36(s2.Id).PadLeft(4, '0'),
                   Coa = s2.Coa != null ? s2.Coa.Value : false,
                   Codigo = s2.Empaque != null ? "ME-QBD-" + s2.IdEmpaque.ToString("D4") : "",
                   DescripcionQBD = s2.Empaque != null ? s2.Empaque.Descripcion ?? "" : "",
                   Lote = s2.Lote ?? "",
                   Um = "g",
                   CantidadSolicitada = s2.CantidadSolicitada * 1000,
                   FechaFabricacion = s2.FechaFabricacion,
                   FechaVencimiento = s2.FechaVencimiento,
                   CantidadPaquetes = s2.PaqueteEmpaques != null ? s2.PaqueteEmpaques.Sum(s => s.Paquete != null ? s.Paquete.CantidadPaquete : 0) : 0m,
                   CantidadRecibida = s2.PaqueteEmpaques != null ? s2.PaqueteEmpaques.Sum(s => s.Paquete != null ? s.Paquete.PesoUnitario : 0) : 0m,
               }).ToList() : null
           }).FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontró la compra");

            return response;
        }

        public async Task<EtiquetaCompra> GetEtiquetaCompraInsumo(int idCompraInsumo)
        {
            EtiquetaCompra? response = await _context.CompraInsumos
            .Where(w => w.Id == idCompraInsumo)
            .Select(s => new EtiquetaCompra()
            {
                Familia = (s.Insumo != null && s.Insumo.Familia != null) ? s.Insumo.Familia.Abreviatura : "",
                Tara = s.PaqueteInsumos != null ? s.PaqueteInsumos.Sum(s => s.Paquete != null ? s.Paquete.Tara : 0) : 0m
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontro Compra Insumo");

            return response;
        }
        public async Task<EtiquetaCompra> GetEtiquetaCompraEmpaque(int idCompraEmpaque)
        {
            EtiquetaCompra? response = await _context.CompraEmpaques
            .Where(w => w.Id == idCompraEmpaque)
            .Select(s => new EtiquetaCompra()
            {
                Familia = (s.Empaque != null && s.Empaque.Familia != null) ? s.Empaque.Familia.Abreviatura : "",
                Tara = s.PaqueteEmpaques != null ? s.PaqueteEmpaques.Sum(s => s.Paquete != null ? s.Paquete.Tara : 0) : 0m
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontro Compra Empaque");

            return response;
        }
        public async Task<List<LabListaRes>> Listar(string[] cadena)
        {
            List<LabListaRes> ordenesEnviadasRes = await _context.Compras
            .Where(w => cadena.Contains(w.EstadoCompra))
            .Select(s => new LabListaRes
            {
                Id = s.Id,
                CUO = "OC" + s.Id,
                FechaCotizacion = s.FechaCotizacion,
                FechaFactura = s.FechaFactura,
                Factura = (s.SerieComprobante ?? "") + (string.IsNullOrEmpty(s.SerieComprobante) || string.IsNullOrEmpty(s.NumeroComprobante) ? "" : "-") + (s.NumeroComprobante ?? ""),
                CodFacQbd = s.CodFacQBD,
                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                EstadoCompra = s.EstadoCompra,
                Familia = s.Familia,
                Guia = s.Guia ?? "",
                ImgFactura = s.ImgFactura,
            })
            .OrderByDescending(o => o.FechaCotizacion)
            .ToListAsync();

            if (ordenesEnviadasRes.Count() == 0) return new List<LabListaRes>();

            return ordenesEnviadasRes;
        }
    }
}
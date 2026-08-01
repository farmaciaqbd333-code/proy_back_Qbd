using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.DetalleCompraLab;
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

        public async Task UpdateDetalleLab(int idCompra, ActualizarDetCompraLabReq request)
        {
            int x = 0;
            if (request.Insumos.Any())
            {
                List<ActualizarInsumoReq> insumos = request.Insumos;
                IEnumerable<int> idInsumos = insumos.Select(s => s.IdCompraInsumo).ToList();

                List<CompraInsumos> detalleCompras = await _context.CompraInsumos
                .Where(w => w.IdCompra == idCompra && idInsumos.Contains(w.Id)).ToListAsync();
                if (detalleCompras.Count == 0) throw new NotFoundException("No se encontro");

                foreach (var item in detalleCompras)
                {
                    ActualizarInsumoReq? req = insumos.FirstOrDefault(f => f.IdCompraInsumo == item.Id);
                    if (req != null)
                    {
                        new DetalleCompraLabMapper().ActualizarInsumo(req, item);
                        x = 1;
                    }
                }
            }
            if (request.Empaques.Any())
            {
                List<ActualizarEmpaqueReq> empaques = request.Empaques;
                IEnumerable<int> idEmpaques = empaques.Select(s => s.IdCompraEmpaque).ToList();

                List<CompraEmpaque> compraEmpaque = await _context.CompraEmpaques
                .Where(w => w.IdCompra == idCompra && idEmpaques.Contains(w.Id)).ToListAsync();
                if (compraEmpaque.Count == 0) throw new NotFoundException("No se encontro");

                foreach (var item in compraEmpaque)
                {
                    ActualizarEmpaqueReq? req = empaques.FirstOrDefault(f => f.IdCompraEmpaque == item.Id);
                    if (req != null)
                    {
                        new DetalleCompraLabMapper().ActualizarEmpaque(req, item);
                        x = 1;
                    }
                }
            }
            if (x == 1)
            {
                Compra? compra = await _context.Compras.FindAsync(idCompra) ?? throw new NotFoundException("No se encontro Compra");
                compra.FechaLab = DateTime.Now;
                await _context.SaveChangesAsync();
            }

        }

        public async Task<ObtenerCompraLabRes> ModalPaquetes(int idCompra)
        {
            ObtenerCompraLabRes? obtenerDetalleCompraLabReq = await _context.Compras
            .AsNoTracking()
            .Where(w => w.Id == idCompra)
            .Select(s => new ObtenerCompraLabRes()
            {
                CodigoProveedor = s.Proveedor != null && s.Proveedor.CodigoProvedor != null ? s.Proveedor.CodigoProvedor : "",
                Ruc = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                CodFacQbd = s.CodFacQBD,
                DetalleInsumos = s.CompraInsumos != null ? s.CompraInsumos.Select(s2 => new CompraLabInsumoModalRes()
                {
                    Id = s2.Id,
                    Reg = Alfanumerico.ConvertToBase36(s2.Id).PadLeft(4, '0'),
                    Familia = (s2.Insumo != null && s2.Insumo.Familia != null) ? s2.Insumo.Familia.Abreviatura : "",
                    Codigo = s2.IdInsumo.ToString(),
                    DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
                    Coa = s2.Coa,
                    Lote = s2.Lote ?? "",
                    Um = (s2.Um != null && (s2.Um.ToUpper() == "L" || s2.Um.ToUpper() == "LITRO")) ? "ML" : (s2.Um ?? "").ToUpper(),
                    CantidadRecibida = (s2.Um != null && (s2.Um.ToUpper() == "L" || s2.Um.ToUpper() == "LITRO")) ? s2.CantidadSolicitada * 1000 : s2.CantidadSolicitada,
                    Potencia = s2.Potencia,
                    FechaFabricacion = s2.FechaFabricacion,
                    FechaVencimiento = s2.FechaVencimiento,
                    CondicionALmacenamiento = s2.CondicionAlmacenamiento ?? "",
                    TotalPaquetes = s2.PaqueteInsumos != null ? s2.PaqueteInsumos.Sum(s => s.Paquete != null ? s.Paquete.CantidadPaquete : 0) : 0,
                    TotalPeso = s2.PaqueteInsumos != null ? s2.PaqueteInsumos.Sum(s => s.Paquete != null ? (s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario) : 0) : 0,
                    Fabricante = s2.Fabricante != null ? $"{s2.Fabricante.Codigo ?? s2.Fabricante.Nombre} ({s2.Fabricante.Pais})" : "",
                    Densidad = s2.Insumo != null ? s2.Insumo.Densidad : null,
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    Observacion = s2.Observacion ?? ""
                }).ToList() : new List<CompraLabInsumoModalRes>(),
                DetalleEmpaques = s.CompraEmpaques != null ? s.CompraEmpaques.Select(s3 => new CompraLabEmpaqueModalRes()
                {
                    Id = s3.Id,
                    Reg = Alfanumerico.ConvertToBase36(s3.Id).PadLeft(4, '0'),
                    Familia = (s3.Empaque != null && s3.Empaque.Familia != null) ? s3.Empaque.Familia.Abreviatura : "",
                    Codigo = s3.IdEmpaque.ToString(),
                    DescripcionQBD = s3.Empaque != null ? s3.Empaque.Descripcion ?? "" : "",
                    Coa = s3.Coa,
                    Lote = s3.Lote ?? "",
                    Um = (s3.Um != null && (s3.Um.ToUpper() == "L" || s3.Um.ToUpper() == "LITRO")) ? "ML" : (s3.Um ?? "").ToUpper(),
                    CantidadRecibida = (s3.Um != null && (s3.Um.ToUpper() == "L" || s3.Um.ToUpper() == "LITRO")) ? s3.CantidadSolicitada * 1000 : s3.CantidadSolicitada,
                    FechaFabricacion = s3.FechaFabricacion,
                    FechaVencimiento = s3.FechaVencimiento,
                    CondicionALmacenamiento = s3.CondicionAlmacenamiento ?? "",
                    TotalPaquetes = s3.PaqueteEmpaques != null ? s3.PaqueteEmpaques.Sum(s => s.Paquete != null ? s.Paquete.CantidadPaquete : 0) : 0,
                    TotalPeso = s3.PaqueteEmpaques != null ? s3.PaqueteEmpaques.Sum(s => s.Paquete != null ? (s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario) : 0) : 0,
                    Fabricante = s3.Fabricante != null ? $"{s3.Fabricante.Codigo ?? s3.Fabricante.Nombre} ({s3.Fabricante.Pais})" : "",
                    DescripcionFactura = s3.DescripcionFactura ?? "",
                    Observacion = s3.Observacion ?? ""
                }).ToList() : new List<CompraLabEmpaqueModalRes>()
            }).FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontro Compra");

            return obtenerDetalleCompraLabReq;
        }

        public async Task<CompraLabDetIdRes> GetDetalleCompraLab(int idCompra)
        {
            var response = await _context.Compras
                .AsNoTracking()
                .Where(c => c.Id == idCompra)
                .Select(c => new CompraLabDetIdRes
                {
                    CodigoProveedor = c.Proveedor!.CodigoProvedor ?? "",
                    Ruc = c.Proveedor != null ? c.Proveedor.NumeroProv : "",
                    NumProvedor = c.Proveedor != null ? c.Proveedor.NumeroProv : "",
                    CodFacQbd = c.CodFacQBD,

                    ListaInsumos = c.CompraInsumos.Select(i => new CompraLabDetInsumosRes
                    {
                        Id = i.Id,
                        Familia = i.Insumo != null ? i.Insumo.Familia!.Abreviatura : "",
                        Conformidad = (bool)i.Conformidad ? "SI" : "NO",
                        CodigoInsumo = i.IdInsumo.ToString(),
                        DescripcionQBD = i.Insumo != null ? i.Insumo.Descripcion : "",
                        Coa = i.Coa,
                        Lote = i.Lote ?? "",
                        Um = i.Um != null &&
                             (i.Um.ToUpper() == "L" || i.Um.ToUpper() == "LITRO")
                                ? "ML"
                                : (i.Um ?? "").ToUpper(),
                        Potencia = i.Potencia,
                        FechaFabricacion = i.FechaFabricacion,
                        FechaVencimiento = i.FechaVencimiento,
                        CantidadPaquetes = i.PaqueteInsumos.Sum(p => p.Paquete != null ? p.Paquete.CantidadPaquete : 0),
                        CantidadRecibida = i.PaqueteInsumos.Sum(p => p.Paquete != null ? p.Paquete.PesoUnitario : 0),
                        Densidad = i.Insumo != null ? i.Insumo.Densidad : null,
                        DescripcionFactura = i.DescripcionFactura ?? "",
                        Fabricante = i.Fabricante != null
                            ? $"{i.Fabricante.Codigo ?? i.Fabricante.Nombre} ({i.Fabricante.Pais})"
                            : "",
                        CondicionAlmacenamiento = i.CondicionAlmacenamiento ?? ""
                    }).ToList(),

                    ListaEmpaques = c.CompraEmpaques.Select(e => new CompraLabDetEmpRes
                    {
                        Id = e.Id,
                        Familia = e.Empaque != null ? e.Empaque.Familia!.Abreviatura : "",
                        Conformidad = (bool)e.Conformidad ? "SI" : "NO",
                        Codigo = e.IdEmpaque.ToString(),
                        Coa = e.Coa ?? false,
                        DescripcionQBD = e.Empaque != null
    ? (e.Empaque.Descripcion ?? "")
    : "",
                        Lote = e.Lote ?? "",
                        Um = e.Um != null &&
                             (e.Um.ToUpper() == "L" || e.Um.ToUpper() == "LITRO")
                                ? "ML"
                                : (e.Um ?? "").ToUpper(),
                        FechaFabricacion = e.FechaFabricacion,
                        FechaVencimiento = e.FechaVencimiento,
                        CantidadPaquetes = e.PaqueteEmpaques.Sum(p => p.Paquete != null ? p.Paquete.CantidadPaquete : 0),
                        CantidadRecibida = e.PaqueteEmpaques.Sum(p => p.Paquete != null ? p.Paquete.PesoUnitario : 0),
                        DescripcionFactura = e.DescripcionFactura ?? "",
                        Fabricante = e.Fabricante != null
                            ? $"{e.Fabricante.Codigo ?? e.Fabricante.Nombre} ({e.Fabricante.Pais})"
                            : "",
                        CondicionAlmacenamiento = e.CondicionAlmacenamiento ?? ""
                    }).ToList()
                })
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException("No se encontró la compra");

            // Lógica que EF Core no puede traducir
            foreach (var item in response.ListaInsumos)
                item.Reg = Alfanumerico.ConvertToBase36(item.Id).PadLeft(4, '0');

            foreach (var item in response.ListaEmpaques)
                item.Reg = Alfanumerico.ConvertToBase36(item.Id).PadLeft(4, '0');

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
            .Where(w => cadena.Contains(w.EstadoCompra) && w.CompraInsumos != null && w.CompraEmpaques != null)
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
                Ruc = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                Usuario = (s.Modificador != null && s.Modificador.Codigo != null)
                    ? s.Modificador.Codigo
                    : (s.Creador != null && s.Creador.Codigo != null)
                        ? s.Creador.Codigo
                        : "",
                FechaLab = s.FechaLab,
            })
            .OrderByDescending(o => o.FechaCotizacion)
            .ToListAsync();

            if (ordenesEnviadasRes.Count() == 0) return new List<LabListaRes>();

            return ordenesEnviadasRes;
        }

    }
}
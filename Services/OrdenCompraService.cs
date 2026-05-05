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
        private readonly IDetalleOrdenCompraService _serviceDOC;
        private readonly IMapper _mapper;
        public OrdenCompraService(ApiContext context, IMapper mapper, IDetalleOrdenCompraService serviceDOC)
        {
            _context = context;
            _mapper = mapper;
            _serviceDOC = serviceDOC;
        }

        public async Task<ObtenerOrdenOCompraRes?> ObtenerDetalleOrdenOCompra(int id)
        {
            ObtenerOrdenOCompraRes? response = await _context.Compras
                        .Where(w => w.Id == id)
                        .Select(s => new ObtenerOrdenOCompraRes
                        {
                            TC = s.TipoCambio.ToString(),
                            Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                            Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                            DetalleOrdenCompras = s.DetalleCompras == null ? null : s.DetalleCompras.Select(s2 => new ObtenerDetalleOrdenOCompraRes
                            {
                                Id = s2.Id,
                                IdInsumo = s2.IdInsumo,
                                Codigo = s2.IdInsumo.ToString(),
                                DescripcionQBD = s2.Insumo == null || s2.Insumo.Descripcion == null ? "" : s2.Insumo.Descripcion,
                                DescripcionFactura = s2.DescripcionFac,
                                CantidadSolicitada = s2.CantidadSolicitada,
                                UM = s2.Um,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal
                            }).ToList(),
                            IdProveedor = s.IdProveedor,
                            IncluyeImpuesto = s.Igv,
                            Observaciones = s.Observaciones,
                            Familia = s.Familia,
                            Responsable = s.Sede != null ? (_context.Personas.Where(p => p.Id.ToString() == s.Sede.Encargado).Select(p => p.NombreCompleto).FirstOrDefault() ?? s.Sede.Encargado) : "",
                            ISC = s.Isc,
                            ICBP = s.Icbp,
                            Guia = s.Guia

                        }).FirstOrDefaultAsync();

            if (response == null)
            {
                return null;
            }
            return response;
        }

        public async Task<List<OrdenesYComprasRes>> ListaOrdenesYCompras()
        {
            List<OrdenesYComprasRes> response = await _context.Compras
                            .Select(s => new OrdenesYComprasRes
                            {
                                Id = s.Id,
                                CUO = "OC-" + s.Id,
                                FechaCotizacion = s.FechaCotizacion,
                                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                                Valor = s.DetalleCompras != null ? s.Valor : 0,
                                Total = s.DetalleCompras != null ? s.Total : 0,
                                Moneda = s.Moneda,
                                CodFacQbd = s.CodFacQBD,
                                Familia = s.Familia,
                                Factura = s.SerieComprobante + s.NumeroComprobante,
                                EstadoPago = s.Modalidad,
                                Usuario = s.Creador != null ? (s.Creador.Codigo ?? s.Creador.Id.ToString()) : "N/A",
                                EstadoCompra = s.EstadoCompra
                            })
                            .OrderByDescending(o => o.Id)
                            .ToListAsync();

            return response;
        }
        public async Task<OrdenesYComprasRes?> ObtenerOrdenOCompra(int id)
        {
            OrdenesYComprasRes? response = await _context.Compras
                            .Select(s => new OrdenesYComprasRes
                            {
                                Id = s.Id,
                                CUO = "OC-" + s.Id,
                                FechaCotizacion = s.FechaCotizacion,
                                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                                Valor = s.DetalleCompras != null ? s.Valor : 0,
                                Total = s.DetalleCompras != null ? s.Total : 0,
                                Moneda = s.Moneda,
                                CodFacQbd = s.CodFacQBD,
                                Familia = s.Familia,
                                Factura = s.SerieComprobante + s.NumeroComprobante,
                                EstadoPago = s.Modalidad,
                                Usuario = s.Creador != null ? (s.Creador.Codigo ?? s.Creador.Id.ToString()) : "N/A",
                                EstadoCompra = s.EstadoCompra
                            })
                            .OrderByDescending(o => o.Id)
                            .FirstOrDefaultAsync();
            if (response == null)
                return null;

            return response;
        }

        public async Task<int?> CrearOrdenDeCompra(OrdenCompraCreateReq request)
        {
            try
            {
                Compra compra = _mapper.Map<Compra>(request);
                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                foreach (var item in request.Detalle)
                {
                    DetalleCompra detalleCompra = _mapper.Map<DetalleCompra>(item);
                    detalleCompra.IdCompra = compra.Id;
                    _context.DetalleCompras.Add(detalleCompra);
                }
                await _context.SaveChangesAsync();

                return compra.Id;
            }
            catch (Exception)
            {
                return null;
            }

        }

        public async Task<string?> EliminarOrdenOCompraOCompra(int id)
        {
            Compra? compra = await _context.Compras.FindAsync(id);
            if (compra == null)
                return null;

            _context.Remove(compra);
            await _context.SaveChangesAsync();

            return "Orden compra eliminado exitosamente";
        }

        public async Task<OrdenesYComprasRes?> ActualizarOrdenDeCompra(int idOC, OrdenCompraUpdateReq request)
        {

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                if (request.DetallesNuevos != null)
                    await _serviceDOC.CrearDetalleOrdenDeCompra(idOC, request.IdModificadorCreador, request.DetallesNuevos);
                if (request.DetallesEliminados != null)
                    await _serviceDOC.EliminarDetalleOrdenDeCompra(request.DetallesEliminados);
                Compra? compra = await _context.Compras.FindAsync(idOC);
                _mapper.Map(request, compra);
                if (request.Detalles != null)
                    foreach (var item in request.Detalles)
                    {
                        DetalleCompra? detalleCompra = await _context.DetalleCompras.FindAsync(item.Id);
                        _mapper.Map(item, detalleCompra);
                    }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                OrdenesYComprasRes? response = await ObtenerOrdenOCompra(idOC);
                return response;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }


        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.Meson;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Util;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class MesonService : IMesonService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public MesonService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<string> CompletarDatos(int ordenCompraId, MesonConvertirReq request)
        {
            if (request == null) throw new BadRequestException("Request vacío");
            //Actualizar a compra
            Compra? compra = await _context.Compras.FindAsync(ordenCompraId);
            if (compra == null) throw new NotFoundException("No se encontro compra");
            _mapper.Map(request, compra);

            if (request.DetallesOtros != null && request.DetallesOtros.Count != 0)
            {
                //Actualizar detalles de la compra
                var idsDetalleOtros = request.DetallesOtros
                                        .Select(s => s.IdDetalleOtro);
                List<CompraOtros> otros = await _context.CompraOtros
                    .Where(w => idsDetalleOtros.Contains(w.Id))
                    .ToListAsync();


                foreach (var item in otros)
                {
                    MesonDetOtrosConvReq? item2 = request.DetallesOtros.FirstOrDefault(w => w.IdDetalleOtro == item.Id);
                    if (item2 != null)
                    {
                        var mapper = new MesonMapper();
                        mapper.ActualizarOtros(item2, item);
                        item.IdModificador = request.IdModificador;
                    }
                }
            }

            if (request.DetallesInsumos != null && request.DetallesInsumos.Any())
            {
                //Actualizar detalles de la compra
                var idsDetalleInsumos = request.DetallesInsumos
                                        .Select(s => s.IdDetalleInsumo);
                List<CompraInsumos> detallesInsumos = await _context.CompraInsumos
                    .Where(w => idsDetalleInsumos.Contains(w.Id))
                    .ToListAsync();


                foreach (var item in detallesInsumos)
                {
                    MesonDetInsumoConvReq? item2 = request.DetallesInsumos.FirstOrDefault(w => w.IdDetalleInsumo == item.Id);
                    if (item2 != null)
                    {
                        var mapper = new MesonMapper();
                        mapper.ActualizarInsumos(item2, item);
                        item.IdModificador = request.IdModificador;
                    }
                }
            }
            if (request.DetallesProductos.Any())
            {
                //Actualizar detalles de la compra
                var idsDetalleProductos = request.DetallesProductos
                                        .Select(s => s.IdDetalleProducto);
                List<CompraProductos> detallesProductos = await _context.CompraProductos
                    .Where(w => idsDetalleProductos.Contains(w.Id))
                    .ToListAsync();


                foreach (var item in detallesProductos)
                {
                    MesonDetProductoConvReq? item2 = request.DetallesProductos.FirstOrDefault(w => w.IdDetalleProducto == item.Id);
                    if (item2 != null)
                    {
                        var mapper = new MesonMapper();
                        mapper.ActualizarProductos(item2, item);
                        item.IdModificador = request.IdModificador;
                    }
                }
            }
            if (request.DetallesEconomatos.Any())
            {
                //Actualizar detalles de la compra
                var idsDetalleEconomatos = request.DetallesEconomatos
                                        .Select(s => s.IdDetalleEconomato);
                List<CompraEconomatos> detallesEconomatos = await _context.CompraEconomatos
                    .Where(w => idsDetalleEconomatos.Contains(w.Id))
                    .ToListAsync();


                foreach (var item in detallesEconomatos)
                {
                    MesonDetEconomatoConvReq? item2 = request.DetallesEconomatos.FirstOrDefault(w => w.IdDetalleEconomato == item.Id);
                    if (item2 != null)
                    {
                        var mapper = new MesonMapper();
                        mapper.ActualizarEconomatos(item2, item);
                        item.IdModificador = request.IdModificador;
                    }
                }
            }
            if (request.DetallesEmpaques.Any())
            {
                //Actualizar detalles de la compra
                var idsDetalleEmpaques = request.DetallesEmpaques
                                        .Select(s => s.IdDetalleEmpaque);
                List<CompraEmpaques> detallesEmpaques = await _context.CompraEmpaques
                    .Where(w => idsDetalleEmpaques.Contains(w.Id))
                    .ToListAsync();


                foreach (var item in detallesEmpaques)
                {
                    MesonDetEmpaqueConvReq? item2 = request.DetallesEmpaques.FirstOrDefault(w => w.IdDetalleEmpaque == item.Id);
                    if (item2 != null)
                    {
                        var mapper = new MesonMapper();
                        mapper.ActualizarEmpaques(item2, item);
                        item.IdModificador = request.IdModificador;
                    }
                }
            }


            await _context.SaveChangesAsync();

            return "Actualizacion Exitosa";
        }

        public async Task<MesonModalRes?> ObtenerDatosModal(int ordenCompraId)
        {
            MesonModalRes? response = await _context.Compras
            .Where(w => w.Id == ordenCompraId)
            .Select(s => new MesonModalRes
            {
                Id = s.Id,
                CUO = "OC" + s.Id,
                FechaEmision = s.FechaFactura,
                SerieComprobante = s.SerieComprobante,
                NumeroComprobante = s.NumeroComprobante,
                Guia = s.Guia,
                CodFacQBD = s.CodFacQBD,
                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                IdProveedor = s.IdProveedor,
                Familia = s.Familia,
                ListaOtros = s.CompraOtros != null ? s.CompraOtros.Select(s2 => new DetalleMesonOtrosRes
                {
                    Id = s2.Id,
                    Familia = s2.Clasificacion ?? "OTRO",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    CantidadRecibida = s2.CantidadRecibida ?? s2.CantidadSolicitada,
                    Um = s2.UnidadMedida,
                    Conformidad = s2.Conformidad
                }).ToList() : new List<DetalleMesonOtrosRes>(),
                ListaInsumos = s.CompraInsumos != null ? s.CompraInsumos.Select(s2 => new DetalleMesonInsumoRes
                {
                    Id = s2.Id,
                    Codigo = "MP-QBD-" + s2.IdInsumo,
                    DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    CantidadRecibida = s2.CantidadRecibida ?? s2.CantidadSolicitada,
                    Um = s2.Um,
                    Coa = s2.Coa,
                    Lote = s2.Lote,
                    RegistroSanitario = s2.RegistroSanitario,
                    FechaFabricacion = s2.FechaFabricacion,
                    FechaVencimiento = s2.FechaVencimiento,
                    Conformidad = s2.Conformidad,
                    IdFabricante = s2.IdFabricante,
                    Familia = (s2.Insumo != null && s2.Insumo.Familia != null) ? s2.Insumo.Familia.Abreviatura ?? "" : "",
                }).ToList() : new List<DetalleMesonInsumoRes>(),
                ListaEconomatos = s.CompraEconomatos != null ? s.CompraEconomatos.Select(s2 => new DetalleMesonEconomatosRes
                {
                    Id = s2.Id,
                    Codigo = s2.Economato != null ? "" + s2.Economato.Id : "",
                    DescripcionQBD = s2.Economato != null ? s2.Economato.Descripcion : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    IdFabricante = s2.IdFabricante,
                    CantidadRecibida = s2.CantidadRecibida ?? s2.CantidadSolicitada,
                    Um = s2.Um,
                    Conformidad = s2.Conformidad,
                    Familia = "ECO"
                }).ToList() : new List<DetalleMesonEconomatosRes>(),
                ListaProductos = s.CompraProductos != null ? s.CompraProductos.Select(s2 => new DetalleMesonProductosRes
                {
                    Id = s2.Id,
                    Codigo = s2.Producto != null ? "" + s2.Producto.Id : "",
                    DescripcionQbd = s2.Producto != null ? s2.Producto.Descripcion ?? "" : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    IdFabricante = s2.IdFabricante,
                    CantidadRecibida = s2.CantidadRecibida ?? s2.CantidadSolicitada,
                    Um = s2.Um,
                    Lote = s2.Lote ?? "",
                    RegistroSanitario = s2.RegistroSanitario ?? "",
                    FechaFabricacion = s2.FechaFabricacion,
                    FechaVencimiento = s2.FechaVencimiento,
                    Conformidad = s2.Conformidad,
                    Familia = "PT"
                }).ToList() : new List<DetalleMesonProductosRes>(),
                ListaEmpaques = s.CompraEmpaques != null ? s.CompraEmpaques.Select(s2 => new DetalleMesonEmpaquesRes
                {
                    Id = s2.Id,
                    Codigo = s2.Empaque != null ? "" + s2.Empaque.Id : "",
                    DescripcionQbd = s2.Empaque != null ? s2.Empaque.Descripcion ?? "" : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    IdFabricante = s2.IdFabricante,
                    CantidadRecibida = s2.CantidadRecibida ?? s2.CantidadSolicitada,
                    Um = s2.Um,
                    Coa = s2.Coa ?? false,
                    Lote = s2.Lote ?? "",
                    FechaFabricacion = s2.FechaFabricacion,
                    FechaVencimiento = s2.FechaVencimiento,
                    Conformidad = s2.Conformidad,
                    Familia = "ME"
                }).ToList() : new List<DetalleMesonEmpaquesRes>()
            }).FirstOrDefaultAsync();

            if (response == null) throw new NotFoundException("No se encontro orden de compra");

            return response;
        }
        public async Task<List<MesonListaRes>> ListarMeson(string[] cadena)
        {
            List<MesonListaRes> ordenesEnviadasRes = await _context.Compras
            .Where(w => cadena.Contains(w.EstadoCompra))
            .Select(s => new MesonListaRes
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
            if (ordenesEnviadasRes.Count() == 0) return new List<MesonListaRes>();

            return ordenesEnviadasRes;
        }

        public async Task<MesonDetalleRes> ObtenerDetalleOrdenOCompra(int id)
        {
            MesonDetalleRes? response = await _context.Compras
                        .Where(w => w.Id == id)
                        .Select(s => new MesonDetalleRes
                        {
                            TC = s.TipoCambio.ToString(),
                            Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                            Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                            // DetalleOrdenCompras = s.DetalleCompras == null ? null : s.DetalleCompras.Select(s2 => new DetInsumoRes2
                            // {
                            //     Id = s2.Id,
                            //     Reg = Alfanumerico.ConvertToBase36(s2.Id).PadLeft(4, '0'),
                            //     IdInsumo = s2.IdInsumo,
                            //     Codigo = s2.IdInsumo.ToString(),
                            //     DescripcionQBD = s2.Insumo == null || s2.Insumo.Descripcion == null ? "" : s2.Insumo.Descripcion,
                            //     DescripcionFactura = s2.DescripcionFac,
                            //     CantidadSolicitada = s2.CantidadSolicitada,
                            //     UM = s2.Um,
                            //     CUnitario = s2.CostoUnitario,
                            //     CTotal = s2.CostoTotal,
                            //     Coa = s2.Coa,
                            //     Lote = s2.Lote,
                            //     RegistroSanitario = s2.RegistroSanitario,
                            //     Conforme = s2.Conformidad ?? false,
                            //     Familia = s2.Familia != null ? s2.Familia.Abreviatura : ""
                            // }).ToList(),
                            IdProveedor = s.IdProveedor,
                            IncluyeImpuesto = s.Igv,
                            Observaciones = s.Observaciones,
                            Familia = s.Familia,
                            Responsable = s.Sede != null ? (_context.Personas.Where(p => p.Id.ToString() == s.Sede.Encargado).Select(p => p.NombreCompleto).FirstOrDefault() ?? s.Sede.Encargado) : "",
                            ISC = s.Isc,
                            ICBP = s.Icbp,
                            Guia = s.Guia,
                            Modalidad = s.Modalidad

                        }).FirstOrDefaultAsync();

            if (response != null && response.DetalleOrdenCompras != null)
            {
                response.Familia = string.Join(", ", response.DetalleOrdenCompras
                    .Select(d => d.Familia)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Distinct());
            }

            if (response == null)
            {
                throw new NotFoundException("No se encontro");
            }

            return response;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        private readonly IOrdenCompraService _serviceOC;
        private readonly IMapper _mapper;
        public MesonService(ApiContext context, IMapper mapper, IOrdenCompraService serviceOC)
        {
            _context = context;
            _mapper = mapper;
            _serviceOC = serviceOC;
        }
        public async Task<MesonListaRes?> CompletarDatos(int ordenCompraId, MesonConvertirReq request)
        {
            //Actualizar a compra
            Compra? compra = await _context.Compras.FindAsync(ordenCompraId);
            if (compra == null) return null;
            _mapper.Map(request, compra);

            //Actualizar detalles de la compra
            var idsDetalleCompra = request.Detalles
                                    .Select(s => s.IdDetalleCompra);

            List<DetalleCompraInsumo> detalleCompras = await _context.DetalleComprasInsumos
                .Include(w => w.Insumo)
                .Where(w => idsDetalleCompra.Contains(w.Id))
                .ToListAsync();

            // Agrupacion por familias
            List<IdFamiliasRes> idFamilias = detalleCompras
            .GroupBy(g => g.Insumo == null ? 0 : g.Insumo.IdFamilia).Select(s => new IdFamiliasRes()
            {
                IdFamilia = s.Key ?? 0,
                Cantidad = s.Count()
            }).ToList();

            var idsFamiliasKeys = idFamilias.Select(f => f.IdFamilia).ToList();

            List<IdFamiliasMaxRes> ultimosId = await _context.DetalleComprasInsumos
            .Where(w => w.Insumo != null && idsFamiliasKeys.Contains(w.Insumo.IdFamilia ?? 0))
            .GroupBy(g => g.Insumo.IdFamilia)
            .Select(s => new IdFamiliasMaxRes
            {
                IdFamilia = s.Key ?? 0,
            })
            .ToListAsync();

            decimal sumaTotal = 0m;

            foreach (var item in detalleCompras)
            {
                MesonConvertirDetalleReq? item2 = request.Detalles.FirstOrDefault(w => w.IdDetalleCompra == item.Id);
                if (item2 != null)
                {
                    _mapper.Map(item2, item);
                    item.IdModificador = request.IdModificador;
                    item.CostoTotal = item.CostoUnitario * item2.Cantidad;
                    if (item.Insumo != null)
                    {
                        IdFamiliasMaxRes? idFamilia = ultimosId.FirstOrDefault(w => w.IdFamilia == item.Insumo.IdFamilia);
                        if (idFamilia == null) return null;
                        idFamilia.Ultimo++;
                    }
                    sumaTotal += item.CostoTotal;
                }
            }

            //Actualizar total de la compra
            compra.Valor = sumaTotal;
            compra.Total = (compra.Igv ? sumaTotal * 1.18m : sumaTotal) + compra.Isc + compra.Icbp;

            var nombresFamiliasConvert = await _context.Familias
                .Where(f => idsFamiliasKeys.Contains(f.Id))
                .Select(f => f.Abreviatura)
                .ToListAsync();
            compra.Familia = string.Join(", ", nombresFamiliasConvert);

            await _context.SaveChangesAsync();
            //Devolver dato actualizado de la compra
            MesonListaRes? response = await _context.Compras
                            .Where(w => w.Id == ordenCompraId)
                            .Select(s => new MesonListaRes
                            {
                                Id = s.Id,
                                CUO = "OC" + s.Id,
                                FechaCotizacion = s.FechaCotizacion,
                                FechaFactura = s.FechaFactura,
                                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                                CodFacQbd = s.CodFacQBD,
                                Familia = s.Familia,
                                Factura = s.SerieComprobante + s.NumeroComprobante,
                                ImgFactura = s.ImgFactura,
                                Guia = s.Guia,
                                EstadoCompra = s.EstadoCompra
                            })
                            .FirstOrDefaultAsync();
            if (response == null)
                return null;

            return response;
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
                ListaOtros = s.DetalleCompraOtros != null ? s.DetalleCompraOtros.Select(s2 => new DetalleMesonOtrosRes
                {
                    Id = s2.Id,
                    Familia = s2.Clasificacion ?? "OTRO",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    CantidadRecibida = s2.CantidadRecibida,
                    Um = s2.UnidadMedida,
                    Conformidad = s2.Conformidad
                }).ToList() : new List<DetalleMesonOtrosRes>(),
                ListaInsumos = s.DetalleCompraInsumos != null ? s.DetalleCompraInsumos.Select(s2 => new DetalleMesonInsumoRes
                {
                    Id = s2.Id,
                    Codigo = "MP-QBD-" + s2.IdInsumo,
                    DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    CantidadRecibida = s2.CantidadRecibida,
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
                ListaEconomatos = s.DetalleCompraEconomatos != null ? s.DetalleCompraEconomatos.Select(s2 => new DetalleMesonEconomatosRes
                {
                    Id = s2.Id,
                    Codigo = s2.Economato != null ? "" + s2.Economato.Id : "",
                    DescripcionQBD = s2.Economato != null ? s2.Economato.Descripcion : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    IdFabricante = s2.IdFabricante,
                    CantidadRecibida = s2.CantidadRecibida,
                    Um = s2.Um,
                    Conformidad = s2.Conformidad,
                    Familia = "ECO"
                }).ToList() : new List<DetalleMesonEconomatosRes>(),
                ListaProductos = s.DetalleCompraProductos != null ? s.DetalleCompraProductos.Select(s2 => new DetalleMesonProductosRes
                {
                    Id = s2.Id,
                    Codigo = s2.Producto != null ? "" + s2.Producto.Id : "",
                    DescripcionQbd = s2.Producto != null ? s2.Producto.Descripcion ?? "" : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    IdFabricante = s2.IdFabricante,
                    CantidadRecibida = s2.CantidadRecibida,
                    Um = s2.Um,
                    Conformidad = s2.Conformidad,
                    Familia = "PT"
                }).ToList() : new List<DetalleMesonProductosRes>(),
                ListaEmpaques = s.DetalleCompraEmpaques != null ? s.DetalleCompraEmpaques.Select(s2 => new DetalleMesonEmpaquesRes
                {
                    Id = s2.Id,
                    Codigo = s2.Empaque != null ? "" + s2.Empaque.Id : "",
                    DescripcionQbd = s2.Empaque != null ? s2.Empaque.Descripcion ?? "" : "",
                    DescripcionFactura = s2.DescripcionFactura ?? "",
                    IdFabricante = s2.IdFabricante,
                    CantidadRecibida = s2.CantidadRecibida,
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
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
                ListaInsumos = new List<DetalleMesonInsumoRes>()
            }).FirstOrDefaultAsync();

            if (response == null) return null;

            // 1. Insumos
            var insumosList = await _context.DetalleComprasInsumos
                .Where(w => w.IdCompra == ordenCompraId)
                .Select(d => new DetalleMesonInsumoRes
                {
                    Id = d.Id,
                    Reg = Alfanumerico.ConvertToBase36(d.Id).PadLeft(4, '0'),
                    Codigo = d.IdInsumo.ToString(),
                    Descripcion = d.Insumo != null && d.Insumo.Descripcion != null ? d.Insumo.Descripcion : "",
                    DescripcionFactura = d.DescripcionFactura ?? "",
                    Cantidad = d.CantidadSolicitada,
                    Um = d.Um ?? "",
                    Coa = d.Coa,
                    Lote = d.Lote ?? "",
                    RegistroSanitario = d.RegistroSanitario ?? "",
                    FechaFabricacion = d.FechaFabricacion,
                    FechaVencimiento = d.FechaVencimiento,
                    Conformidad = d.Conformidad ?? false,
                    IdFabricante = d.IdFabricante,
                    NombreFabricante = d.Fabricante != null ? d.Fabricante.Nombre : "",
                    CodigoFabricante = d.Fabricante != null ? d.Fabricante.Codigo : "",
                    Familia = d.Insumo != null && d.Insumo.Familia != null ? d.Insumo.Familia.Abreviatura : "MP"
                })
                .ToListAsync();

            if (insumosList.Any())
            {
                response.ListaInsumos.AddRange(insumosList);
            }

            // 2. Empaques
            var empaquesList = await _context.DetalleCompraEmpaques
                .Where(w => w.IdCompra == ordenCompraId)
                .Select(d => new DetalleMesonInsumoRes
                {
                    Id = d.Id,
                    Reg = "",
                    Codigo = d.IdEmpaque.ToString(),
                    Descripcion = d.Empaque != null && d.Empaque.Descripcion != null ? d.Empaque.Descripcion : "",
                    DescripcionFactura = d.Empaque != null && d.Empaque.Descripcion != null ? d.Empaque.Descripcion : "",
                    Cantidad = d.CantidadSolicitada,
                    Um = d.Um ?? "",
                    Coa = false,
                    Lote = "",
                    RegistroSanitario = "",
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Conformidad = false,
                    IdFabricante = null,
                    NombreFabricante = "",
                    CodigoFabricante = "",
                    Familia = "ME"
                })
                .ToListAsync();

            if (empaquesList.Any())
            {
                response.ListaInsumos.AddRange(empaquesList);
            }

            // 3. Productos
            var productosList = await _context.DetalleCompraProductos
                .Where(w => w.IdCompra == ordenCompraId)
                .Select(d => new DetalleMesonInsumoRes
                {
                    Id = d.Id,
                    Reg = "",
                    Codigo = d.IdProducto.ToString(),
                    Descripcion = d.Producto != null && d.Producto.Descripcion != null ? d.Producto.Descripcion : "",
                    DescripcionFactura = d.Producto != null && d.Producto.Descripcion != null ? d.Producto.Descripcion : "",
                    Cantidad = d.CantidadSolicitada,
                    Um = d.Um ?? "",
                    Coa = false,
                    Lote = "",
                    RegistroSanitario = "",
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Conformidad = false,
                    IdFabricante = null,
                    NombreFabricante = "",
                    CodigoFabricante = "",
                    Familia = "PT"
                })
                .ToListAsync();

            if (productosList.Any())
            {
                response.ListaInsumos.AddRange(productosList);
            }

            // 4. Economatos
            var economatosList = await _context.DetalleCompraEconomatos
                .Where(w => w.IdCompra == ordenCompraId)
                .Select(d => new DetalleMesonInsumoRes
                {
                    Id = d.Id,
                    Reg = "",
                    Codigo = d.IdEconomato.ToString(),
                    Descripcion = d.Economato != null && d.Economato.Descripcion != null ? d.Economato.Descripcion : "",
                    DescripcionFactura = d.Economato != null && d.Economato.Descripcion != null ? d.Economato.Descripcion : "",
                    Cantidad = d.CantidadSolicitada,
                    Um = d.Um ?? "",
                    Coa = false,
                    Lote = "",
                    RegistroSanitario = "",
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Conformidad = false,
                    IdFabricante = null,
                    NombreFabricante = "",
                    CodigoFabricante = "",
                    Familia = "ECO"
                })
                .ToListAsync();

            if (economatosList.Any())
            {
                response.ListaInsumos.AddRange(economatosList);
            }

            // 5. Genérico
            var comprasList = await _context.DetalleCompraOtros
                .Where(w => w.IdCompra == ordenCompraId)
                .Select(d => new DetalleMesonInsumoRes
                {
                    Id = d.Id,
                    Reg = "",
                    Codigo = "",
                    Descripcion = d.Clasificacion ?? "",
                    DescripcionFactura = d.Clasificacion ?? "",
                    Cantidad = d.CantidadSolicitada,
                    Um = d.UnidadMedida ?? "",
                    Coa = false,
                    Lote = "",
                    RegistroSanitario = "",
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Conformidad = false,
                    IdFabricante = null,
                    NombreFabricante = "",
                    CodigoFabricante = "",
                    Familia = d.Clasificacion ?? "OTR"
                })
                .ToListAsync();

            if (comprasList.Any())
            {
                response.ListaInsumos.AddRange(comprasList);
            }

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
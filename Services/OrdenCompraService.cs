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
                                CTotal = s2.CostoTotal,
                                Coa = s2.Coa,
                                Lote = s2.Lote,
                                RegistroSanitario = s2.RegistroSanitario,
                                Conforme = s2.Conformidad ?? false,
                                Familia = s2.Familia != null ? s2.Familia.Abreviatura : "",
                                IdFabricante = s2.IdFabricante,
                                NombreFabricante = s2.Fabricante != null ? s2.Fabricante.Nombre : "",
                                CodigoFabricante = s2.Fabricante != null ? s2.Fabricante.Codigo : ""
                            }).ToList(),
                            IdProveedor = s.IdProveedor,
                            IncluyeImpuesto = s.Igv,
                            Observaciones = s.Observaciones,
                            Familia = s.Familia,
                            Responsable = s.Sede != null ? (_context.Personas.Where(p => p.Id.ToString() == s.Sede.Encargado).Select(p => p.NombreCompleto).FirstOrDefault() ?? s.Sede.Encargado) : "",
                            ISC = s.Isc,
                            ICBP = s.Icbp,
                            Guia = s.Guia,
                            Modalidad = s.Modalidad,
                            EstadoCompra = s.EstadoCompra

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
                                CUO = "OC" + s.Id,
                                FechaCotizacion = s.FechaCotizacion,
                                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                                Valor = s.Valor,
                                Total = s.Total,
                                Moneda = s.Moneda,
                                CodFacQbd = s.CodFacQBD,
                                Familia = s.Familia,
                                Factura = s.SerieComprobante + s.NumeroComprobante,
                                RutaFactura = s.ImgFactura,
                                EstadoPago = s.Modalidad,
                                Usuario = s.Creador != null ? (s.Creador.Codigo ?? s.Creador.Id.ToString()) : "N/A",
                                EstadoCompra = s.EstadoCompra
                            })
                            .OrderByDescending(o => o.Id)
                            .ToListAsync();

            if (response.Any())
            {
                var ids = response.Select(r => r.Id).ToList();
                var familiasPorCompra = await _context.DetalleCompras
                    .Where(dc => ids.Contains(dc.IdCompra) && dc.Familia != null)
                    .Select(dc => new { dc.IdCompra, dc.Familia!.Abreviatura })
                    .Distinct()
                    .ToListAsync();

                var dict = familiasPorCompra
                    .GroupBy(x => x.IdCompra)
                    .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Abreviatura)));

                foreach (var item in response)
                {
                    if (dict.TryGetValue(item.Id, out var fams))
                    {
                        item.Familia = fams;
                    }
                }
            }

            return response;
        }
        public async Task<OrdenesYComprasRes?> ObtenerOrdenOCompra(int id)
        {
            OrdenesYComprasRes? response = await _context.Compras
                            .Where(w => w.Id == id)
                            .Select(s => new OrdenesYComprasRes
                            {
                                Id = s.Id,
                                CUO = "OC" + s.Id,
                                FechaCotizacion = s.FechaCotizacion,
                                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                                Valor = s.Valor,
                                Total = s.Total,
                                Moneda = s.Moneda,
                                CodFacQbd = s.CodFacQBD,
                                Familia = s.Familia,
                                Factura = s.SerieComprobante + s.NumeroComprobante,
                                RutaFactura = s.ImgFactura,
                                EstadoPago = s.Modalidad,
                                Usuario = s.Creador != null ? (s.Creador.Codigo ?? s.Creador.Id.ToString()) : "N/A",
                                EstadoCompra = s.EstadoCompra
                            })
                            .FirstOrDefaultAsync();

            if (response != null)
            {
                var familias = await _context.DetalleCompras
                    .Where(dc => dc.IdCompra == response.Id && dc.Familia != null)
                    .Select(dc => dc.Familia!.Abreviatura)
                    .Distinct()
                    .ToListAsync();

                if (familias.Any())
                {
                    response.Familia = string.Join(", ", familias);
                }
            }

            return response;
        }

        public async Task<int?> CrearOrdenDeCompra(OrdenCreateReq request)
        {
            try
            {
                Compra compra = _mapper.Map<Compra>(request);
                compra.Valor = request.Detalle.Sum(s => s.CostoTotal);
                compra.Total = request.Igv ? (compra.Valor * 1.18m) + request.Isc + request.Icbp : compra.Valor + request.Isc + request.Icbp;

                var idsFamilias = request.Detalle.Select(s => s.IdFamilia).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
                var nombresFamilias = await _context.Familias
                    .Where(f => idsFamilias.Contains(f.Id))
                    .Select(f => f.Abreviatura)
                    .ToListAsync();
                compra.Familia = string.Join(", ", nombresFamilias);

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

        public async Task<OrdenesYComprasRes?> ActualizarOrdenDeCompra(int idOC, OrdenUpdateReq request)
        {

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                decimal valor = 0;
                decimal total = 0;
                if (request.DetallesNuevos != null)
                {
                    valor = request.DetallesNuevos.Sum(s => s.CostoTotal);
                    await _serviceDOC.CrearDetalleOrdenDeCompra(idOC, request.IdModificadorCreador, request.DetallesNuevos);
                }
                if (request.DetallesEliminados != null)
                    await _serviceDOC.EliminarDetalleOrdenDeCompra(request.DetallesEliminados);

                if (request.Detalles != null)
                {

                    foreach (var item in request.Detalles)
                    {
                        DetalleCompra? detalleCompra = await _context.DetalleCompras.FindAsync(item.Id);
                        if (detalleCompra != null)
                        {
                            _mapper.Map(item, detalleCompra);
                        }
                    }
                    valor += request.Detalles.Sum(s => s.CostoTotal);
                }

                total = (request.Igv == true ? valor * 1.18m : valor) + request.Isc + request.Icbp;

                Compra? compra = await _context.Compras.FindAsync(idOC);
                if (compra != null)
                {
                    _mapper.Map(request, compra);
                    compra.Valor = valor;
                    compra.Total = total;

                    var idsFamilias = new List<int>();
                    if (request.Detalles != null) idsFamilias.AddRange(request.Detalles.Select(s => s.IdFamilia).Where(id => id.HasValue).Select(id => id!.Value));
                    if (request.DetallesNuevos != null) idsFamilias.AddRange(request.DetallesNuevos.Select(s => s.IdFamilia).Where(id => id.HasValue).Select(id => id!.Value));

                    var nombresFamilias = await _context.Familias
                        .Where(f => idsFamilias.Distinct().Contains(f.Id))
                        .Select(f => f.Abreviatura)
                        .ToListAsync();
                    compra.Familia = string.Join(", ", nombresFamilias);
                }
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                OrdenesYComprasRes? response = await ObtenerOrdenOCompra(idOC);
                return response;
            }
            catch
            {
                await tx.RollbackAsync();
                return null;
            }

        }

        public async Task<DescripcionFacturaRes> DescripcionFactura(int idProveedor)
        {
            var detalles = await _context.DetalleCompras
                .Where(w => w.Compra != null && w.Compra.IdProveedor == idProveedor)
                .OrderBy(w => w.Id)
                .ToListAsync();

            var dict = detalles
                .Where(d => d.IdInsumo > 0 && !string.IsNullOrEmpty(d.DescripcionFac))
                .GroupBy(d => d.IdInsumo)
                .ToDictionary(g => g.Key, g => g.Last().DescripcionFac);

            DescripcionFacturaRes response = new()
            {
                DescripcionFactura = detalles.Select(s => s.DescripcionFac).Distinct().ToArray(),
                DescripcionPorInsumo = dict
            };

            return response;
        }

        public async Task<OrdenesYComprasRes?> ConvertirCompra(int ordenCompraId, ConvertirACompraReq request)
        {
            //Actualizar a compra
            Compra? compra = await _context.Compras.FindAsync(ordenCompraId);
            if (compra == null) return null;
            _mapper.Map(request, compra);

            //Actualizar detalles de la compra
            var idsDetalleCompra = request.Detalles
                                    .Select(s => s.IdDetalleCompra);

            List<DetalleCompra> detalleCompras = await _context.DetalleCompras
                .Where(w => idsDetalleCompra.Contains(w.Id))
                .ToListAsync();

            // Agrupacion por familias
            List<IdFamiliasRes> idFamilias = detalleCompras
            .GroupBy(g => g.IdFamilia).Select(s => new IdFamiliasRes()
            {
                IdFamilia = s.Key,
                Cantidad = s.Count()
            }).ToList();

            var idsFamiliasKeys = idFamilias.Select(f => f.IdFamilia).ToList();

            List<IdFamiliasMaxRes> ultimosId = await _context.DetalleCompras
            .Where(w => idsFamiliasKeys.Contains(w.IdFamilia))
            .GroupBy(g => g.IdFamilia)
            .Select(s => new IdFamiliasMaxRes
            {
                IdFamilia = s.Key,
                Ultimo = s.Max(x => x.Reg) ?? 0
            })
            .ToListAsync();

            decimal sumaTotal = 0m;

            foreach (var item in detalleCompras)
            {
                ConvertirDetalleCompraReq? item2 = request.Detalles.FirstOrDefault(w => w.IdDetalleCompra == item.Id);
                if (item2 != null)
                {
                    _mapper.Map(item2, item);
                    item.IdModificador = request.IdModificador;
                    item.CostoTotal = item.CostoUnitario * item2.Cantidad;
                    IdFamiliasMaxRes? idFamilia = ultimosId.FirstOrDefault(w => w.IdFamilia == item.IdFamilia);
                    if (idFamilia == null) return null;
                    idFamilia.Ultimo++;
                    item.Reg = idFamilia.Ultimo;
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
            //Devolver datos actualizados de la compra
            OrdenesYComprasRes? response = await ObtenerOrdenOCompra(ordenCompraId);
            if (response == null)
                return null;

            return response;
        }

        public async Task<bool> ActualizarRutaFactura(int id, UpdateRutaFacturaReq request)
        {
            Compra? compra = await _context.Compras.FindAsync(id);
            if (compra == null) return false;
            compra.ImgFactura = request.RutaFactura;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarEstadoCompra(int OrdenCompraId, CambiarEstadoReq response)
        {

            Compra? compra = await _context.Compras.FindAsync(OrdenCompraId);
            if (compra == null) return false;
            compra.EstadoCompra = response.Estado;
            compra.IdModificador = response.IdModificador;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<OrdenesYComprasRes>> ListaFacturasPorFamilia(string familia)
        {
            // Normalizar búsqueda: ignorar mayúsculas/minúsculas y espacios extra
            string familiaNorm = familia.Trim().ToUpper();

            // Traer solo compras que ya son facturas (tienen NumeroComprobante)
            // y cuya columna Familia contenga la abreviatura buscada
            List<OrdenesYComprasRes> response = await _context.Compras
                .Where(w => !string.IsNullOrEmpty(w.NumeroComprobante)
                         && w.Familia != null
                         && w.Familia.ToUpper().Contains(familiaNorm))
                .Select(s => new OrdenesYComprasRes
                {
                    Id = s.Id,
                    CUO = "OC" + s.Id,
                    FechaCotizacion = s.FechaCotizacion,
                    NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                    NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                    Valor = s.Valor,
                    Total = s.Total,
                    Moneda = s.Moneda,
                    CodFacQbd = s.CodFacQBD,
                    Familia = s.Familia,
                    Factura = s.SerieComprobante + s.NumeroComprobante,
                    RutaFactura = s.ImgFactura,
                    EstadoPago = s.Modalidad,
                    Usuario = s.Creador != null ? (s.Creador.Codigo ?? s.Creador.Id.ToString()) : "N/A",
                    EstadoCompra = s.EstadoCompra
                })
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            // Enriquecer la columna Familia con los datos reales de DetalleCompras
            if (response.Any())
            {
                var ids = response.Select(r => r.Id).ToList();
                var familiasPorCompra = await _context.DetalleCompras
                    .Where(dc => ids.Contains(dc.IdCompra) && dc.Familia != null)
                    .Select(dc => new { dc.IdCompra, dc.Familia!.Abreviatura })
                    .Distinct()
                    .ToListAsync();

                var dict = familiasPorCompra
                    .GroupBy(x => x.IdCompra)
                    .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Abreviatura)));

                foreach (var item in response)
                {
                    if (dict.TryGetValue(item.Id, out var fams))
                        item.Familia = fams;
                }
            }

            return response;
        }

        public async Task<OrdenMesonRes?> ObtenerCompraMeson(int ordenCompraId)
        {
            OrdenMesonRes? response = await _context.Compras
            .Where(w => w.Id == ordenCompraId)
            .Select(s => new OrdenMesonRes
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
                Lista = s.DetalleCompras != null ? s.DetalleCompras.Select(s => new DetalleOrdenMesonRes
                {
                    Id = s.Id,
                    Reg = s.Reg != null ? Alfanumerico.ConvertToBase36(s.Reg.Value).PadLeft(4, '0') : "",
                    Codigo = "MP-QBD-" + s.IdInsumo,
                    Descripcion = s.Insumo != null ? s.Insumo.Descripcion : "",
                    DescripcionFactura = s.DescripcionFac,
                    Cantidad = s.CantidadSolicitada,
                    Um = s.Um,
                    Coa = s.Coa,
                    Lote = s.Lote,
                    RegistroSanitario = s.RegistroSanitario,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Conformidad = s.Conformidad,
                    IdFabricante = s.IdFabricante,
                    NombreFabricante = s.Fabricante != null ? s.Fabricante.Nombre : "",
                    CodigoFabricante = s.Fabricante != null ? s.Fabricante.Codigo : ""
                }).ToList() : null
            }).FirstOrDefaultAsync();

            if (response == null) return null;

            return response;
        }
    }
}
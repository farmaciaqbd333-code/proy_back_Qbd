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

        public async Task<OrdenCompraGetRes?> ObtenerOrdenCompra(int id)
        {
            OrdenCompraGetRes? response = await _context.Compras
                        .Where(w => w.Id == id)
                        .Select(s => new OrdenCompraGetRes
                        {
                            TC = s.TipoCambio.ToString(),
                            Moneda = s.Moneda,
                            FechaCotizacion = s.FechaCotizacion,
                            Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                            Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                            CodigoProveedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                            RUC = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                            RazonSocial = s.Proveedor != null ? s.Proveedor.Datos : "",
                            DetalleCompraInsumos = s.DetalleCompraInsumos == null ? null : s.DetalleCompraInsumos.Select(s2 => new DetalleInsumosRes
                            {
                                Id = s2.Id,
                                IdInsumo = s2.IdInsumo,
                                Codigo = s2.IdInsumo.ToString(),
                                DescripcionQBD = s2.Insumo == null || s2.Insumo.Descripcion == null ? "" : s2.Insumo.Descripcion,
                                DescripcionFactura = s2.DescripcionFactura,
                                CantidadSolicitada = s2.CantidadSolicitada,
                                UM = s2.Um,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal,
                                Coa = s2.Coa,
                                Lote = s2.Lote,
                                RegistroSanitario = s2.RegistroSanitario,
                                Conforme = s2.Conformidad ?? false,
                                Familia = s2.Insumo == null || s2.Insumo.Familia == null ? "" : s2.Insumo.Familia.Abreviatura,
                                IdFabricante = s2.IdFabricante,
                                NombreFabricante = s2.Fabricante != null ? s2.Fabricante.Nombre : "",
                                CodigoFabricante = s2.Fabricante != null ? s2.Fabricante.Codigo : ""
                            }).ToList(),
                            DetalleEmpaques = s.DetalleCompraEmpaques == null ? null : s.DetalleCompraEmpaques.Select(s2 => new DetalleEmpaquesRes
                            {
                                Id = s2.Id,
                                IdEmpaque = s2.IdEmpaque,
                                Codigo = s2.IdEmpaque.ToString(),
                                DescripcionQBD = s2.Empaque == null || s2.Empaque.Descripcion == null ? "" : s2.Empaque.Descripcion,
                                CantidadSolicitada = s2.CantidadSolicitada,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal
                            }).ToList(),
                            DetalleProductos = s.DetalleCompraProductos == null ? null : s.DetalleCompraProductos.Select(s2 => new DetalleProductosRes
                            {
                                Id = s2.Id,
                                IdProducto = s2.IdProducto,
                                Codigo = s2.IdProducto.ToString(),
                                DescripcionQBD = s2.Producto == null || s2.Producto.Descripcion == null ? "" : s2.Producto.Descripcion,
                                CantidadSolicitada = s2.CantidadSolicitada,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal
                            }).ToList(),
                            DetalleEconomatos = s.DetalleCompraEconomatos == null ? null : s.DetalleCompraEconomatos.Select(s2 => new DetalleEconomatosRes
                            {
                                Id = s2.Id,
                                IdEconomato = s2.IdEconomato,
                                Codigo = s2.IdEconomato.ToString(),
                                DescripcionQBD = s2.Economato == null || s2.Economato.Descripcion == null ? "" : s2.Economato.Descripcion,
                                CantidadSolicitada = s2.CantidadSolicitada,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal
                            }).ToList(),
                            DetalleCompras = s.DetalleCompraOtros == null ? null : s.DetalleCompraOtros.Select(s2 => new DetalleComprasRes
                            {
                                Id = s2.Id,
                                Clasificacion = s2.Clasificacion,
                                Codigo = "",
                                DescripcionQBD = "",
                                CantidadSolicitada = s2.CantidadSolicitada,
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
                            Guia = s.Guia,
                            Modalidad = s.Modalidad,
                            EstadoCompra = s.EstadoCompra

                        }).FirstOrDefaultAsync();

            if (response != null)
            {
                var partesFamilia = new List<string>();

                // Familias reales de insumos (desde su entidad Familia)
                if (response.DetalleCompraInsumos != null && response.DetalleCompraInsumos.Any())
                {
                    var familiasInsumos = response.DetalleCompraInsumos
                        .Select(d => d.Familia)
                        .Where(f => !string.IsNullOrEmpty(f))
                        .Distinct();
                    if (familiasInsumos.Any())
                        partesFamilia.AddRange(familiasInsumos);
                    else
                        partesFamilia.Add("MP");
                }

                if (response.DetalleEmpaques != null && response.DetalleEmpaques.Any())
                    partesFamilia.Add("ME");

                if (response.DetalleProductos != null && response.DetalleProductos.Any())
                    partesFamilia.Add("PT");

                if (response.DetalleEconomatos != null && response.DetalleEconomatos.Any())
                    partesFamilia.Add("ECO");

                if (response.DetalleCompras != null && response.DetalleCompras.Any())
                {
                    var clases = response.DetalleCompras
                        .Select(d => d.Clasificacion)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct();
                    partesFamilia.AddRange(clases);
                }

                response.Familia = string.Join("- ", partesFamilia.Distinct());
            }

            if (response == null)
            {
                return null;
            }
            return response;
        }

        public async Task<List<OrdenesYComprasRes>> ListaOrdenesDeCompras()
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
            if (response.Count == 0) throw new NotFoundException("No hay ordenes de compras");

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


            return response;
        }

        public async Task<int> CrearOrdenDeCompra(OrdenCreateReq request)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                Compra compra = new Compra
                {
                    IdProveedor = request.IdProveedor,
                    Modalidad = request.Modalidad,
                    Moneda = request.Moneda,
                    TipoCambio = request.TipoCambio,
                    Igv = request.Igv,
                    FechaCotizacion = request.FechaCotizacion,
                    Observaciones = request.Observaciones,
                    IdSede = request.IdSede,
                    IdCreador = request.IdCreador,
                    Isc = request.Isc,
                    Icbp = request.Icbp,
                    EstadoCompra = "PENDIENTE",
                    Familia = "",
                    Valor = 0,
                    Total = 0
                };

                decimal valorTotal = 0;
                string Familia = "";
                if (request.DetalleCompraInsumos.Any())
                {
                    valorTotal += request.DetalleCompraInsumos.Sum(s => s.CostoTotal);
                    Familia += Familia == "" ? "MP" : "- MP";
                }
                if (request.DetalleCompraEmpaques.Any())
                {
                    valorTotal += request.DetalleCompraEmpaques.Sum(s => s.CostoTotal);
                    Familia += Familia == "" ? "ME" : "- ME";
                }

                if (request.DetalleCompraProductos.Any())
                {
                    valorTotal += request.DetalleCompraProductos.Sum(s => s.CostoTotal);
                    Familia += Familia == "" ? "PT" : "- PT";
                }
                if (request.DetalleCompraEconomatos.Any())
                {
                    valorTotal += request.DetalleCompraEconomatos.Sum(s => s.CostoTotal);
                    Familia += Familia == "" ? "ECO" : "- ECO";
                }
                if (request.DetalleCompraOtros.Any())
                {
                    valorTotal += request.DetalleCompraOtros.Sum(s => s.CostoTotal);
                    List<string> lista = request.DetalleCompraOtros.Select(s => s.Clasificacion).Distinct().ToList();
                    foreach (var item in lista)
                    {
                        Familia += Familia == "" ? item : "- " + item;
                    }
                }
                compra.Familia = Familia;
                compra.Valor = valorTotal;
                compra.Total = request.Igv ? (compra.Valor * 1.18m) + request.Isc + request.Icbp : compra.Valor + request.Isc + request.Icbp;
                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                if (request.DetalleCompraInsumos.Any())
                {
                    foreach (var item in request.DetalleCompraInsumos)
                    {
                        var detalleCompra = new DetalleCompraInsumo
                        {
                            IdCompra = compra.Id,
                            IdCreador = request.IdCreador,
                            IdInsumo = item.IdInsumo,
                            DescripcionFactura = item.DescripcionFac,
                            CantidadSolicitada = item.Cantidad,
                            Um = item.Um,
                            CostoUnitario = item.CostoUnitario,
                            CostoTotal = item.CostoTotal,
                            IdFabricante = item.IdFabricante,
                        };
                        _context.DetalleComprasInsumos.Add(detalleCompra);
                    }
                }

                // Guardar DetalleCompraEmpaque
                if (request.DetalleCompraEmpaques.Any())
                {
                    foreach (var item in request.DetalleCompraEmpaques)
                    {
                        DetalleCompraEmpaque detalleCompra = _mapper.Map<DetalleCompraEmpaque>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.DetalleCompraEmpaques.Add(detalleCompra);
                    }
                }

                // Guardar DetalleCompraProducto
                if (request.DetalleCompraProductos.Any())
                {
                    foreach (var item in request.DetalleCompraProductos)
                    {
                        DetalleCompraProducto detalleCompra = _mapper.Map<DetalleCompraProducto>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.DetalleCompraProductos.Add(detalleCompra);
                    }
                }


                // Guardar DetalleCompraEconomato
                if (request.DetalleCompraEconomatos.Any())
                {
                    foreach (var item in request.DetalleCompraEconomatos)
                    {
                        DetalleCompraEconomato detalleCompra = _mapper.Map<DetalleCompraEconomato>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.DetalleCompraEconomatos.Add(detalleCompra);
                    }
                }

                // Guardar DetalleCompra (Generico)
                if (request.DetalleCompraOtros.Any())
                {
                    foreach (var item in request.DetalleCompraOtros)
                    {
                        DetalleCompraOtros detalleCompra = _mapper.Map<DetalleCompraOtros>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.DetalleCompraOtros.Add(detalleCompra);
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return compra.Id;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
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

                // 1. Eliminar
                if (request.DetallesEliminados.Any())
                {
                    var items = _context.DetalleCompraOtros.Where(d => request.DetallesEliminados.Contains(d.Id));
                    _context.DetalleCompraOtros.RemoveRange(items);
                }
                if (request.DetallesCompraInsumosEliminados.Any())
                {
                    var items = _context.DetalleComprasInsumos.Where(d => request.DetallesCompraInsumosEliminados.Contains(d.Id));
                    _context.DetalleComprasInsumos.RemoveRange(items);
                }
                if (request.DetalleCompraEmpaquesEliminados.Any())
                {
                    var items = _context.DetalleCompraEmpaques.Where(d => request.DetalleCompraEmpaquesEliminados.Contains(d.Id));
                    _context.DetalleCompraEmpaques.RemoveRange(items);
                }

                if (request.DetalleCompraProductosEliminados.Any())
                {
                    var items = _context.DetalleCompraProductos.Where(d => request.DetalleCompraProductosEliminados.Contains(d.Id));
                    _context.DetalleCompraProductos.RemoveRange(items);
                }
                if (request.DetalleCompraEconomatosEliminados.Any())
                {
                    var items = _context.DetalleCompraEconomatos.Where(d => request.DetalleCompraEconomatosEliminados.Contains(d.Id));
                    _context.DetalleCompraEconomatos.RemoveRange(items);
                }

                // 2. Aplicar Insumos (Nuevos + Editados)
                if (request.DetalleCompraInsumosNuevos.Any())
                {
                    foreach (var item in request.DetalleCompraInsumosNuevos)
                    {
                        var detalleCompraInsumo = new DetalleCompraInsumo
                        {
                            IdCompra = idOC,
                            IdCreador = request.IdModificadorCreador,
                            IdInsumo = item.IdInsumo,
                            DescripcionFactura = item.DescripcionFac,
                            CantidadSolicitada = item.Cantidad,
                            Um = item.Um,
                            CostoUnitario = item.CostoUnitario,
                            CostoTotal = item.CostoTotal,
                            IdFabricante = item.IdFabricante,
                        };
                        _context.DetalleComprasInsumos.Add(detalleCompraInsumo);
                    }
                }
                if (request.DetalleCompraInsumosUpd.Any())
                {
                    foreach (var item in request.DetalleCompraInsumosUpd)
                    {
                        DetalleCompraInsumo? detalle = await _context.DetalleComprasInsumos.FindAsync(item.Id);
                        if (detalle != null)
                        {
                            if (detalle.IdInsumo != item.IdInsumo)
                                detalle.IdInsumo = item.IdInsumo;

                            if (detalle.DescripcionFactura != item.DescripcionFac)
                                detalle.DescripcionFactura = item.DescripcionFac;

                            if (detalle.CantidadSolicitada != item.Cantidad)
                                detalle.CantidadSolicitada = item.Cantidad;

                            if (detalle.Um != item.Um)
                                detalle.Um = item.Um;

                            if (detalle.CostoUnitario != item.CostoUnitario)
                                detalle.CostoUnitario = item.CostoUnitario;

                            if (detalle.CostoTotal != item.CostoTotal)
                                detalle.CostoTotal = item.CostoTotal;

                            if (detalle.IdFabricante != item.IdFabricante)
                                detalle.IdFabricante = item.IdFabricante;

                            // normalmente este sí se asigna siempre (auditoría)
                            detalle.IdModificador = request.IdModificadorCreador;
                        }
                    }
                }

                if (request.DetalleCompraEmpaquesNuevos.Any())
                {
                    foreach (var item in request.DetalleCompraEmpaquesNuevos)
                    {
                        DetalleCompraEmpaque detalle = _mapper.Map<DetalleCompraEmpaque>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.DetalleCompraEmpaques.Add(detalle);
                    }
                }
                if (request.DetalleCompraEmpaquesUpd.Any())
                {
                    foreach (var item in request.DetalleCompraEmpaquesUpd)
                    {
                        DetalleCompraEmpaque? detalle = await _context.DetalleCompraEmpaques.FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                        }
                    }
                }

                // 5. Aplicar Productos (Nuevos + Editados)
                if (request.DetalleCompraProductosNuevos.Any())
                {
                    foreach (var item in request.DetalleCompraProductosNuevos)
                    {
                        DetalleCompraProducto detalle = _mapper.Map<DetalleCompraProducto>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.DetalleCompraProductos.Add(detalle);
                    }
                }
                if (request.DetalleCompraProductosUpd.Any())
                {
                    foreach (var item in request.DetalleCompraProductosUpd)
                    {
                        DetalleCompraProducto? detalle = await _context.DetalleCompraProductos.FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                        }
                    }
                }

                // 6. Aplicar Economatos (Nuevos + Editados)
                if (request.DetalleCompraEconomatosNuevos.Any())
                {
                    foreach (var item in request.DetalleCompraEconomatosNuevos)
                    {
                        DetalleCompraEconomato detalle = _mapper.Map<DetalleCompraEconomato>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.DetalleCompraEconomatos.Add(detalle);
                    }
                }
                if (request.DetalleCompraEconomatosUpd.Any())
                {
                    foreach (var item in request.DetalleCompraEconomatosUpd)
                    {
                        DetalleCompraEconomato? detalle = await _context.DetalleCompraEconomatos.FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                        }
                    }
                }

                // 7. Aplicar Generico (Nuevos + Editados)
                if (request.DetalleComprasNuevos.Any())
                {
                    foreach (var item in request.DetalleComprasNuevos)
                    {
                        DetalleCompraOtros detalle = _mapper.Map<DetalleCompraOtros>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.DetalleCompraOtros.Add(detalle);
                    }
                }
                if (request.DetalleComprasUpd.Any())
                {
                    foreach (var item in request.DetalleComprasUpd)
                    {
                        DetalleCompraOtros? detalle = await _context.DetalleCompraOtros.FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                        }
                    }
                }

                // Guardar cambios para persistir en la DB antes de calcular la suma y las familias
                await _context.SaveChangesAsync();

                // Calcular el nuevo valor de la compra sumando de todas las tablas asociadas
                decimal totalInsumos = await _context.DetalleComprasInsumos.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalEmpaques = await _context.DetalleCompraEmpaques.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalProductos = await _context.DetalleCompraProductos.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalEconomatos = await _context.DetalleCompraEconomatos.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalCompras = await _context.DetalleCompraOtros.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);

                valor = totalInsumos + totalEmpaques + totalProductos + totalEconomatos + totalCompras;
                total = (request.Igv == true ? valor * 1.18m : valor) + request.Isc + request.Icbp;

                Compra? compra = await _context.Compras.FindAsync(idOC);
                if (compra != null)
                {
                    _mapper.Map(request, compra);
                    compra.Valor = valor;
                    compra.Total = total;

                    // Calcular familias recalculando directamente del estado actual de la DB
                    var partesFamilia = new List<string>();

                    var idsFamilias = await _context.DetalleComprasInsumos
                        .Where(d => d.IdCompra == idOC && d.Insumo != null && d.Insumo.Familia != null)
                        .Select(d => d.Insumo!.IdFamilia)
                        .Distinct()
                        .ToListAsync();

                    var nombresFamilias = await _context.Familias
                        .Where(f => idsFamilias.Contains(f.Id))
                        .Select(f => f.Abreviatura)
                        .ToListAsync();

                    if (nombresFamilias.Any())
                        partesFamilia.AddRange(nombresFamilias);
                    else if (await _context.DetalleComprasInsumos.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("MP");

                    if (await _context.DetalleCompraEmpaques.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("ME");

                    if (await _context.DetalleCompraProductos.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("PT");

                    if (await _context.DetalleCompraEconomatos.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("ECO");

                    var clases = await _context.DetalleCompraOtros
                        .Where(d => d.IdCompra == idOC && !string.IsNullOrEmpty(d.Clasificacion))
                        .Select(d => d.Clasificacion)
                        .Distinct()
                        .ToListAsync();
                    if (clases.Any())
                        partesFamilia.AddRange(clases);

                    compra.Familia = string.Join("- ", partesFamilia.Distinct());
                }
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                OrdenesYComprasRes? response = await ObtenerOrdenOCompra(idOC);
                return response;
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Console.WriteLine($"Error en ActualizarOrdenDeCompra: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<DescripcionFacturaRes> DescripcionFactura(int idProveedor)
        {
            var detalles = await _context.DetalleComprasInsumos
                .Where(w => w.Compra != null && w.Compra.IdProveedor == idProveedor)
                .OrderBy(w => w.Id)
                .ToListAsync();

            var dict = detalles
                .Where(d => d.IdInsumo > 0 && !string.IsNullOrEmpty(d.DescripcionFactura))
                .GroupBy(d => d.IdInsumo)
                .ToDictionary(g => g.Key, g => g.Last().DescripcionFactura);

            DescripcionFacturaRes response = new()
            {
                DescripcionFactura = detalles.Select(s => s.DescripcionFactura).Distinct().ToArray(),
                DescripcionPorInsumo = dict
            };

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
            return response;
        }

       
    }
}
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
                            DetalleCompraInsumos = s.CompraInsumos == null ? null : s.CompraInsumos.Select(s2 => new DetalleInsumosRes
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
                                CodigoFabricante = s2.Fabricante != null ? s2.Fabricante.Codigo : "",
                                Pdf = s2.Pdf,
                                FechaFabricacion = s2.FechaFabricacion,
                                FechaVencimiento = s2.FechaVencimiento
                            }).ToList(),
                            DetalleEmpaques = s.CompraEmpaques == null ? null : s.CompraEmpaques.Select(s2 => new DetalleEmpaquesRes
                            {
                                Id = s2.Id,
                                IdEmpaque = s2.IdEmpaque,
                                Codigo = s2.IdEmpaque.ToString(),
                                DescripcionQBD = s2.Empaque == null || s2.Empaque.Descripcion == null ? "" : s2.Empaque.Descripcion,
                                DescripcionFactura = s2.DescripcionFactura ?? "",
                                CantidadSolicitada = s2.CantidadSolicitada,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal,
                                UM = s2.Um,
                                Pdf = s2.Pdf,
                                Coa = s2.Coa ?? false,
                                Lote = s2.Lote,
                                FechaFabricacion = s2.FechaFabricacion,
                                FechaVencimiento = s2.FechaVencimiento,
                                Conforme = s2.Conformidad ?? false,
                                IdFabricante = s2.IdFabricante
                            }).ToList(),
                            DetalleProductos = s.CompraProductos == null ? null : s.CompraProductos.Select(s2 => new DetalleProductosRes
                            {
                                Id = s2.Id,
                                IdProducto = s2.IdProducto,
                                Codigo = s2.IdProducto.ToString(),
                                DescripcionQBD = s2.Producto == null || s2.Producto.Descripcion == null ? "" : s2.Producto.Descripcion,
                                DescripcionFactura = s2.DescripcionFactura ?? "",
                                CantidadSolicitada = s2.CantidadSolicitada,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal,
                                UM = s2.Um,
                                Lote = s2.Lote,
                                RegistroSanitario = s2.RegistroSanitario,
                                FechaFabricacion = s2.FechaFabricacion,
                                FechaVencimiento = s2.FechaVencimiento,
                                Conforme = s2.Conformidad ?? false,
                                IdFabricante = s2.IdFabricante
                            }).ToList(),
                            DetalleEconomatos = s.CompraEconomatos == null ? null : s.CompraEconomatos.Select(s2 => new DetalleEconomatosRes
                            {
                                Id = s2.Id,
                                IdEconomato = s2.IdEconomato,
                                Codigo = s2.IdEconomato.ToString(),
                                DescripcionQBD = s2.Economato == null || s2.Economato.Descripcion == null ? "" : s2.Economato.Descripcion,
                                DescripcionFactura = s2.DescripcionFactura ?? "",
                                CantidadSolicitada = s2.CantidadSolicitada,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal,
                                UM = s2.Um,
                                Pdf = s2.Pdf,
                                Conforme = s2.Conformidad ?? false,
                                IdFabricante = s2.IdFabricante
                            }).ToList(),
                            DetalleOtros = s.CompraOtros == null ? null : s.CompraOtros.Select(s2 => new DetalleComprasOtrosRes
                            {
                                Id = s2.Id,
                                IdFamilia = s2.IdFamilia,
                                Codigo = "",
                                DescripcionFactura = s2.DescripcionFactura ?? "",
                                CantidadSolicitada = s2.CantidadSolicitada,
                                CUnitario = s2.CostoUnitario,
                                CTotal = s2.CostoTotal,
                                UM = s2.UnidadMedida,
                                Pdf = s2.Pdf,
                                Conforme = s2.Conformidad ?? false
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
                            EstadoCompra = s.EstadoCompra,
                            SerieComprobante = s.SerieComprobante,
                            NumeroComprobante = s.NumeroComprobante,
                            Factura = s.SerieComprobante + "-" + s.NumeroComprobante,
                            CodFacQBD = s.CodFacQBD,
                            FechaFactura = s.FechaFactura
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

                if (response.DetalleOtros != null && response.DetalleOtros.Any())
                {
                    var clases1 = response.DetalleOtros
                        .Select(d => d.IdFamilia)
                        .Where(c => c != 0)
                        .Distinct();
                    List<string> clases2 = new();
                    if (clases1.Contains(5)) clases2.Add("FXP");
                    if (clases1.Contains(6)) clases2.Add("FXS");
                    if (clases1.Contains(7)) clases2.Add("RH");
                    partesFamilia.AddRange(clases2);
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
                    List<int> clases1 = request.DetalleCompraOtros.Select(s => s.IdFamilia).Distinct().ToList();
                    var nombresFamiliasOtros = await _context.Familias
                        .Where(f => clases1.Contains(f.Id))
                        .Select(f => f.Abreviatura)
                        .ToListAsync();
                    foreach (var item in nombresFamiliasOtros)
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
                        var detalleCompra = new CompraInsumos
                        {
                            IdCompra = compra.Id,
                            IdCreador = request.IdCreador,
                            IdInsumo = item.IdInsumo,
                            DescripcionFactura = item.DescripcionFac,
                            CantidadSolicitada = item.Cantidad,
                            Um = item.Um,
                            CostoUnitario = item.CostoUnitario,
                            CostoTotal = item.CostoTotal,
                            IdFabricante = item.IdFabricante == 0 ? null : item.IdFabricante,
                        };
                        _context.CompraInsumos.Add(detalleCompra);
                    }
                }

                // Guardar DetalleCompraEmpaque
                if (request.DetalleCompraEmpaques.Any())
                {
                    foreach (var item in request.DetalleCompraEmpaques)
                    {
                        CompraEmpaques detalleCompra = _mapper.Map<CompraEmpaques>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.CompraEmpaques.Add(detalleCompra);
                    }
                }

                // Guardar DetalleCompraProducto
                if (request.DetalleCompraProductos.Any())
                {
                    foreach (var item in request.DetalleCompraProductos)
                    {
                        CompraProductos detalleCompra = _mapper.Map<CompraProductos>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.CompraProductos.Add(detalleCompra);
                    }
                }


                // Guardar DetalleCompraEconomato
                if (request.DetalleCompraEconomatos.Any())
                {
                    foreach (var item in request.DetalleCompraEconomatos)
                    {
                        CompraEconomatos detalleCompra = _mapper.Map<CompraEconomatos>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.CompraEconomatos.Add(detalleCompra);
                    }
                }

                // Guardar DetalleCompra (Generico)
                if (request.DetalleCompraOtros.Any())
                {
                    foreach (var item in request.DetalleCompraOtros)
                    {
                        CompraOtros detalleCompra = _mapper.Map<CompraOtros>(item);
                        detalleCompra.IdCompra = compra.Id;
                        detalleCompra.IdCreador = request.IdCreador;
                        _context.CompraOtros.Add(detalleCompra);
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
                    var items = _context.CompraOtros.Where(d => request.DetallesEliminados.Contains(d.Id));
                    _context.CompraOtros.RemoveRange(items);
                }
                if (request.DetallesCompraInsumosEliminados.Any())
                {
                    var items = _context.CompraInsumos.Where(d => request.DetallesCompraInsumosEliminados.Contains(d.Id));
                    _context.CompraInsumos.RemoveRange(items);
                }
                if (request.DetalleCompraEmpaquesEliminados.Any())
                {
                    var items = _context.CompraEmpaques.Where(d => request.DetalleCompraEmpaquesEliminados.Contains(d.Id));
                    _context.CompraEmpaques.RemoveRange(items);
                }

                if (request.DetalleCompraProductosEliminados.Any())
                {
                    var items = _context.CompraProductos.Where(d => request.DetalleCompraProductosEliminados.Contains(d.Id));
                    _context.CompraProductos.RemoveRange(items);
                }
                if (request.DetalleCompraEconomatosEliminados.Any())
                {
                    var items = _context.CompraEconomatos.Where(d => request.DetalleCompraEconomatosEliminados.Contains(d.Id));
                    _context.CompraEconomatos.RemoveRange(items);
                }

                // 2. Aplicar Insumos (Nuevos + Editados)
                if (request.DetalleCompraInsumosNuevos.Any())
                {
                    foreach (var item in request.DetalleCompraInsumosNuevos)
                    {
                        var detalleCompraInsumo = new CompraInsumos
                        {
                            IdCompra = idOC,
                            IdCreador = request.IdModificadorCreador,
                            IdInsumo = item.IdInsumo,
                            DescripcionFactura = item.DescripcionFac,
                            CantidadSolicitada = item.Cantidad,
                            Um = item.Um,
                            CostoUnitario = item.CostoUnitario,
                            CostoTotal = item.CostoTotal,
                            IdFabricante = item.IdFabricante == 0 ? null : item.IdFabricante,
                        };
                        _context.CompraInsumos.Add(detalleCompraInsumo);
                    }
                }
                if (request.DetalleCompraInsumosUpd.Any())
                {
                    foreach (var item in request.DetalleCompraInsumosUpd)
                    {
                        CompraInsumos? detalle = await _context.CompraInsumos.FindAsync(item.Id);
                        if (detalle != null)
                        {
                            if (detalle.IdInsumo != item.IdInsumo)
                                detalle.IdInsumo = item.IdInsumo;

                            if (detalle.DescripcionFactura != item.DescripcionFactura)
                                detalle.DescripcionFactura = item.DescripcionFactura;

                            if (detalle.CantidadSolicitada != item.CantidadSolicitada)
                                detalle.CantidadSolicitada = item.CantidadSolicitada;

                            if (detalle.Um != item.Um)
                                detalle.Um = item.Um;

                            if (detalle.CostoUnitario != item.CostoUnitario)
                                detalle.CostoUnitario = item.CostoUnitario;

                            if (detalle.CostoTotal != item.CostoTotal)
                                detalle.CostoTotal = item.CostoTotal;

                            var incomingFabricanteId = item.IdFabricante == 0 ? null : item.IdFabricante;
                            if (detalle.IdFabricante != incomingFabricanteId)
                                detalle.IdFabricante = incomingFabricanteId;

                            // normalmente este sí se asigna siempre (auditoría)
                            detalle.IdModificador = request.IdModificadorCreador;
                        }
                    }
                }

                if (request.DetalleCompraEmpaquesNuevos.Any())
                {
                    foreach (var item in request.DetalleCompraEmpaquesNuevos)
                    {
                        CompraEmpaques detalle = _mapper.Map<CompraEmpaques>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.CompraEmpaques.Add(detalle);
                    }
                }
                if (request.DetalleCompraEmpaquesUpd.Any())
                {
                    foreach (var item in request.DetalleCompraEmpaquesUpd)
                    {
                        CompraEmpaques? detalle = await _context.CompraEmpaques.FindAsync(item.Id);
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
                        CompraProductos detalle = _mapper.Map<CompraProductos>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.CompraProductos.Add(detalle);
                    }
                }
                if (request.DetalleCompraProductosUpd.Any())
                {
                    foreach (var item in request.DetalleCompraProductosUpd)
                    {
                        CompraProductos? detalle = await _context.CompraProductos.FindAsync(item.Id);
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
                        CompraEconomatos detalle = _mapper.Map<CompraEconomatos>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.CompraEconomatos.Add(detalle);
                    }
                }
                if (request.DetalleCompraEconomatosUpd.Any())
                {
                    foreach (var item in request.DetalleCompraEconomatosUpd)
                    {
                        CompraEconomatos? detalle = await _context.CompraEconomatos.FindAsync(item.Id);
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
                        CompraOtros detalle = _mapper.Map<CompraOtros>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        _context.CompraOtros.Add(detalle);
                    }
                }
                if (request.DetalleComprasUpd.Any())
                {
                    foreach (var item in request.DetalleComprasUpd)
                    {
                        CompraOtros? detalle = await _context.CompraOtros.FindAsync(item.Id);
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
                decimal totalInsumos = await _context.CompraInsumos.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalEmpaques = await _context.CompraEmpaques.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalProductos = await _context.CompraProductos.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalEconomatos = await _context.CompraEconomatos.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalCompras = await _context.CompraOtros.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);

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

                    var idsFamilias = await _context.CompraInsumos
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
                    else if (await _context.CompraInsumos.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("MP");

                    if (await _context.CompraEmpaques.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("ME");

                    if (await _context.CompraProductos.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("PT");

                    if (await _context.CompraEconomatos.AnyAsync(d => d.IdCompra == idOC))
                        partesFamilia.Add("ECO");

                    var clases1 = await _context.CompraOtros
                        .Where(d => d.IdCompra == idOC && d.IdFamilia != 0)
                        .Select(d => d.IdFamilia)
                        .Distinct()
                        .ToListAsync();
                    var nombresFamiliasOtros = await _context.Familias
                        .Where(f => clases1.Contains(f.Id))
                        .Select(f => f.Abreviatura)
                        .ToListAsync();
                    if (nombresFamiliasOtros.Any()) partesFamilia.AddRange(nombresFamiliasOtros);

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
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                Console.WriteLine($"Error en ActualizarOrdenDeCompra: {ex.Message} - {innerMsg}\n{ex.StackTrace}");
                throw new Exception($"Error al guardar en DB: {ex.Message}. Detalle interno: {innerMsg}");
            }
        }

        public async Task<DescripcionFacturaRes> DescripcionFactura(int idProveedor)
        {
            var detalles = await _context.CompraInsumos
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

        public async Task<bool> ActualizarEstadoPago(int OrdenCompraId, CambiarEstadoReq response)
        {
            Compra? compra = await _context.Compras.FindAsync(OrdenCompraId);
            if (compra == null) return false;
            compra.Modalidad = response.Estado;
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

        public async Task<bool> ActualizarDetallePdf(string familia, int id, string? pdf)
        {
            string famNorm = familia.Trim().ToUpper();
            if (famNorm.Contains("MP") || famNorm.Contains("INSUMO"))
            {
                CompraInsumos? detail = await _context.CompraInsumos.FindAsync(id);
                if (detail == null) return false;
                detail.Pdf = pdf;
            }
            else if (famNorm.Contains("ME") || famNorm.Contains("EMPAQUE"))
            {
                CompraEmpaques? detail = await _context.CompraEmpaques.FindAsync(id);
                if (detail == null) return false;
                detail.Pdf = pdf;
            }
            else if (famNorm.Contains("ECO") || famNorm.Contains("ECONOMATO"))
            {
                CompraEconomatos? detail = await _context.CompraEconomatos.FindAsync(id);
                if (detail == null) return false;
                detail.Pdf = pdf;
            }
            else if (famNorm.Contains("OTROS") || famNorm.Contains("OTRO"))
            {
                CompraOtros? detail = await _context.CompraOtros.FindAsync(id);
                if (detail == null) return false;
                detail.Pdf = pdf;
            }
            else
            {
                return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

    }
}
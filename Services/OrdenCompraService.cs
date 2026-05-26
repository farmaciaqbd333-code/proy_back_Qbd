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
                            Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                            Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                            DetalleCompraInsumos = s.DetalleCompraInsumos == null ? null : s.DetalleCompraInsumos.Select(s2 => new DetalleInsumosRes
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
                                Familia = s2.Insumo.Familia != null ? s2.Insumo.Familia.Abreviatura : "",
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
                            DetalleCompras = s.DetalleCompras == null ? null : s.DetalleCompras.Select(s2 => new DetalleComprasRes
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

            if (response != null && response.DetalleCompraInsumos != null)
            {
                response.Familia = string.Join(", ", response.DetalleCompraInsumos
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


            var ids = response.Select(r => r.Id).ToList();
            var familiasPorCompra = await _context.DetalleComprasInsumos
                .Where(dc => ids.Contains(dc.IdCompra) && dc.Insumo.Familia != null)
                .Select(dc => new { dc.IdCompra, dc.Insumo.Familia!.Abreviatura })
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
            Compra compra = _mapper.Map<Compra>(request);
            decimal valorTotal = 0;
            string Familia = "";
            if (request.DetalleCompraInsumos != null)
            {
                valorTotal += request.DetalleCompraInsumos.Sum(s => s.CostoTotal);
                Familia += Familia == "" ? "MP" : "- MP";
            }
            if (request.DetalleCompraEmpaques != null)
            {
                valorTotal += request.DetalleCompraEmpaques.Sum(s => s.CostoTotal);
                Familia += Familia == "" ? "ME" : "- ME";
            }

            if (request.DetalleCompraProductos != null)
            {
                valorTotal += request.DetalleCompraProductos.Sum(s => s.CostoTotal);
                Familia += Familia == "" ? "PT" : "- PT";
            }
            if (request.DetalleCompraEconomatos != null)
            {
                valorTotal += request.DetalleCompraEconomatos.Sum(s => s.CostoTotal);
                Familia += Familia == "" ? "ECO" : "- ECO";
            }
            if (request.DetalleCompras != null)
            {
                valorTotal += request.DetalleCompras.Sum(s => s.CostoTotal);
                List<string> lista = request.DetalleCompras.Select(s => s.Clasificacion).Distinct().ToList();
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

            if (request.DetalleCompraInsumos != null)
            {
                foreach (var item in request.DetalleCompraInsumos)
                {
                    DetalleCompraInsumo detalleCompra = _mapper.Map<DetalleCompraInsumo>(item);
                    detalleCompra.IdCompra = compra.Id;
                    detalleCompra.IdCreador = 1;
                    _context.DetalleComprasInsumos.Add(detalleCompra);
                }
            }

            // Guardar DetalleCompraEmpaque
            if (request.DetalleCompraEmpaques != null)
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
            if (request.DetalleCompraProductos != null)
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
            if (request.DetalleCompraEconomatos != null)
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
            if (request.DetalleCompras != null)
            {
                foreach (var item in request.DetalleCompras)
                {
                    DetalleCompra detalleCompra = _mapper.Map<DetalleCompra>(item);
                    detalleCompra.IdCompra = compra.Id;
                    detalleCompra.IdCreador = request.IdCreador;
                    _context.DetalleCompras.Add(detalleCompra);
                }
            }

            await _context.SaveChangesAsync();

            return compra.Id;

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
                if (request.DetallesEliminados != null)
                    await _serviceDOC.EliminarDetalleOrdenDeCompra(request.DetallesEliminados);

                // 2. Aplicar Insumos (Nuevos + Editados)
                if (request.DetalleCompraInsumosNuevos != null)
                {
                    foreach (var item in request.DetalleCompraInsumosNuevos)
                    {
                        DetalleCompraInsumo detalle = _mapper.Map<DetalleCompraInsumo>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        detalle.FechaCreacion = DateTime.Now;
                        _context.DetalleComprasInsumos.Add(detalle);
                    }
                }
                if (request.DetalleCompraInsumos != null)
                {
                    foreach (var item in request.DetalleCompraInsumos)
                    {
                        DetalleCompraInsumo? detalle = await _context.DetalleComprasInsumos.FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                            detalle.FechaModificacion = DateTime.Now;
                        }
                    }
                }

                // 4. Aplicar Empaques (Nuevos + Editados)
                if (request.DetalleCompraEmpaquesNuevos != null)
                {
                    foreach (var item in request.DetalleCompraEmpaquesNuevos)
                    {
                        DetalleCompraEmpaque detalle = _mapper.Map<DetalleCompraEmpaque>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        detalle.FechaCreacion = DateTime.Now;
                        _context.DetalleCompraEmpaques.Add(detalle);
                    }
                }
                if (request.DetalleCompraEmpaques != null)
                {
                    foreach (var item in request.DetalleCompraEmpaques)
                    {
                        DetalleCompraEmpaque? detalle = await _context.Set<DetalleCompraEmpaque>().FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                            detalle.FechaModificacion = DateTime.Now;
                        }
                    }
                }

                // 5. Aplicar Productos (Nuevos + Editados)
                if (request.DetalleCompraProductosNuevos != null)
                {
                    foreach (var item in request.DetalleCompraProductosNuevos)
                    {
                        DetalleCompraProducto detalle = _mapper.Map<DetalleCompraProducto>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        detalle.FechaCreacion = DateTime.Now;
                        _context.Add(detalle);
                    }
                }
                if (request.DetalleCompraProductos != null)
                {
                    foreach (var item in request.DetalleCompraProductos)
                    {
                        DetalleCompraProducto? detalle = await _context.Set<DetalleCompraProducto>().FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                            detalle.FechaModificacion = DateTime.Now;
                        }
                    }
                }

                // 6. Aplicar Economatos (Nuevos + Editados)
                if (request.DetalleCompraEconomatosNuevos != null)
                {
                    foreach (var item in request.DetalleCompraEconomatosNuevos)
                    {
                        DetalleCompraEconomato detalle = _mapper.Map<DetalleCompraEconomato>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        detalle.FechaCreacion = DateTime.Now;
                        _context.Add(detalle);
                    }
                }
                if (request.DetalleCompraEconomatos != null)
                {
                    foreach (var item in request.DetalleCompraEconomatos)
                    {
                        DetalleCompraEconomato? detalle = await _context.Set<DetalleCompraEconomato>().FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                            detalle.FechaModificacion = DateTime.Now;
                        }
                    }
                }

                // 7. Aplicar Generico (Nuevos + Editados)
                if (request.DetalleComprasNuevos != null)
                {
                    foreach (var item in request.DetalleComprasNuevos)
                    {
                        DetalleCompra detalle = _mapper.Map<DetalleCompra>(item);
                        detalle.IdCompra = idOC;
                        detalle.IdCreador = request.IdModificadorCreador;
                        detalle.FechaCreacion = DateTime.Now;
                        _context.Add(detalle);
                    }
                }
                if (request.DetalleCompras != null)
                {
                    foreach (var item in request.DetalleCompras)
                    {
                        DetalleCompra? detalle = await _context.Set<DetalleCompra>().FindAsync(item.Id);
                        if (detalle != null)
                        {
                            _mapper.Map(item, detalle);
                            detalle.IdModificador = request.IdModificadorCreador;
                            detalle.FechaModificacion = DateTime.Now;
                        }
                    }
                }

                // Guardar cambios para persistir en la DB antes de calcular la suma y las familias
                await _context.SaveChangesAsync();

                // Calcular el nuevo valor de la compra sumando de todas las tablas asociadas
                decimal totalInsumos = await _context.DetalleCompras.Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalEmpaques = await _context.Set<DetalleCompraEmpaque>().Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalProductos = await _context.Set<DetalleCompraProducto>().Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalEconomatos = await _context.Set<DetalleCompraEconomato>().Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);
                decimal totalCompras = await _context.Set<DetalleCompra>().Where(d => d.IdCompra == idOC).SumAsync(d => d.CostoTotal);

                valor = totalInsumos + totalEmpaques + totalProductos + totalEconomatos + totalCompras;
                total = (request.Igv == true ? valor * 1.18m : valor) + request.Isc + request.Icbp;

                Compra? compra = await _context.Compras.FindAsync(idOC);
                if (compra != null)
                {
                    _mapper.Map(request, compra);
                    compra.Valor = valor;
                    compra.Total = total;

                    // Calcular familias recalculando directamente del estado actual de la DB
                    var idsFamilias = await _context.DetalleComprasInsumos
                        .Where(d => d.IdCompra == idOC)
                        .Select(d => d.Insumo.IdFamilia)
                        .Distinct()
                        .ToListAsync();

                    var nombresFamilias = await _context.Familias
                        .Where(f => idsFamilias.Contains(f.Id))
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
            var detalles = await _context.DetalleComprasInsumos
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

            List<DetalleCompraInsumo> detalleCompras = await _context.DetalleComprasInsumos
                .Where(w => idsDetalleCompra.Contains(w.Id))
                .ToListAsync();

            // Agrupacion por familias
            List<IdFamiliasRes> idFamilias = detalleCompras
            .GroupBy(g => g.Insumo.IdFamilia).Select(s => new IdFamiliasRes()
            {
                IdFamilia = s.Key,
                Cantidad = s.Count()
            }).ToList();

            var idsFamiliasKeys = idFamilias.Select(f => f.IdFamilia).ToList();

            List<IdFamiliasMaxRes> ultimosId = await _context.DetalleComprasInsumos
            .Where(w => idsFamiliasKeys.Contains(w.Insumo.IdFamilia))
            .GroupBy(g => g.Insumo.IdFamilia)
            .Select(s => new IdFamiliasMaxRes
            {
                IdFamilia = s.Key,
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
                    IdFamiliasMaxRes? idFamilia = ultimosId.FirstOrDefault(w => w.IdFamilia == item.Insumo.IdFamilia);
                    if (idFamilia == null) return null;
                    idFamilia.Ultimo++;
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
                var familiasPorCompra = await _context.DetalleComprasInsumos
                    .Where(dc => ids.Contains(dc.IdCompra) && dc.Insumo.Familia != null)
                    .Select(dc => new { dc.IdCompra, dc.Insumo.Familia!.Abreviatura })
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
                // Lista = s.DetalleCompras != null ? s.DetalleCompras.Select(s => new DetalleOrdenMesonRes
                // {
                //     Id = s.Id,
                //     Reg = s.Reg != null ? Alfanumerico.ConvertToBase36(s.Reg.Value).PadLeft(4, '0') : "",
                //     Codigo = "MP-QBD-" + s.IdInsumo,
                //     Descripcion = s.Insumo != null ? s.Insumo.Descripcion : "",
                //     DescripcionFactura = s.DescripcionFac,
                //     Cantidad = s.CantidadSolicitada,
                //     Um = s.Um,
                //     Coa = s.Coa,
                //     Lote = s.Lote,
                //     RegistroSanitario = s.RegistroSanitario,
                //     FechaFabricacion = s.FechaFabricacion,
                //     FechaVencimiento = s.FechaVencimiento,
                //     Conformidad = s.Conformidad,
                //     IdFabricante = s.IdFabricante,
                //     NombreFabricante = s.Fabricante != null ? s.Fabricante.Nombre : "",
                //     CodigoFabricante = s.Fabricante != null ? s.Fabricante.Codigo : ""
                // }).ToList() : null
            }).FirstOrDefaultAsync();

            if (response == null) return null;

            return response;
        }
    }
}
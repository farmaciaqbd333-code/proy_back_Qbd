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
            if (request == null) return;
            int x = 0;
            Compra? compra = await _context.Compras.FindAsync(idCompra);

            if (request.Insumos != null && request.Insumos.Any())
            {
                List<ActualizarInsumoReq> insumos = request.Insumos;

                IEnumerable<int> idInsumos = insumos.Select(s => s.IdCompraInsumo).ToList();

                List<CompraInsumos> compraInsumos = await _context.CompraInsumos
                    .Include(w => w.PaqueteInsumos!)
                    .ThenInclude(p => p.Paquete)
                    .Where(w => w.IdCompra == idCompra && idInsumos.Contains(w.Id)).ToListAsync();

                foreach (var item in compraInsumos)
                {
                    ActualizarInsumoReq? req = insumos.FirstOrDefault(f => f.IdCompraInsumo == item.Id);
                    if (req != null)
                    {
                        new DetalleCompraLabMapper().ActualizarInsumo(req, item);
                        item.Conformidad = UtilConformidad.CalcularConformidad(req.FechaVencimiento);

                        // Si tiene paquetes registrados, la cantidad real recibida es la suma de los paquetes
                        decimal cantidadRecibida = req.CantidadFinal;
                        if (item.PaqueteInsumos != null && item.PaqueteInsumos.Any(p => p.Paquete != null))
                        {
                            decimal totalPaquetes = item.PaqueteInsumos
                                .Where(p => p.Paquete != null)
                                .Sum(p => (decimal)p.Paquete!.CantidadPaquete * p.Paquete.PesoUnitario);
                            if (totalPaquetes > 0)
                            {
                                cantidadRecibida = totalPaquetes;
                            }
                        }

                        item.CantidadRecibida = cantidadRecibida;
                        x = 1;
                        StockInsumo? stockInsumo = await _context.StockInsumos.FirstOrDefaultAsync(f => f.IdCompraInsumo == item.Id);
                        if (stockInsumo != null)
                        {
                            stockInsumo.StockDisponible = cantidadRecibida;
                            if (compra != null && (stockInsumo.IdSede == null || stockInsumo.IdSede == 0))
                            {
                                stockInsumo.IdSede = compra.IdSede;
                            }
                        }
                        else
                        {
                            StockInsumo stockInsumo2 = new()
                            {
                                IdCompraInsumo = item.Id,
                                IdSede = compra != null ? compra.IdSede : 15,
                                Tipo = "MP",
                                StockDisponible = cantidadRecibida,
                                UnidadMedida = "G"
                            };
                            await _context.StockInsumos.AddAsync(stockInsumo2);
                        }
                    }
                }
            }
            if (request.Empaques != null && request.Empaques.Any())
            {
                List<ActualizarEmpaqueReq> empaques = request.Empaques;
                IEnumerable<int> idEmpaques = empaques.Select(s => s.IdCompraEmpaque).ToList();

                List<CompraEmpaque> compraEmpaque = await _context.CompraEmpaques
                    .Include(w => w.PaqueteEmpaques!)
                    .ThenInclude(p => p.Paquete)
                    .Where(w => w.IdCompra == idCompra && idEmpaques.Contains(w.Id)).ToListAsync();

                foreach (var item in compraEmpaque)
                {
                    ActualizarEmpaqueReq? req = empaques.FirstOrDefault(f => f.IdCompraEmpaque == item.Id);
                    if (req != null)
                    {
                        new DetalleCompraLabMapper().ActualizarEmpaque(req, item);
                        item.Conformidad = UtilConformidad.CalcularConformidad(req.FechaVencimiento);
                        x = 1;

                        decimal totalPaquetes = 0;
                        if (item.PaqueteEmpaques != null && item.PaqueteEmpaques.Any(p => p.Paquete != null))
                        {
                            totalPaquetes = item.PaqueteEmpaques
                                .Where(p => p.Paquete != null)
                                .Sum(p => (decimal)p.Paquete!.CantidadPaquete * p.Paquete.PesoUnitario);
                        }

                        StockEmpaque? stockEmpaque = await _context.StockEmpaques.FirstOrDefaultAsync(f => f.IdCompraEmpaque == item.Id);
                        if (stockEmpaque != null)
                        {
                            if (totalPaquetes > 0)
                            {
                                stockEmpaque.StockDisponible = totalPaquetes;
                            }
                            if (compra != null && (stockEmpaque.IdSede == null || stockEmpaque.IdSede == 0))
                            {
                                stockEmpaque.IdSede = compra.IdSede;
                            }
                        }
                        else if (totalPaquetes > 0)
                        {
                            StockEmpaque stockEmpaque2 = new()
                            {
                                IdCompraEmpaque = item.Id,
                                IdSede = compra != null ? compra.IdSede : 15,
                                StockDisponible = totalPaquetes,
                                UnidadMedida = item.Um ?? "UND"
                            };
                            await _context.StockEmpaques.AddAsync(stockEmpaque2);
                        }
                    }
                }
            }
            if (x == 1)
            {
                if (compra != null)
                {
                    compra.FechaLab = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        private static readonly HashSet<string> UnidadesVolumen = new(StringComparer.OrdinalIgnoreCase) { "L", "LITRO" };

        private static (string Um, decimal Cantidad) NormalizarUnidad(string? um, decimal cantidadSolicitada)
        {
            var umUpper = (um ?? "").ToUpper();
            return UnidadesVolumen.Contains(umUpper)
                ? ("ML", cantidadSolicitada * 1000)
                : (umUpper, cantidadSolicitada);
        }

        private static string FormatearFabricante(string? codigo, string? nombre, string? pais)
            => string.IsNullOrEmpty(codigo) && string.IsNullOrEmpty(nombre)
                ? ""
                : $"{codigo ?? nombre} ({pais})";

        public async Task<ObtenerCompraLabRes> ModalPaquetes(int idCompra)
        {
            var compra = await _context.Compras
                .AsNoTracking()
                .Where(w => w.Id == idCompra)
                .AsSplitQuery()
                .Select(s => new
                {
                    CodigoProveedor = s.Proveedor != null ? s.Proveedor.CodigoProvedor ?? "" : "",
                    Ruc = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                    NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                    CodFacQbd = s.CodFacQBD,
                    Insumos = s.CompraInsumos != null ? s.CompraInsumos.OrderBy(s2 => s2.Id).Select(s2 => new
                    {
                        s2.Id,
                        Familia = s2.Insumo != null && s2.Insumo.Familia != null ? s2.Insumo.Familia.Abreviatura : "",
                        Codigo = s2.IdInsumo,
                        DescripcionQBD = s2.Insumo != null ? s2.Insumo.Descripcion : "",
                        s2.Coa,
                        Lote = s2.Lote ?? "",
                        s2.Um,
                        CantidadSolicitada = s2.CantidadSolicitada,
                        s2.Potencia,
                        s2.FechaFabricacion,
                        s2.FechaVencimiento,
                        CondicionALmacenamiento = s2.CondicionAlmacenamiento ?? "",
                        TotalPaquetes = s2.PaqueteInsumos.Sum(p => p.Paquete.CantidadPaquete),
                        TotalPeso = s2.PaqueteInsumos.Sum(p => p.Paquete.CantidadPaquete * p.Paquete.PesoUnitario),
                        FabCodigo = s2.Fabricante != null ? s2.Fabricante.Codigo : null,
                        FabNombre = s2.Fabricante != null ? s2.Fabricante.Nombre : null,
                        FabPais = s2.Fabricante != null ? s2.Fabricante.Pais : null,
                        Densidad = s2.Insumo != null ? s2.Insumo.Densidad : null,
                        DescripcionFactura = s2.DescripcionFactura ?? "",
                        Observacion = s2.Observacion ?? ""
                    }).ToList() : null,
                    Empaques = s.CompraEmpaques != null ? s.CompraEmpaques.OrderBy(s3 => s3.Id).Select(s3 => new
                    {
                        s3.Id,
                        Familia = s3.Empaque != null && s3.Empaque.Familia != null ? s3.Empaque.Familia.Abreviatura : "",
                        Codigo = s3.IdEmpaque,
                        DescripcionQBD = s3.Empaque != null ? s3.Empaque.Descripcion ?? "" : "",
                        s3.Coa,
                        Lote = s3.Lote ?? "",
                        s3.Um,
                        CantidadSolicitada = s3.CantidadSolicitada,
                        s3.FechaFabricacion,
                        s3.FechaVencimiento,
                        CondicionALmacenamiento = s3.CondicionAlmacenamiento ?? "",
                        TotalPaquetes = s3.PaqueteEmpaques.Sum(p => p.Paquete.CantidadPaquete),
                        TotalPeso = s3.PaqueteEmpaques.Sum(p => p.Paquete.CantidadPaquete * p.Paquete.PesoUnitario),
                        FabCodigo = s3.Fabricante != null ? s3.Fabricante.Codigo : null,
                        FabNombre = s3.Fabricante != null ? s3.Fabricante.Nombre : null,
                        FabPais = s3.Fabricante != null ? s3.Fabricante.Pais : null,
                        DescripcionFactura = s3.DescripcionFactura ?? "",
                        Observacion = s3.Observacion ?? ""
                    }).ToList() : null
                })
                .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontro Compra");

            // Normalización de unidades y formateo se hace en memoria, una sola vez por fila
            var result = new ObtenerCompraLabRes
            {
                CodigoProveedor = compra.CodigoProveedor,
                Ruc = compra.Ruc,
                NumProvedor = compra.NumProvedor,
                CodFacQbd = compra.CodFacQbd,
                DetalleInsumos = compra.Insumos?.Select(s2 =>
                {
                    var (um, cantidad) = NormalizarUnidad(s2.Um, s2.CantidadSolicitada);
                    return new CompraLabInsumoModalRes
                    {
                        Id = s2.Id,
                        Reg = Alfanumerico.ConvertToBase36(s2.Id).PadLeft(4, '0'),
                        Familia = s2.Familia,
                        Codigo = s2.Codigo.ToString(),
                        DescripcionQBD = s2.DescripcionQBD,
                        Coa = s2.Coa,
                        Lote = s2.Lote,
                        Um = um,
                        CantidadRecibida = cantidad,
                        Potencia = s2.Potencia,
                        FechaFabricacion = s2.FechaFabricacion,
                        FechaVencimiento = s2.FechaVencimiento,
                        CondicionALmacenamiento = s2.CondicionALmacenamiento,
                        TotalPaquetes = s2.TotalPaquetes,
                        TotalPeso = s2.TotalPeso,
                        Fabricante = FormatearFabricante(s2.FabCodigo, s2.FabNombre, s2.FabPais),
                        Densidad = s2.Densidad,
                        DescripcionFactura = s2.DescripcionFactura,
                        Observacion = s2.Observacion
                    };
                }).ToList() ?? new List<CompraLabInsumoModalRes>(),
                DetalleEmpaques = compra.Empaques?.Select(s3 =>
                {
                    var (um, cantidad) = NormalizarUnidad(s3.Um, s3.CantidadSolicitada);
                    return new CompraLabEmpaqueModalRes
                    {
                        Id = s3.Id,
                        Reg = Alfanumerico.ConvertToBase36(s3.Id).PadLeft(4, '0'),
                        Familia = s3.Familia,
                        Codigo = s3.Codigo.ToString(),
                        DescripcionQBD = s3.DescripcionQBD,
                        Coa = s3.Coa,
                        Lote = s3.Lote,
                        Um = um,
                        CantidadRecibida = cantidad,
                        FechaFabricacion = s3.FechaFabricacion,
                        FechaVencimiento = s3.FechaVencimiento,
                        CondicionALmacenamiento = s3.CondicionALmacenamiento,
                        TotalPaquetes = s3.TotalPaquetes,
                        TotalPeso = s3.TotalPeso,
                        Fabricante = FormatearFabricante(s3.FabCodigo, s3.FabNombre, s3.FabPais),
                        DescripcionFactura = s3.DescripcionFactura,
                        Observacion = s3.Observacion
                    };
                }).ToList() ?? new List<CompraLabEmpaqueModalRes>()
            };

            return result;
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

                    ListaInsumos = c.CompraInsumos.OrderBy(i => i.Id).Select(i => new CompraLabDetInsumosRes
                    {
                        Id = i.Id,
                        Familia = i.Insumo != null ? i.Insumo.Familia!.Abreviatura : "",
                        Conformidad = i.Conformidad ?? "Conforme",
                        CodigoInsumo = i.IdInsumo.ToString(),
                        DescripcionQBD = i.Insumo != null ? i.Insumo.Descripcion : "",
                        Coa = i.Coa,
                        Lote = i.Lote ?? "",
                        Um = "G",
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

                    ListaEmpaques = c.CompraEmpaques.OrderBy(e => e.Id).Select(e => new CompraLabDetEmpRes
                    {
                        Id = e.Id,
                        Familia = e.Empaque != null ? e.Empaque.Familia!.Abreviatura : "",
                        Conformidad = e.Conformidad ?? "Conforme",
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
            {
                item.Reg = Alfanumerico.ConvertToBase36(item.Id).PadLeft(4, '0');
                item.Conformidad = UtilConformidad.CalcularConformidad(item.FechaVencimiento);
            }

            foreach (var item in response.ListaEmpaques)
            {
                item.Reg = Alfanumerico.ConvertToBase36(item.Id).PadLeft(4, '0');
                item.Conformidad = UtilConformidad.CalcularConformidad(item.FechaVencimiento);
            }

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
        public async Task<List<LabListaRes>> Listar(string[] cadena, int idSede)
        {
            var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.Id == idSede);
            bool esCentral = idSede == 0 || idSede == 15 || (sede != null && sede.Nombre != null && sede.Nombre.ToUpper().Contains("CENTRAL"));

            var query = _context.Compras.AsQueryable();
            if (!esCentral)
            {
                query = query.Where(w => w.IdSede == idSede);
            }

            List<LabListaRes> ordenesEnviadasRes = await query
            .Where(w => cadena.Contains(w.EstadoCompra) && w.CompraInsumos != null && w.CompraEmpaques != null)
            .Select(s => new LabListaRes
            {
                Id = s.Id,
                CUO = (s.IdSede == 1 ? "OCL" : (s.IdSede == 2 ? "OCPJ" : (s.IdSede == 3 ? "OCP" : (s.IdSede == 4 ? "OCT" : (s.IdSede == 12 ? "OCCS" : "OC"))))) + s.Id,
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


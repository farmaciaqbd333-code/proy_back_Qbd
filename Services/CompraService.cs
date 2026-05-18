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
    public class CompraService : ICompraService
    {
        private readonly ApiContext _context;
        private readonly IDetalleOrdenCompraService _serviceDOC;
        public CompraService(ApiContext context, IDetalleOrdenCompraService serviceDOC)
        {
            _context = context;
            this._serviceDOC = serviceDOC;
        }

        public async Task<List<OrdenesEnviadasRes>> ListaOrdenesEnviadas(string[] cadena)
        {
            List<OrdenesEnviadasRes> ordenesEnviadasRes = await _context.Compras
            .Where(w => cadena.Contains(w.EstadoCompra))
            .Select(s => new OrdenesEnviadasRes
            {
                Id = s.Id,
                CUO = "BDCO" + s.Id,
                FechaCotizacion = s.FechaCotizacion,
                FechaFactura = s.FechaFactura,
                SerieComprobante = s.SerieComprobante,
                NumeroComprobante = s.NumeroComprobante,
                Factura = (s.SerieComprobante ?? "") + (string.IsNullOrEmpty(s.SerieComprobante) || string.IsNullOrEmpty(s.NumeroComprobante) ? "" : "-") + (s.NumeroComprobante ?? ""),
                Guia = s.Guia ?? "",
                CodFacQbd = s.CodFacQBD,
                NumProvedor = s.Proveedor != null ? s.Proveedor.NumeroProv : "",
                NombreProveedor = s.Proveedor != null ? s.Proveedor.Datos : "",
                EstadoCompra = s.EstadoCompra,
                Familia = s.Familia,
                Usuario = s.Creador != null && s.Creador.Codigo != null ? s.Creador.Codigo : "",
                RutaFactura = s.ImgFactura,
            })
            .OrderByDescending(o => o.FechaCotizacion)
            .ToListAsync();
            if (ordenesEnviadasRes.Count() == 0) return new List<OrdenesEnviadasRes>();

            return ordenesEnviadasRes;
        }

        public async Task<ObtenerOrdenOCompraRes2> ObtenerDetalleOrdenOCompra(int id)
        {
            ObtenerOrdenOCompraRes2? response = await _context.Compras
                        .Where(w => w.Id == id)
                        .Select(s => new ObtenerOrdenOCompraRes2
                        {
                            TC = s.TipoCambio.ToString(),
                            Destino = s.Sede == null || s.Sede.Nombre == null ? "" : s.Sede.Nombre,
                            Direccion = s.Sede == null || s.Sede.Direccion == null ? "" : s.Sede.Direccion,
                            DetalleOrdenCompras = s.DetalleCompras == null ? null : s.DetalleCompras.Select(s2 => new ObtenerDetalleOrdenOCompraRes2
                            {
                                Id = s2.Id,
                                Reg = Alfanumerico.ConvertToBase36(s2.Reg.Value),
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
                                Familia = s2.Familia != null ? s2.Familia.Abreviatura : ""
                            }).ToList(),
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
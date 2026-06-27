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
using Proy_back_QBD.Models;

namespace proy_back_Qbd.Services
{
    public class PaqueteService : IPaqueteService
    {
        public readonly ApiContext _context;
        public readonly IMapper _mapper;
        public PaqueteService(ApiContext _context, IMapper _mapper)
        {
            this._context = _context;
            this._mapper = _mapper;
        }

        public async Task<int> CrearPaqueteInsumo(PaqueteInsumoCrearReq req)
        {
            CompraInsumos compraInsumo = await _context.CompraInsumos
                .Include(i => i.Insumo)
                .Include(i => i.PaqueteInsumos!)
                .ThenInclude(p => p.Paquete)
                .FirstOrDefaultAsync(f => f.Id == req.IdCompraInsumo) ?? throw new NotFoundException("No se encontró la compra insumo");
            List<PaqueteInsumo> paqueteInsumos = compraInsumo.PaqueteInsumos ?? throw new NotFoundException("No se encontró el paquete insumo"); ;
            List<Paquete> paquetes = new();
            Insumo insumo = compraInsumo.Insumo ?? throw new NotFoundException("No hay Insumo");

            if (paqueteInsumos.Count != 0)
            {
                foreach (var item in paqueteInsumos)
                {
                    if (item.Paquete == null) throw new NotFoundException("No se encontró el Paquete");
                    paquetes.Add(item.Paquete);
                }
            }

            //CONVERSION A GRAMOS
            decimal pesoTotalCompra;
            string um = compraInsumo.Um;
            decimal cantidadSolicitada = compraInsumo.CantidadSolicitada;
            if (compraInsumo.Um == "G")
            {
                pesoTotalCompra = cantidadSolicitada;
            }
            else if (compraInsumo.Um == "KG")
            {
                pesoTotalCompra = cantidadSolicitada * 1000;
            }
            else if (compraInsumo.Um == "L")
            {
                decimal densidad = insumo.Densidad ?? throw new NotFoundException("No se encontró la densidad del insumo"); ;
                pesoTotalCompra = cantidadSolicitada * densidad;
            }
            else throw new NotFoundException("Unidad de Medida no apta");

            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;
            decimal pesoTotalPaquete = paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario);
            decimal pesoPaqueteNuevo = paqueteEntrante + pesoTotalPaquete;
            if (pesoTotalCompra < pesoPaqueteNuevo) throw new BadRequestException("Se ha pasado el límite del peso solicitado");

            Paquete paquete = PaqueteMapper.CrearPaqueteInsumo(req);
            paquete.FechaCreacion = DateTime.Now;
            _context.Paquetes.Add(paquete);
            await _context.SaveChangesAsync();
            PaqueteInsumo paqueteInsumo = new()
            {
                IdPaquete = paquete.Id,
                IdCompraInsumo = req.IdCompraInsumo
            };
            _context.PaqueteInsumos.Add(paqueteInsumo);
            await _context.SaveChangesAsync();
            return paquete.Id;

        }
        public async Task<int> CrearPaqueteEmpaque(PaqueteEmpaqueCrearReq req)
        {
            //CONSULTA
            CompraEmpaques compraEmpaque = await _context.CompraEmpaques
                .Include(i => i.PaqueteEmpaques!)
                .ThenInclude(p => p.Paquete)
                .FirstOrDefaultAsync(f => f.Id == req.IdCompraEmpaque) ?? throw new NotFoundException("No se encontró la compra Empaque");
            List<PaqueteEmpaque> paqueteEmpaques = compraEmpaque.PaqueteEmpaques ?? throw new NotFoundException("No se encontró Paquete Empaque");
            List<Paquete> paquetes = new();
            if (paqueteEmpaques.Count != 0)
            {
                foreach (var item in paqueteEmpaques)
                {
                    if (item.Paquete == null) throw new NotFoundException("No se encontró el Paquete");
                    paquetes.Add(item.Paquete);
                }
            }

            decimal pesoTotalPaquete = paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario);
            decimal pesoTotalSolicitado = compraEmpaque.CantidadSolicitada;
            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;

            if (pesoTotalSolicitado < (paqueteEntrante + pesoTotalPaquete))
                throw new BadRequestException("Se ha pasado el límite de unidades solicitadas");

            Paquete paquete = PaqueteMapper.CrearPaqueteEmpaque(req);
            paquete.FechaCreacion = DateTime.Now;
            _context.Paquetes.Add(paquete);
            await _context.SaveChangesAsync();

            PaqueteEmpaque paqueteEmpaque = new()
            {
                IdPaquete = paquete.Id,
                IdCompraEmpaque = req.IdCompraEmpaque
            };
            _context.PaqueteEmpaques.Add(paqueteEmpaque);
            compraEmpaque.StockDisponible += paqueteEntrante;
            await _context.SaveChangesAsync();

            return paquete.Id;

        }

        public async Task<string> EliminarPaquete(int idPaquete, int empaqueInsumo)
        {
            Paquete paquete = await _context.Paquetes.FindAsync(idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            _context.Paquetes.Remove(paquete);
            if (empaqueInsumo == 0)
            {
                CompraInsumos compraInsumo = await _context.PaqueteInsumos
                           .Where(w => w.IdPaquete == idPaquete)
                           .Select(s => s.CompraInsumo)
                           .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontró el compra insumo"); ;
                compraInsumo.StockDisponible -= paquete.PesoUnitario * paquete.CantidadPaquete;
            }
            else
            {
                CompraEmpaques compraEmpaque = await _context.PaqueteEmpaques
           .Where(w => w.IdPaquete == idPaquete)
           .Select(s => s.CompraEmpaques)
           .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontró el compra empaque"); ;
                compraEmpaque.StockDisponible -= paquete.PesoUnitario * paquete.CantidadPaquete;
            }

            await _context.SaveChangesAsync();
            return "Se Elimino el paquete id " + idPaquete;
        }

        public async Task<string> ModificarPaqueteInsumo(int idPaquete, PaqueteInsumoModificarReq req)
        {

            Paquete paquete = await _context.Paquetes
            .Include(i => i.PaqueteInsumos)
            .ThenInclude(th => th!.CompraInsumo)
            .ThenInclude(th => th!.Insumo)
            .FirstOrDefaultAsync(f => f.Id == idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            PaqueteInsumo paqueteInsumo = paquete.PaqueteInsumos ?? throw new NotFoundException("No se encontró paquetes insumos");
            CompraInsumos compraInsumo = paqueteInsumo.CompraInsumo ?? throw new NotFoundException("No hay Compra Insumos");
            Insumo insumo = compraInsumo.Insumo ?? throw new NotFoundException("No hay Insumo");

            var totales = await _context.PaqueteInsumos
                                    .Where(w => w.IdCompraInsumo == compraInsumo.Id)
                                    .GroupBy(g => g.IdCompraInsumo)
                                    .Select(s => new
                                    {
                                        paquetePesoTotal = s.Sum(s2 => s2.Paquete != null ? (s2.Paquete.CantidadPaquete * s2.Paquete.PesoUnitario) : 0),
                                        PesoTotalCompra = s.Sum(s3 => s3.CompraInsumo != null ? s3.CompraInsumo.CantidadSolicitada * 1000 : 0)
                                    }).FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontró el detalle compra");

            //CONVERSION A GRAMOS
            decimal pesoTotalCompra;
            string Um = compraInsumo.Um;
            decimal CantidadSolicitada = compraInsumo.CantidadSolicitada;
            if (Um == "G")
            {
                pesoTotalCompra = CantidadSolicitada;
            }
            else if (Um == "KG")
            {
                pesoTotalCompra = CantidadSolicitada * 1000;
            }
            else if (Um == "L")
            {
                decimal densidad = insumo.Densidad ?? throw new NotFoundException("No se encontró la densidad del insumo"); ;
                pesoTotalCompra = CantidadSolicitada * densidad;
            }
            else throw new NotFoundException("Unidad de Medida no apta");

            //VALIDACION DE CANTIDAD DE PESO COMPRA
            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            decimal paquetePesoEntrante = req.CantidadPaquete * req.PesoUnitario;
            decimal nuevoPeso = paquetePesoEntrante + totales.paquetePesoTotal - paquetePesoActual;
            if (pesoTotalCompra < nuevoPeso)
                throw new BadRequestException("Se ha pasado el límite del peso solicitado");

            //ACTUALIZAR PESO
            PaqueteMapper.ModificarPaqueteInsumo(req, paquete);
            compraInsumo.StockDisponible = compraInsumo.StockDisponible - paquetePesoActual + paquetePesoEntrante;
            await _context.SaveChangesAsync();

            return "Modificacion Exitosa";
        }
        public async Task<string> ModificarPaqueteEmpaque(int idPaquete, PaqueteEmpaqueModificarReq req)
        {

            Paquete? paquete = await _context.Paquetes
            .Include(i => i.PaqueteEmpaques)
            .ThenInclude(th => th!.CompraEmpaques)
            .FirstOrDefaultAsync(f => f.Id == idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            PaqueteEmpaque paqueteEmpaques = paquete.PaqueteEmpaques ?? throw new NotFoundException("No se encontró paquetes empaques");
            CompraEmpaques CompraEmpaque = paqueteEmpaques.CompraEmpaques ?? throw new NotFoundException("No hay Compra empaque");
            if (paquete.PaqueteEmpaques == null) throw new NotFoundException("No se encontró paquetes Empaques");
            var totales = await _context.PaqueteEmpaques.Where(w => w.IdCompraEmpaque == paquete.PaqueteEmpaques.IdCompraEmpaque)
                                    .GroupBy(g => g.IdCompraEmpaque)
                                    .Select(s => new
                                    {
                                        PesoTotalPaquete = s.Sum(s2 => s2.Paquete != null ? (s2.Paquete.CantidadPaquete * s2.Paquete.PesoUnitario) : 0),
                                        PesoTotalCompra = s.Sum(s3 => s3.CompraEmpaques != null ? s3.CompraEmpaques.CantidadSolicitada * 1000 : 0m)
                                    }).FirstOrDefaultAsync()
                                    ;
            if (totales == null)
                throw new NotFoundException("No se encontró el detalle compra");
            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            decimal paquetePesoEntrante = req.CantidadPaquete * req.PesoUnitario;
            decimal nuevaCantidad = paquetePesoEntrante + totales.PesoTotalPaquete - paquetePesoActual;
            if (totales.PesoTotalCompra < nuevaCantidad)
                throw new BadRequestException("Se ha pasado el límite de cantidad solicitada");
            PaqueteMapper.ModificarPaqueteEmpaque(req, paquete);
            CompraEmpaque.StockDisponible = CompraEmpaque.StockDisponible - paquetePesoActual + paquetePesoEntrante;
            await _context.SaveChangesAsync();

            return "Modificacion Exitosa";
        }
        public async Task<PaqueteInsumoDetalleRes> GetDetallePaquetes(int idCompra)
        {
            PaqueteInsumoDetalleRes response = new();
            List<PaqueteInsumoListRes> ListaInsumos = _context.PaqueteInsumos
            .Where(w => w.CompraInsumo != null && w.CompraInsumo.IdCompra == idCompra)
            .Select(s => new PaqueteInsumoListRes
            {
                IdPaquete = s.IdPaquete,
                CodigoCompraInsumo = s.CompraInsumo != null ? ("MP-QbD-" + s.CompraInsumo.IdInsumo.ToString("D4")) : "",
                CantidadPaquete = s.Paquete != null ? s.Paquete.CantidadPaquete : 0,
                PesoUnitario = s.Paquete != null ? s.Paquete.PesoUnitario : 0,
                Tara = s.Paquete != null ? s.Paquete.Tara : 0
            }).ToList();
            List<PaqueteEmpaqueListRes> ListaEmpaques = _context.PaqueteEmpaques
            .Where(w => w.CompraEmpaques != null && w.CompraEmpaques.IdCompra == idCompra)
            .Select(s => new PaqueteEmpaqueListRes
            {
                IdPaquete = s.IdPaquete,
                CodigoCompraEmpaque = "ME-QbD-" + (s.CompraEmpaques != null ? s.CompraEmpaques.IdEmpaque.ToString("D4") : ""),
                CantidadPaquete = s.Paquete != null ? s.Paquete.CantidadPaquete : 0,
                PesoUnitario = s.Paquete != null ? s.Paquete.PesoUnitario : 0,
                Tara = s.Paquete != null ? s.Paquete.Tara : 0
            }).ToList();
            response.ListaInsumos = ListaInsumos;
            response.ListaEmpaques = ListaEmpaques;
            return response;
        }
    }
}
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
            var compraInsumo = await _context.CompraInsumos
                .Include(i => i.PaqueteInsumos)
                .ThenInclude(p => p.Paquete)
                .FirstOrDefaultAsync(f => f.Id == req.IdCompraInsumo) ?? throw new NotFoundException("No se encontró la compra insumo");

            decimal pesoTotalPaquete = compraInsumo.PaqueteInsumos != null
                ? compraInsumo.PaqueteInsumos.Sum(s => s.Paquete != null ? s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario : 0m)
                : 0m;

            decimal pesoTotalSolicitado = (compraInsumo.CantidadRecibida ?? 0m) * 1000;

            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;

            if (pesoTotalSolicitado < (paqueteEntrante + pesoTotalPaquete))
                throw new BadRequestException("Se ha pasado el límite del peso solicitado");

            Paquete paquete = PaqueteMapper.CrearPaqueteInsumo(req);
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
            var compraEmpaque = await _context.CompraEmpaques
                .Include(i => i.PaqueteEmpaques)
                .ThenInclude(p => p.Paquete)
                .FirstOrDefaultAsync(f => f.Id == req.IdCompraEmpaque) ?? throw new NotFoundException("No se encontró la compra Empaque");

            decimal pesoTotalPaquete = compraEmpaque.PaqueteEmpaques != null
                ? compraEmpaque.PaqueteEmpaques.Sum(s => s.Paquete != null ? s.Paquete.CantidadPaquete * s.Paquete.PesoUnitario : 0m)
                : 0m;

            decimal pesoTotalSolicitado = (compraEmpaque.CantidadRecibida ?? 0m) * 1000;

            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;

            if (pesoTotalSolicitado < (paqueteEntrante + pesoTotalPaquete))
                throw new BadRequestException("Se ha pasado el límite del peso solicitado");

            Paquete paquete = PaqueteMapper.CrearPaqueteEmpaque(req);
            _context.Paquetes.Add(paquete);
            await _context.SaveChangesAsync();

            PaqueteEmpaque paqueteEmpaque = new()
            {
                IdPaquete = paquete.Id,
                IdCompraEmpaque = req.IdCompraEmpaque
            };
            _context.PaqueteEmpaques.Add(paqueteEmpaque);

            await _context.SaveChangesAsync();

            return paquete.Id;

        }

        public async Task<string> EliminarPaquete(int idPaquete)
        {
            Paquete? paquete = await _context.Paquetes.FindAsync(idPaquete) ?? throw new NotFoundException("No se encontró el paquete");

            _context.Paquetes.Remove(paquete);
            await _context.SaveChangesAsync();
            return "Se Elimino el id " + idPaquete;
        }

        public async Task<string> ModificarPaqueteInsumo(int idPaquete, PaqueteInsumoModificarReq req)
        {

            Paquete? paquete = await _context.Paquetes
            .Include(i => i.PaqueteInsumos)
            .FirstOrDefaultAsync(f => f.Id == idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            if (paquete.PaqueteInsumos == null) throw new NotFoundException("No se encontró paquetes insumos");

            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            var validar = await _context.PaqueteInsumos.Where(w => w.IdCompraInsumo == paquete.PaqueteInsumos.IdCompraInsumo)
                                    .GroupBy(g => g.IdCompraInsumo)
                                    .Select(s => new
                                    {
                                        PesoTotalPaquete = s.Sum(s2 => s2.Paquete != null ? (s2.Paquete.CantidadPaquete * s2.Paquete.PesoUnitario) : null),
                                        PesoTotalSolicitado = s.Sum(s3 => s3.CompraInsumos != null ? s3.CompraInsumos.CantidadSolicitada * 1000 : 0m)
                                    }).FirstOrDefaultAsync()
                                    ;
            if (validar == null)
                throw new NotFoundException("No se encontró el detalle compra");
            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;
            if (validar.PesoTotalSolicitado < (paqueteEntrante + validar.PesoTotalPaquete - paquetePesoActual))
                throw new BadRequestException("Se ha pasado el límite del peso solicitado");
            PaqueteMapper.ModificarPaqueteInsumo(req, paquete);
            await _context.SaveChangesAsync();

            return "Modificacion Exitosa";
        }
        public async Task<string> ModificarPaqueteEmpaque(int idPaquete, PaqueteEmpaqueModificarReq req)
        {

            Paquete? paquete = await _context.Paquetes
            .Include(i => i.PaqueteEmpaques)
            .FirstOrDefaultAsync(f => f.Id == idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            if (paquete.PaqueteEmpaques == null) throw new NotFoundException("No se encontró paquetes Empaques");

            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            var validar = await _context.PaqueteEmpaques.Where(w => w.IdCompraEmpaque == paquete.PaqueteEmpaques.IdCompraEmpaque)
                                    .GroupBy(g => g.IdCompraEmpaque)
                                    .Select(s => new
                                    {
                                        PesoTotalPaquete = s.Sum(s2 => s2.Paquete != null ? (s2.Paquete.CantidadPaquete * s2.Paquete.PesoUnitario) : null),
                                        PesoTotalSolicitado = s.Sum(s3 => s3.CompraEmpaques != null ? s3.CompraEmpaques.CantidadSolicitada * 1000 : 0m)
                                    }).FirstOrDefaultAsync()
                                    ;
            if (validar == null)
                throw new NotFoundException("No se encontró el detalle compra");
            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;
            if (validar.PesoTotalSolicitado < (paqueteEntrante + validar.PesoTotalPaquete - paquetePesoActual))
                throw new BadRequestException("Se ha pasado el límite del peso solicitado");
            PaqueteMapper.ModificarPaqueteEmpaque(req, paquete);
            await _context.SaveChangesAsync();

            return "Modificacion Exitosa";
        }
        public async Task<PaqueteInsumoDetalleRes> GetDetallePaquetes(int idCompra)
        {
            PaqueteInsumoDetalleRes response = new();
            List<PaqueteInsumoListRes> ListaInsumos = _context.PaqueteInsumos
            .Where(w => w.CompraInsumos != null && w.CompraInsumos.IdCompra == idCompra)
            .Select(s => new PaqueteInsumoListRes
            {
                IdPaquete = s.IdPaquete,
                CodigoCompraInsumo = s.CompraInsumos != null ? ("MP-QbD-" + s.CompraInsumos.IdInsumo.ToString("D4")) : "",
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
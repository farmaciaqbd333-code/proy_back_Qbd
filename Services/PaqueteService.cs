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

        public async Task<int> CrearPaquete(CrearPaqueteReq req)
        {

            var validar = await _context.Paquetes.Where(w => w.IdDetalleCompra == req.IdDetalleCompra)
            .GroupBy(g => g.IdDetalleCompra)
            .Select(s => new
            {
                PesoTotalPaquete = s.Sum(s2 => s2.CantidadPaquete * s2.PesoUnitario),
                PesoTotalSolicitado = s.Sum(s3 => s3.DetalleCompra != null ? s3.DetalleCompra.CantidadSolicitada * 1000 : 0m)
            }).FirstOrDefaultAsync()
            ;
            if (validar == null)
                throw new NotFoundException("No se encontró el detalle compra");
            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;
            if (validar.PesoTotalSolicitado < (paqueteEntrante + validar.PesoTotalPaquete))
                throw new BadRequestException("Se ha pasado el límite del peso solicitado");

            Paquete paquete = _mapper.Map<Paquete>(req);
            _context.Paquetes.Add(paquete);

            await _context.SaveChangesAsync();

            return paquete.Id;


        }

        public async Task<bool> EliminarPaquete(int idPaquete)
        {
            Paquete? paquete = await _context.Paquetes.FindAsync(idPaquete);
            if (paquete == null) return false;

            _context.Paquetes.Remove(paquete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ModificarPaquete(int idPaquete, ModificarPaqueteReq req)
        {

            Paquete? paquete = await _context.Paquetes.FindAsync(idPaquete);

            if (paquete == null)
                throw new NotFoundException("No se encontró el paquete");

            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            var validar = await _context.Paquetes.Where(w => w.IdDetalleCompra == paquete.IdDetalleCompra)
                                    .GroupBy(g => g.IdDetalleCompra)
                                    .Select(s => new
                                    {
                                        PesoTotalPaquete = s.Sum(s2 => s2.CantidadPaquete * s2.PesoUnitario),
                                        PesoTotalSolicitado = s.Sum(s3 => s3.DetalleCompra != null ? s3.DetalleCompra.CantidadSolicitada * 1000 : 0m)
                                    }).FirstOrDefaultAsync()
                                    ;
            if (validar == null)
                throw new NotFoundException("No se encontró el detalle compra");
            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;
            if (validar.PesoTotalSolicitado < (paqueteEntrante + validar.PesoTotalPaquete - paquetePesoActual))
                throw new BadRequestException("Se ha pasado el límite del peso solicitado");
            _mapper.Map(req, paquete);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<DetallePaqueteRes?> GetDetallePaquete(int idDetalleCompra)
        {
            DetallePaqueteRes? response = await _context.Paquetes
            .Where(w => w.IdDetalleCompra == idDetalleCompra)
            .Select(s => new DetallePaqueteRes
            {
                PesoUnitario = s.PesoUnitario,
                Tara = s.Tara,
                Lista = _context.Paquetes.Where(w => w.IdDetalleCompra == idDetalleCompra).Select(s => new ListaDetallePaqueteRes
                {
                    IdInsumo = "MP-QbD-" + (s.DetalleCompra != null ? s.DetalleCompra.IdInsumo.ToString("D4") : ""),
                    CantidadPaquete = s.CantidadPaquete,
                    PesoUnitario = s.PesoUnitario,
                    Tara = s.Tara
                }).ToList()
            }).FirstOrDefaultAsync();
            if (response == null) return null;

            return response;
        }
    }
}
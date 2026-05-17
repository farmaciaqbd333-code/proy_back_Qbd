using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
            bool validar = await _context.Paquetes.Where(w => w.IdDetalleCompra == req.IdDetalleCompra).AnyAsync();


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
            if (paquete == null) return false;
            _mapper.Map(req, paquete);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (System.Exception)
            {

                return false;
            }
            return true;
        }

        public async Task<DetallePaqueteRes?> ObtenerDetallePaquete(int idDetalleCompra)
        {
            DetallePaqueteRes? response = await _context.Paquetes
            .Where(w => w.IdDetalleCompra == idDetalleCompra)
            .Select(s => new DetallePaqueteRes
            {
                PesoUnitario = s.PesoUnitario,
                Tara = s.Tara,
                Lista = _context.Paquetes.Where(w => w.IdDetalleCompra == idDetalleCompra).Select(s => new ListaDetallePaqueteRes
                {
                    PesoUnitario = s.PesoUnitario,
                    Tara = s.Tara
                }).ToList()
            }).FirstOrDefaultAsync();
            if (response == null) return null;

            return response;
        }
    }
}
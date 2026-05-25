using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class DetalleOrdenCompraService : IDetalleOrdenCompraService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public DetalleOrdenCompraService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<bool> CrearDetalleOrdenDeCompra(int idCompra, int idCreador, IEnumerable<DetalleInsumosCreateReq> request)
        {
            try
            {
                foreach (var item in request)
                {
                    DetalleCompraInsumo detalleCompra = _mapper.Map<DetalleCompraInsumo>(item);
                    detalleCompra.IdCompra = idCompra;
                    detalleCompra.IdCreador = idCreador;
                    _context.DetalleCompras.Add(detalleCompra);
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                return false;
            }

            return true;
        }

        public async Task<bool> EliminarDetalleOrdenDeCompra(IEnumerable<int> request)
        {
            try
            {
                var detalles = await _context.DetalleCompras
                .Where(x => request.Contains(x.Id))
                .ToListAsync();
                _context.DetalleCompras.RemoveRange(detalles);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                return false;
            }

            return true;
        }
    }
}
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
            List<PaqueteInsumo> paqueteInsumos = compraInsumo.PaqueteInsumos ?? new List<PaqueteInsumo>();
            List<Paquete> paquetes = new();
            Insumo insumo = compraInsumo.Insumo ?? throw new NotFoundException("No hay Insumo");

            foreach (var item in paqueteInsumos)
            {
                if (item.Paquete != null)
                    paquetes.Add(item.Paquete);
            }

            // CONVERSION DE UNIDAD DE MEDIDA
            decimal pesoTotalCompra;
            string um = (compraInsumo.Um ?? "").Trim().ToUpper();
            decimal cantidadSolicitada = compraInsumo.CantidadSolicitada;
            if (um == "G" || um == "GR" || um == "GRAMO" || um == "GRAMOS")
            {
                pesoTotalCompra = cantidadSolicitada;
            }
            else if (um == "KG" || um == "KILO" || um == "KILOS" || um == "KILOGRAMO" || um == "KILOGRAMOS")
            {
                pesoTotalCompra = cantidadSolicitada * 1000;
            }
            else if (um == "L" || um == "LT" || um == "LITRO" || um == "LITROS" || um == "ML" || um == "MILLILITRO")
            {
                decimal densidad = insumo.Densidad ?? 1m;
                pesoTotalCompra = cantidadSolicitada * (densidad > 0 ? densidad : 1m);
            }
            else
            {
                pesoTotalCompra = cantidadSolicitada;
            }


            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;
            decimal pesoPaquetesActual = paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario);
            decimal pesoPaqueteNuevo = paqueteEntrante + pesoPaquetesActual;
            
            // if (pesoTotalCompra < pesoPaqueteNuevo) 
            //     throw new BadRequestException("Se ha pasado el límite del peso solicitado");

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
            CompraEmpaque compraEmpaque = await _context.CompraEmpaques
                .Include(i => i.PaqueteEmpaques!)
                .ThenInclude(p => p.Paquete)
                .FirstOrDefaultAsync(f => f.Id == req.IdCompraEmpaque) ?? throw new NotFoundException("No se encontró la compra Empaque");
            List<PaqueteEmpaque> paqueteEmpaques = compraEmpaque.PaqueteEmpaques ?? new List<PaqueteEmpaque>();
            List<Paquete> paquetes = new();
            foreach (var item in paqueteEmpaques)
            {
                if (item.Paquete != null)
                    paquetes.Add(item.Paquete);
            }

            decimal pesoTotalPaquete = paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario);
            decimal pesoTotalSolicitado = compraEmpaque.CantidadSolicitada;
            decimal paqueteEntrante = req.CantidadPaquete * req.PesoUnitario;

            // if (pesoTotalSolicitado < (paqueteEntrante + pesoTotalPaquete))
            //     throw new BadRequestException("Se ha pasado el límite de unidades solicitadas");

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

            StockEmpaque? stockEmpaques = await _context.StockEmpaques.Where(w => w.IdCompraEmpaque == compraEmpaque.Id && w.IdSede == req.IdSede).FirstOrDefaultAsync();
            if (stockEmpaques == null)
            {
                StockEmpaque stockEmpaque = new()
                {
                    IdCompraEmpaque = compraEmpaque.Id,
                    IdSede = req.IdSede,
                    UnidadMedida = compraEmpaque.Um,
                    StockDisponible = paqueteEntrante
                };
                _context.StockEmpaques.Add(stockEmpaque);
            }
            else
            {
                stockEmpaques.StockDisponible += paqueteEntrante;
            }
            await _context.SaveChangesAsync();

            return paquete.Id;

        }

        public async Task<string> EliminarPaquete(int idPaquete, int empaqueInsumo, int idSede)
        {
            Paquete paquete = await _context.Paquetes.FindAsync(idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            _context.Paquetes.Remove(paquete);
            decimal stockEliminar = paquete.CantidadPaquete * paquete.PesoUnitario;
            if (empaqueInsumo == 0)
            {
                CompraInsumos compraInsumo = await _context.PaqueteInsumos
                           .Where(w => w.IdPaquete == idPaquete)
                           .Select(s => s.CompraInsumo)
                           .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontró el compra insumo"); ;
            }
            else
            {
                CompraEmpaque compraEmpaque = await _context.PaqueteEmpaques
           .Where(w => w.IdPaquete == idPaquete)
           .Select(s => s.CompraEmpaques)
           .FirstOrDefaultAsync() ?? throw new NotFoundException("No se encontró el compra empaque"); ;
                StockEmpaque? stockEmpaque = await _context.StockEmpaques.Where(w => w.IdCompraEmpaque == compraEmpaque.Id && w.IdSede == idSede).FirstOrDefaultAsync();
                stockEmpaque.StockDisponible -= stockEliminar;
            }

            await _context.SaveChangesAsync();
            return "Se Elimino el paquete id " + idPaquete;
        }

        public async Task<string> ModificarPaqueteInsumo(int idSede, int idPaquete, PaqueteInsumoModificarReq req)
        {

            Paquete paquete = await _context.Paquetes
            .Include(i => i.PaqueteInsumos)
            .ThenInclude(th => th!.CompraInsumo)
            .Include(i => i.PaqueteInsumos)
            .ThenInclude(th => th!.CompraInsumo)
            .ThenInclude(th => th!.Insumo)
            .FirstOrDefaultAsync(f => f.Id == idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            PaqueteInsumo paqueteInsumo = paquete.PaqueteInsumos ?? throw new NotFoundException("No se encontró paquetes insumos");
            CompraInsumos compraInsumo = paqueteInsumo.CompraInsumo ?? throw new NotFoundException("No hay Compra Insumos");
            // Busca stock para el idSede dado; si no existe, busca cualquier stock de esa compra
            Insumo insumo = compraInsumo.Insumo ?? throw new NotFoundException("No hay Insumo");

            //CONVERSION A GRAMOS
            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            decimal paquetePesoEntrante = req.CantidadPaquete * req.PesoUnitario;

            //ACTUALIZAR PAQUETE
            PaqueteMapper.ModificarPaqueteInsumo(req, paquete);

            await _context.SaveChangesAsync();

            return "Modificacion Exitosa";
        }
        public async Task<string> ModificarPaqueteEmpaque(int idSede, int idPaquete, PaqueteEmpaqueModificarReq req)
        {

            Paquete? paquete = await _context.Paquetes
            .Include(i => i.PaqueteEmpaques)
            .ThenInclude(th => th!.CompraEmpaques)
            .ThenInclude(th => th!.StockEmpaques)
            .FirstOrDefaultAsync(f => f.Id == idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            PaqueteEmpaque paqueteEmpaques = paquete.PaqueteEmpaques ?? throw new NotFoundException("No se encontró paquetes empaques");
            CompraEmpaque CompraEmpaque = paqueteEmpaques.CompraEmpaques ?? throw new NotFoundException("No hay Compra empaque");

            // Busca stock para el idSede dado; si no existe, busca cualquier stock de esa compra
            StockEmpaque? stockEmpaque = paqueteEmpaques.CompraEmpaques.StockEmpaques.FirstOrDefault(f => f.IdSede == idSede)
                ?? paqueteEmpaques.CompraEmpaques.StockEmpaques.FirstOrDefault();

            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            decimal paquetePesoEntrante = req.CantidadPaquete * req.PesoUnitario;

            PaqueteMapper.ModificarPaqueteEmpaque(req, paquete);

            // Actualizar stock solo si existe el registro
            if (stockEmpaque != null)
            {
                stockEmpaque.StockDisponible = stockEmpaque.StockDisponible - paquetePesoActual + paquetePesoEntrante;
            }

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
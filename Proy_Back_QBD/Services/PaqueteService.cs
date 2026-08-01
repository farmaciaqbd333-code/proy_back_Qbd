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
            decimal pesoPaquetesActual = paquetes.Sum(s => s.CantidadPaquete * s.PesoUnitario);
            decimal pesoPaqueteNuevo = paqueteEntrante + pesoPaquetesActual;
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
            StockInsumo? stockInsumos = await _context.StockInsumos.Where(w => w.IdCompraInsumo == compraInsumo.Id && w.IdSede == req.IdSede).FirstOrDefaultAsync();
            if (stockInsumos == null)
            {
                StockInsumo stockInsumo = new()
                {
                    IdCompraInsumo = compraInsumo.Id,
                    IdSede = req.IdSede,
                    StockDisponible = paqueteEntrante
                };
                _context.StockInsumos.Add(stockInsumo);
            }
            else
            {
                stockInsumos.StockDisponible += paqueteEntrante;
            }

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

            StockEmpaque? stockInsumos = await _context.StockEmpaques.Where(w => w.IdCompraEmpaque == compraEmpaque.Id && w.IdSede == req.IdSede).FirstOrDefaultAsync();
            if (stockInsumos == null)
            {
                StockEmpaque stockEmpaque = new()
                {
                    IdCompraEmpaque = compraEmpaque.Id,
                    IdSede = req.IdSede,
                    StockDisponible = paqueteEntrante
                };
                _context.StockEmpaques.Add(stockEmpaque);
            }
            else
            {
                stockInsumos.StockDisponible += paqueteEntrante;
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
                StockInsumo? stockInsumo = await _context.StockInsumos.Where(w => w.IdCompraInsumo == compraInsumo.Id && w.IdSede == idSede).FirstOrDefaultAsync();
                stockInsumo.StockDisponible -= stockEliminar;
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
            .ThenInclude(th => th!.StockInsumos)
            .Include(i => i.PaqueteInsumos)
            .ThenInclude(th => th!.CompraInsumo)
            .ThenInclude(th => th!.Insumo)
            .FirstOrDefaultAsync(f => f.Id == idPaquete) ?? throw new NotFoundException("No se encontró el paquete");
            PaqueteInsumo paqueteInsumo = paquete.PaqueteInsumos ?? throw new NotFoundException("No se encontró paquetes insumos");
            CompraInsumos compraInsumo = paqueteInsumo.CompraInsumo ?? throw new NotFoundException("No hay Compra Insumos");
            // Busca stock para el idSede dado; si no existe, busca cualquier stock de esa compra
            StockInsumo? stockInsumo = paqueteInsumo.CompraInsumo.StockInsumos.FirstOrDefault(w => w.IdSede == idSede)
                ?? paqueteInsumo.CompraInsumo.StockInsumos.FirstOrDefault();
            Insumo insumo = compraInsumo.Insumo ?? throw new NotFoundException("No hay Insumo");

            //CONVERSION A GRAMOS
            decimal paquetePesoActual = paquete.CantidadPaquete * paquete.PesoUnitario;
            decimal paquetePesoEntrante = req.CantidadPaquete * req.PesoUnitario;

            //ACTUALIZAR PAQUETE
            PaqueteMapper.ModificarPaqueteInsumo(req, paquete);

            // Actualizar stock solo si existe el registro
            if (stockInsumo != null)
            {
                stockInsumo.StockDisponible = stockInsumo.StockDisponible - paquetePesoActual + paquetePesoEntrante;
            }

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
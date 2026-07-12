using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;

namespace Proy_back_QBD.Services
{
    public class ProdTermService : IProdTermService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public ProdTermService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ProdTerm?> Actualizar(int id, int sedeId, ProdTermUpdateReq request)
        {
            ProdTerm? prodTerm = await _context.ProdTerms
            .Where(p => p.Id == id && p.SedeId == sedeId)
            .FirstOrDefaultAsync();
            if (prodTerm == null)
            {
                return null;
            }

            _mapper.Map(request, prodTerm);

            await _context.SaveChangesAsync();

            Pedido? pedido = await _context.Pedidos
            .Include(i => i.ProdTerms)
            .FirstOrDefaultAsync(fod => fod.Id == prodTerm.PedidoId && fod.SedeId == prodTerm.SedeId);

            if (pedido == null)
            {
                return null;
            }
            List<ProdTerm>? prodTerms = pedido.ProdTerms;
            if (prodTerms == null)
            {
                return null;
            }

            decimal costoReq = 0;
            costoReq = request.Costo * request.Cantidad;
            decimal costoForm = 0;
            costoForm = prodTerm.Costo * prodTerm.Cantidad;
            decimal diferencia = Math.Abs(costoReq - costoForm);

            if (costoReq != costoForm)
            {
                if (costoReq > costoForm)
                {
                    pedido.Total -= diferencia;
                    pedido.Saldo -= diferencia;
                }
                else if (costoReq < costoForm)
                {
                    pedido.Total += diferencia;
                    pedido.Saldo += diferencia;
                }
            }

            await _context.SaveChangesAsync();

            return prodTerm;
        }

        public async Task<ProdTerm?> Crear(ProdTermCreateReq request)
        {
            ProdTerm prodTerm = _mapper.Map<ProdTerm>(request);
            prodTerm.ModificadorId = prodTerm.CreadorId;
            prodTerm.Estado = "TERMINADO";

            await _context.ProdTerms.AddAsync(prodTerm);
            await _context.SaveChangesAsync();

            Pedido? pedido = await _context.Pedidos
            .Include(i => i.ProdTerms)
            .FirstOrDefaultAsync(fod => fod.Id == request.PedidoId && fod.SedeId == request.SedeId);

            if (pedido == null)
            {
                return null;
            }
            bool b = await _context.Formulas.AnyAsync(fod => fod.PedidoId == pedido.Id);
            if (b == true)
            {
                pedido.Estado = "PENDIENTE";
            }
            else
            {
                pedido.Estado = "PT";
            }

            pedido.Total += prodTerm.Costo * prodTerm.Cantidad;
            pedido.Saldo += prodTerm.Costo * prodTerm.Cantidad;

            await _context.SaveChangesAsync();

            return prodTerm;
        }

        public async Task<ProdTerm?> Eliminar(int id, int sedeId)
        {
            ProdTerm? prodTerm = await _context.ProdTerms
           .FirstOrDefaultAsync(a => a.Id == id && a.SedeId == sedeId);
            if (prodTerm == null)
            {
                return null;
            }
            _context.ProdTerms.Remove(prodTerm);
            await _context.SaveChangesAsync();

            Pedido? pedido = await _context.Pedidos
                        .FirstOrDefaultAsync(fod => fod.Id == prodTerm.PedidoId && fod.SedeId == sedeId);

            if (pedido == null)
            {
                return null;
            }

            bool b = await _context.Formulas.AnyAsync(fod => fod.PedidoId == pedido.Id);
            bool b2 = await _context.ProdTerms.AnyAsync(fod => fod.PedidoId == pedido.Id);

            if (b == true)
            {
                pedido.Estado = "PENDIENTE";
            }

            if (b == false && b2 == true)
            {
                pedido.Estado = "PT";
            }

            pedido.Total -= prodTerm.Costo * prodTerm.Cantidad;
            pedido.Saldo = pedido.Total - pedido.Adelanto;
            await _context.SaveChangesAsync();
            return prodTerm;
        }

    }
}
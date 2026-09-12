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
                .Include(p => p.Pedido)
                .FirstOrDefaultAsync(p => p.Id == id && p.SedeId == sedeId);

            if (prodTerm == null)
            {
                return null;
            }

            Pedido? pedido = prodTerm.Pedido;
            if (pedido == null && prodTerm.PedidoId.HasValue)
            {
                pedido = await _context.Pedidos
                    .FirstOrDefaultAsync(fod => fod.Id == prodTerm.PedidoId.Value && fod.SedeId == prodTerm.SedeId);
            }

            // Descontar del total y saldo el costo anterior antes de modificar
            if (pedido != null)
            {
                pedido.Total -= prodTerm.Costo * prodTerm.Cantidad;
                pedido.Saldo -= prodTerm.Costo * prodTerm.Cantidad;
            }

            // Traspaso directo sin AutoMapper
            prodTerm.Costo = request.Costo;
            prodTerm.Cantidad = request.Cantidad;
            prodTerm.ProductoId = request.ProductoId;
            prodTerm.ZonaAplicacion = request.ZonaAplicacion;
            prodTerm.Diagnostico = request.Diagnostico;
            if (!string.IsNullOrEmpty(request.Estado))
            {
                prodTerm.Estado = request.Estado;
            }
            prodTerm.ModificadorId = request.ModificadorId;

            // Sumar el nuevo costo al total y recalcular saldo del pedido
            if (pedido != null)
            {
                pedido.Total += prodTerm.Costo * prodTerm.Cantidad;
                pedido.Saldo = pedido.Total - pedido.Adelanto;
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
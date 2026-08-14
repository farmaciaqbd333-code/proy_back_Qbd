using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;
using proy_back_Qbd.Repositories.Interfaces;
using System.Threading.Tasks;

namespace proy_back_Qbd.Repositories
{
    public class RecepcionRepository : IRecepcionRepository
    {
        private readonly ApiContext _context;

        public RecepcionRepository(ApiContext context)
        {
            _context = context;
        }

        public async Task<NotaSalida?> GetNotaSalidaByIdAsync(int id)
        {
            return await _context.NotaSalidas
                .Include(n => n.NotaSalidaInsumos!)
                    .ThenInclude(i => i.PaqueteNotaSalidaInsumos)
                .Include(n => n.NotaSalidaEmpaques!)
                    .ThenInclude(e => e.PaqueteNotaSalidaEmpaques)
                .Include(n => n.NotaSalidaEconomatos!)
                    .ThenInclude(ec => ec.PaqueteNotaSalidaEconomatos)
                .Include(n => n.NotaSalidaProductos!)
                    .ThenInclude(p => p.PaqueteNotaSalidaProductos)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task GuardarCambiosAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

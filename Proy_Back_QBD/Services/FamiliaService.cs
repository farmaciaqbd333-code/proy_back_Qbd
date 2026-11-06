using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class FamiliaService : IFamiliaService
    {
        private readonly ApiContext _context;

        public FamiliaService(ApiContext context)
        {
            _context = context;
        }

        public Task<List<Familia>> Listar() =>
            _context.Familias.OrderBy(f => f.Nombre).ToListAsync();

        public async Task<Familia> Obtener(int id) =>
            await _context.Familias.FindAsync(id) ?? throw new NotFoundException("Familia no encontrada.");

        public async Task<Familia> Crear(Familia familia)
        {
            familia.FechaCreacion = DateTime.Now;
            _context.Familias.Add(familia);
            await _context.SaveChangesAsync();
            return familia;
        }

        public async Task Actualizar(int id, Familia familia)
        {
            if (id != familia.Id) throw new BadRequestException("El id no coincide con el de la familia.");

            _context.Entry(familia).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Familias.AnyAsync(e => e.Id == id)) throw new NotFoundException("Familia no encontrada.");
                throw;
            }
        }

        public async Task Eliminar(int id)
        {
            var familia = await Obtener(id);
            _context.Familias.Remove(familia);
            await _context.SaveChangesAsync();
        }
    }
}

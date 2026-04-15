using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class FamiliaController : ControllerBase
    {
        private readonly ApiContext _context;

        public FamiliaController(ApiContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Familia>>> GetFamilias()
        {
            return await _context.Familias
                .OrderBy(f => f.Nombre)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Familia>> GetFamilia(int id)
        {
            var familia = await _context.Familias.FindAsync(id);
            if (familia == null) return NotFound();
            return familia;
        }

        [HttpPost]
        public async Task<ActionResult<Familia>> PostFamilia(Familia familia)
        {
            familia.FechaCreacion = System.DateTime.Now;
            _context.Familias.Add(familia);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFamilia), new { id = familia.Id }, familia);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutFamilia(int id, Familia familia)
        {
            if (id != familia.Id) return BadRequest();
            _context.Entry(familia).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Familias.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamilia(int id)
        {
            var familia = await _context.Familias.FindAsync(id);
            if (familia == null) return NotFound();

            // Verificar si hay órdenes que usan esta familia antes de borrar
            var tieneOrdenes = await _context.OrdenCompras.AnyAsync(o => o.IdFamilia == id);
            if (tieneOrdenes) return BadRequest("No se puede eliminar la familia porque tiene órdenes de compra asociadas.");

            _context.Familias.Remove(familia);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

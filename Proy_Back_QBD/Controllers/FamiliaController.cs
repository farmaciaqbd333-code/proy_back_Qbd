using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class FamiliaController : ControllerBase
    {
        private readonly IFamiliaService _familias;

        public FamiliaController(IFamiliaService familias)
        {
            _familias = familias;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Familia>>> GetFamilias() =>
            await _familias.Listar();

        [HttpGet("{id}")]
        public async Task<ActionResult<Familia>> GetFamilia(int id) =>
            await _familias.Obtener(id);

        [HttpPost]
        public async Task<ActionResult<Familia>> PostFamilia(Familia familia)
        {
            var creada = await _familias.Crear(familia);
            return CreatedAtAction(nameof(GetFamilia), new { id = creada.Id }, creada);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutFamilia(int id, Familia familia)
        {
            await _familias.Actualizar(id, familia);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamilia(int id)
        {
            await _familias.Eliminar(id);
            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;

namespace proy_back_Qbd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedorController : Controller
    {
        private readonly IProveedorService _proveedores;

        public ProveedorController(IProveedorService proveedores)
        {
            _proveedores = proveedores;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProveedorDto>>> GetAll() =>
            Ok(await _proveedores.Listar());

        [HttpGet("{id}")]
        public async Task<ActionResult<ProveedorDto>> GetById(int id) =>
            Ok(await _proveedores.Obtener(id));

        [HttpPost]
        public async Task<ActionResult<ProveedorDto>> Create([FromBody] ProveedorCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var creado = await _proveedores.Crear(dto);
            return CreatedAtAction(nameof(GetById), new { id = creado.IdProveedor }, creado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProveedorUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _proveedores.Actualizar(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _proveedores.Eliminar(id);
            return NoContent();
        }
    }
}

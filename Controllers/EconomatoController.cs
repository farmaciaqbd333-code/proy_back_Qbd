using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using proy_back_Qbd.Models;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers
{
    [ApiController]
    [Route("api/economato")]
    public class EconomatoController : ControllerBase
    {
        private readonly IEconomatoService _economatoService;

        public EconomatoController(IEconomatoService economatoService)
        {
            _economatoService = economatoService;
        }

        [HttpGet]
        [SwaggerResponse(200, "Obtención exitosa", typeof(List<EconomatoRes>))]
        public async Task<IActionResult> Obtener()
        {
            var response = await _economatoService.Obtener();
            return Ok(response ?? new List<EconomatoRes>());
        }

        [HttpGet("{id}")]
        [SwaggerResponse(200, "Obtención exitosa", typeof(EconomatoRes))]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var response = await _economatoService.ObtenerById(id);
            if (response == null)
            {
                return NotFound("Economato no encontrado");
            }
            return Ok(response);
        }

        [HttpPost]
        [SwaggerResponse(201, "Creación exitosa", typeof(Economato))]
        public async Task<IActionResult> Crear([FromBody] EconomatoReq request)
        {
            var response = await _economatoService.Crear(request);
            if (response == null)
            {
                return BadRequest("Error al crear el economato");
            }
            return CreatedAtAction(nameof(ObtenerPorId), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        [SwaggerResponse(200, "Actualización exitosa", typeof(Economato))]
        public async Task<IActionResult> Actualizar(int id, [FromBody] EconomatoReq request)
        {
            var response = await _economatoService.Actualizar(id, request);
            if (response == null)
            {
                return NotFound("Economato no encontrado");
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [SwaggerResponse(204, "Eliminación exitosa")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var response = await _economatoService.Eliminar(id);
            if (!response)
            {
                return NotFound("Economato no encontrado");
            }
            return NoContent();
        }
    }
}

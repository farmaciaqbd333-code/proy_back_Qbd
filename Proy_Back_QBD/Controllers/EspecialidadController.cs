using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Proy_back_QBD.Dto.Auxiliares;
using Proy_back_QBD.Services.Interfaces;

namespace Proy_back_QBD.Controllers
{
    [ApiController]
    [Route("api/especialidad")]
    public class EspecialidadController : Controller
    {
        private readonly ILogger<EspecialidadController> _logger;
        private readonly IEspecialidadService _especialidadService;

        public EspecialidadController(ILogger<EspecialidadController> logger, IEspecialidadService especialidadService)
        {
            _logger = logger;
            _especialidadService = especialidadService;
        }

        // Crear una nueva especialidad
        [HttpPost("crear")]
        public async Task<IActionResult> Crear([FromBody] EspecialidadCreateReq request)
        {
            if (request == null)
            {
                return BadRequest("La solicitud no es válida.");
            }

            try
            {
                var result = await _especialidadService.Crear(request);
                if (result == null)
                {
                    return BadRequest("Error al crear la especialidad.");
                }

                return CreatedAtAction(nameof(ObtenerById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la especialidad.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        // Actualizar una especialidad
        [HttpPut("actualizar/{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] EspecialidadUpdateReq request)
        {
            if (request == null)
            {
                return BadRequest("La solicitud no es válida.");
            }

            try
            {
                var result = await _especialidadService.Actualizar(id, request);
                if (result == null)
                {
                    return NotFound($"Especialidad con ID {id} no encontrada.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la especialidad.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        // Eliminar una especialidad
        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var especialidadEliminada = await _especialidadService.Eliminar(id);
                if (especialidadEliminada == null)
                {
                    return NotFound($"Especialidad con ID {id} no encontrada.");
                }

                return NoContent();  // 204 No Content
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la especialidad.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        // Obtener todas las especialidades
        [HttpGet("obtener")]
        public async Task<IActionResult> Obtener()
        {
            try
            {
                var especialidades = await _especialidadService.Obtener();
                if (especialidades == null || especialidades.Count == 0)
                {
                    return NoContent();  // 204 No Content
                }

                return Ok(especialidades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las especialidades.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }
        
        // Obtener especialidad por ID
        [HttpGet("obtener/{id}")]
        public async Task<IActionResult> ObtenerById(int id)
        {
            try
            {
                var especialidad = await _especialidadService.ObtenerById(id);
                if (especialidad == null)
                {
                    return NotFound($"Especialidad con ID {id} no encontrada.");
                }

                return Ok(especialidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la especialidad.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }
    }
}
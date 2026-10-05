using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers
{
    [ApiController]
    [Route("api/cotizacion")]
    public class CotizacionController : ControllerBase
    {
        private readonly ICotizacionService _cotizacionService;

        public CotizacionController(ICotizacionService cotizacionService)
        {
            _cotizacionService = cotizacionService;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Listar cotizaciones con filtros")]
        public async Task<IActionResult> Listar(
            [FromQuery] int? sedeId,
            [FromQuery] DateOnly? fechaInicio,
            [FromQuery] DateOnly? fechaFin,
            [FromQuery] string? search,
            [FromQuery] string? estado)
        {
            var resultado = await _cotizacionService.Listar(sedeId, fechaInicio, fechaFin, search, estado);
            return Ok(resultado);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Obtener cotización por ID con sus detalles")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var cotizacion = await _cotizacionService.ObtenerPorId(id);
            if (cotizacion == null)
            {
                return NotFound(new { message = $"Cotización con ID {id} no encontrada" });
            }
            return Ok(cotizacion);
        }

        [HttpGet("siguiente-numero")]
        [SwaggerOperation(Summary = "Obtener correlativo para la siguiente cotización")]
        public async Task<IActionResult> ObtenerSiguienteNumero([FromQuery] int? sedeId)
        {
            var siguiente = await _cotizacionService.ObtenerSiguienteNumero(sedeId);
            return Ok(new { numero = siguiente });
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Crear una nueva cotización con sus detalles")]
        public async Task<IActionResult> Crear(
            [FromBody] CotizacionCreateReq request,
            [FromHeader(Name = "x-user-id")] int? headerUserId)
        {
            if (request == null)
            {
                return BadRequest("El cuerpo de la solicitud no puede estar vacío");
            }

            int usuarioId = headerUserId ?? 1;
            var nuevaCotizacion = await _cotizacionService.Crear(request, usuarioId);

            if (nuevaCotizacion == null)
            {
                return BadRequest("No se pudo crear la cotización");
            }

            return CreatedAtAction(nameof(ObtenerPorId), new { id = nuevaCotizacion.Id }, nuevaCotizacion);
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Actualizar cotización existente")]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] CotizacionUpdateReq request,
            [FromHeader(Name = "x-user-id")] int? headerUserId)
        {
            if (request == null)
            {
                return BadRequest("El cuerpo de la solicitud no puede estar vacío");
            }

            int usuarioId = headerUserId ?? 1;
            var actualizada = await _cotizacionService.Actualizar(id, request, usuarioId);

            if (actualizada == null)
            {
                return NotFound(new { message = $"Cotización con ID {id} no encontrada" });
            }

            return Ok(actualizada);
        }

        [HttpPatch("{id}/estado")]
        [SwaggerOperation(Summary = "Cambiar estado de una cotización (Pendiente, Aprobado, Facturado, Anulado)")]
        public async Task<IActionResult> CambiarEstado(
            int id,
            [FromBody] CotizacionEstadoReq request,
            [FromHeader(Name = "x-user-id")] int? headerUserId)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Estado))
            {
                return BadRequest("Debe especificar un estado válido");
            }

            int usuarioId = headerUserId ?? 1;
            var success = await _cotizacionService.CambiarEstado(id, request.Estado, usuarioId);

            if (!success)
            {
                return NotFound(new { message = $"Cotización con ID {id} no encontrada" });
            }

            return Ok(new { message = "Estado actualizado correctamente", estado = request.Estado });
        }

        [HttpPatch("{id}/cpe")]
        [SwaggerOperation(Summary = "Asignar CPE (Boleta/Factura) relacionado a una cotización")]
        public async Task<IActionResult> AsignarCpe(
            int id,
            [FromBody] CotizacionCpeReq request,
            [FromHeader(Name = "x-user-id")] int? headerUserId)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CpeRelacionado))
            {
                return BadRequest("Debe especificar el CPE relacionado");
            }

            int usuarioId = headerUserId ?? 1;
            var success = await _cotizacionService.AsignarCpe(id, request.CpeRelacionado, usuarioId);

            if (!success)
            {
                return NotFound(new { message = $"Cotización con ID {id} no encontrada" });
            }

            return Ok(new { message = "CPE asignado correctamente", cpe = request.CpeRelacionado });
        }

        [HttpPatch("{id}/enviado")]
        [SwaggerOperation(Summary = "Marcar si la cotización fue enviada al cliente")]
        public async Task<IActionResult> MarcarEnviado(
            int id,
            [FromQuery] bool enviado,
            [FromHeader(Name = "x-user-id")] int? headerUserId)
        {
            int usuarioId = headerUserId ?? 1;
            var success = await _cotizacionService.MarcarEnviado(id, enviado, usuarioId);

            if (!success)
            {
                return NotFound(new { message = $"Cotización con ID {id} no encontrada" });
            }

            return Ok(new { message = "Estado de envío actualizado", enviado });
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Eliminar cotización")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var success = await _cotizacionService.Eliminar(id);
            if (!success)
            {
                return NotFound(new { message = $"Cotización con ID {id} no encontrada" });
            }

            return Ok(new { message = "Cotización eliminada correctamente" });
        }
    }
}

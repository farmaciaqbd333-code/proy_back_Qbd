using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using proy_back_Qbd.Services.Interfaces.INotaSalidaService;
using Swashbuckle.AspNetCore.Annotations;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[Controller]")]
    public class NotaSalidaController : Controller
    {
        private readonly INotaSalidaService _serviceNotaSalida;
        public NotaSalidaController(INotaSalidaService _serviceNotaSalida)
        {
            this._serviceNotaSalida = _serviceNotaSalida;
        }

        [HttpPost]
        public async Task<ActionResult<int>> CrearNotaSalida([FromBody] CreateReq request)
        {
            try
            {
                int num = await _serviceNotaSalida.Crear(request);
                return Ok(num);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<int>> ActualizarNotaSalida(int id, [FromBody] CreateReq request)
        {
            try
            {
                await _serviceNotaSalida.Actualizar(id, request);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }
        /// <summary>
        /// Listar Nota Salida por sede
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<List<NotaSalidaListaRes>>> Listar(int id)
        {
            var lista = await _serviceNotaSalida.ObtenerLista(id);
            return Ok(lista);
        }

        /// <summary>
        /// Listar Detalle de Nota Salida
        /// </summary>
        [HttpGet("detalle/{id}")]
        public async Task<ActionResult<List<NotaSalidaDetalleRes>>> GetDetalle(int id)
        {
            try
            {
                var detalles = await _serviceNotaSalida.ObtenerDetalles(id);
                return Ok(detalles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
            }
        }

        /// <summary>
        /// Listar articulos por familia, sede y idRegistro
        /// </summary>
        [HttpGet("articulo/{idRegistro}")]
        public async Task<ActionResult<List<RegistrosListaRes>>> Get(int idRegistro, string familia, int idSede)
        {
            var lista = await _serviceNotaSalida.ObtenerDatosXRegistro(idRegistro, familia, idSede);
            return Ok(lista);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Eliminar(int id)
        {
            await _serviceNotaSalida.Eliminar(id);
            return Ok();
        }

        [HttpPost("familias")]
        [ProducesResponseType(typeof(List<RegistrosRes>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<RegistrosRes>>> ListarRegistrosPorFamilia([FromBody] ObtenerRegistroReq request)
        {
            try
            {
                var resultado = await _serviceNotaSalida.ObtenerRegistrosXFamilia(request);
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("confirmar")]
        public async Task<IActionResult> Confirmar([FromBody] ConfirmarReq request)
        {
            try
            {
                await _serviceNotaSalida.Confirmar(request);

                return Ok(new
                {
                    mensaje = "Nota de salida confirmada correctamente."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
        }

    }
}
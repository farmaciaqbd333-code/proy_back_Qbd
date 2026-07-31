using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
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
        public async Task<ActionResult<int>> CrearNotaSalida([FromBody] NotaSalidaCreateReq request)
        {
            int num = await _serviceNotaSalida.CrearAsync(request);
            return Ok(num);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult<int>> ActualizarNotaSalida(int id, [FromBody] NotaSalidaCreateReq request)
        {
            await _serviceNotaSalida.ActualizarAsync(id, request);
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<List<NotaSalidaListaRes>>> Get(int id)
        {
            var lista = await _serviceNotaSalida.ObtenerListaAsync(id);
            return Ok(lista);
        }
        [HttpGet("articulo/{id}")]
        public async Task<ActionResult<List<RegistrosListaRes>>> Get(int id, string familia)
        {
            var lista = await _serviceNotaSalida.ObtenerRegistros(id, familia);
            return Ok(lista);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Eliminar(int id)
        {
            await _serviceNotaSalida.EliminarAsync(id);
            return Ok();
        }

    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;

namespace proy_back_Qbd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AjusteController : ControllerBase
    {
        private readonly IAjusteService _ajusteService;
        public AjusteController(IAjusteService _ajusteService)
        {
            this._ajusteService = _ajusteService;
        }

        [HttpGet("lista")]
        public async Task<IActionResult> ListarAjustes(string familia, int idSede)
        {
            List<TablaAjustesRes> response = await _ajusteService.ListaAjustes(familia, idSede);
            return Ok(response);
        }

        [HttpGet("detalle")]
        public async Task<IActionResult> DetalleAjustes([FromQuery] int registroId, [FromQuery] string familia, [FromQuery] int? idInsumo, [FromQuery] int? idSede)
        {
            List<DetalleAjusteRes> response = await _ajusteService.DetalleAjuste(registroId, familia, idInsumo, idSede);
            return Ok(response);
        }

        [HttpPost("registrar-ajuste")]
        public async Task<IActionResult> RegistrarAjuste([FromBody] CrearAjusteReq? request)
        {
            if (request == null) return BadRequest(new { message = "El cuerpo de la solicitud no puede ser nulo o tiene formato JSON inválido." });
            if (request.ListaAjustes == null || request.ListaAjustes.Count == 0) return BadRequest(new { message = "La lista de ajustes está vacía." });
            await _ajusteService.RegistrarAjuste(request);
            return Ok(new
            {
                mensaje = "Ajuste registrado correctamente."
            });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;
using proy_back_Qbd.Models.Kardex;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;
using Proy_back_QBD.Security;
using proy_back_Qbd.Services.Ajuste;

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

        /// <summary>
        /// Ajuste masivo de materia prima (MP, no PI): deja cada insumo con <paramref name="objetivo"/> en cada sede
        /// (lote vigente más reciente = objetivo, demás lotes vigentes = 0; vencidos no se tocan).
        /// SIN <paramref name="confirmar"/> solo SIMULA y devuelve lo que cambiaría. Para aplicar, confirmar=AJUSTAR-MP.
        /// </summary>
        [HttpPost("mp-masivo")]
        [Authorize(Roles = Roles.SoloAdmin)] // Solo el administrador
        public async Task<IActionResult> AjusteMasivoMp(
            [FromServices] AjusteMasivoMpService servicio,
            [FromQuery] decimal objetivo = 1000,
            [FromQuery] int? idSede = null,
            [FromQuery] bool detalle = false,
            [FromQuery] int idCreador = 1,
            [FromQuery] string? confirmar = null)
        {
            bool aplicar = string.Equals(confirmar, AjusteMasivoMpService.CONFIRMACION, StringComparison.Ordinal);
            var res = await servicio.Ejecutar(objetivo, aplicar, idCreador, idSede, detalle);
            return Ok(res);
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
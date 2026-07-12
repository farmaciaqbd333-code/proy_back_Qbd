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
    [Route("api/[controller]")]
    public class AjusteController : Controller
    {
        private readonly IAjusteService _ajusteService;
        public AjusteController(IAjusteService _ajusteService)
        {
            this._ajusteService = _ajusteService;
        }

        [HttpGet("lista")]
        public async Task<IActionResult> ListarAjustes(string familia)
        {
            List<TablaAjustesRes> response = await _ajusteService.ListaAjustes(familia);
            return Ok(response);
        }
        [HttpGet("detalle")]
        public async Task<IActionResult> DetalleAjustes(int registroId, string familia)
        {
            List<DetalleAjusteRes> response = await _ajusteService.DetalleAjuste(registroId, familia);
            return Ok(response);
        }
        [HttpPost("registrar-ajuste")]
        public async Task<IActionResult> RegistrarAjuste([FromBody] CrearAjusteReq request)
        {
            await _ajusteService.RegistrarAjuste(request);
            return Ok(new
            {
                mensaje = "Ajuste registrado correctamente."
            });
        }
    }
}
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
        public async Task<ActionResult<MesonDetalleRes>> ObtenerOrdenCompra(NotaSalidaCreateReq request)
        {
            int num = await _serviceNotaSalida.CrearAsync(request);
            return Ok(num);
        }

    }
}
using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Dto.Recepcion;
using proy_back_Qbd.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecepcionController : ControllerBase
    {
        private readonly IRecepcionService _recepcionService;

        public RecepcionController(IRecepcionService recepcionService)
        {
            _recepcionService = recepcionService;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarRecepcion(int id, [FromBody] RecepcionNotaSalidaReq request)
        {
            try
            {
                await _recepcionService.ActualizarRecepcionAsync(id, request);
                return Ok(new { mensaje = "Recepción actualizada correctamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { mensaje = ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }
}

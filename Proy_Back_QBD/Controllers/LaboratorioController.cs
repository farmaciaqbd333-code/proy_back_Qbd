using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Proy_back_QBD.Dto.Productos;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers
{
    [ApiController]
    [Route("api/laboratorio")]
    public class LaboratorioController : Controller
    {

        private readonly ILogger<LaboratorioController> _logger;
        private readonly ILaboratorioService _labService;

        public LaboratorioController(ILogger<LaboratorioController> logger, ILaboratorioService labService)
        {
            _logger = logger;
            _labService = labService;
        }

        [HttpGet("lab/{sedeId}")]
        [SwaggerResponse(200, "Lista exitosa", typeof(List<PedidoLab>))]
        public async Task<IActionResult> ListarPedidosLab(int sedeId)
        {
            List<PedidoLab> response = await _labService.ListaLab(sedeId);
            if (response == null)
            {
                return BadRequest();

            }
            return Ok(response);
        }

        [HttpGet("{cod}/{sedeId}")]
        [SwaggerResponse(200, "Lista exitosa", typeof(LabFindPedIdRes))]
        public async Task<IActionResult> ListarPedidoLab(string cod, int sedeId)
        {
            LabFindPedIdRes? response = await _labService.ObtenerByCod(cod, sedeId);

            if (response == null)
            {
                return NotFound("");
            }

            return Ok(response);
        }

        [HttpPost]
        [SwaggerResponse(200, "Registro exitosa", typeof(string))]
        public async Task<IActionResult> CrearLaboratorio(FormLabIns request)
        {
            if (request.Lab == null || request.Ins == null)
            {
                return BadRequest("Faltan Datos");
            }
            string? response = await _labService.RegistrarLabIns(request);

            if (response == null)
            {
                return NotFound("");
            }

            return Ok(response);
        }
        [HttpPut("elaborado/{IdLab}/{sedeId}")]
        [SwaggerResponse(200, "Registro exitosa", typeof(string))]
        public async Task<IActionResult> ActualizarEntregado(int IdLab, int sedeId, [FromBody] int IdElaborado)
        {
            string response = await _labService.EditarElaborado(IdLab, sedeId, IdElaborado);
            
            return Ok(response);
        }

    }
}
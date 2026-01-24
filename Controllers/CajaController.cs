using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Response;
using Proy_back_QBD.Response.Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/caja")]
public class CajaController : ControllerBase
{

    private readonly ICajaService _cajaService;

    public CajaController(ICajaService cajaService)
    {
        _cajaService = cajaService;
    }

    [HttpPost("{sedeId}")]
    [SwaggerResponse(200, "Creacion exitosa", typeof(List<CajaFindAllRes?>))]
    public async Task<IActionResult> ObtenerCajas(CajaFindAllReq request, int sedeId)
    {
        CajaFindAllRes? response = await _cajaService.Obtener(request, sedeId);

        return Ok(response);
    }

}

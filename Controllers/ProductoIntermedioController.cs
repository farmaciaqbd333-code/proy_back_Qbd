using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Interface;

namespace Proy_back_QBD.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoIntermedioController : ControllerBase
    {
        public readonly IProductoIntermedioService _productoIntermedioService;
        public ProductoIntermedioController(IProductoIntermedioService _productoIntermedioService)
        {
            this._productoIntermedioService = _productoIntermedioService;
        }

        [HttpGet("tabla")]
        public async Task<IActionResult> Get()
        {
            return Ok(await _productoIntermedioService.ListaProductoIntermedio());
        }


    }
}
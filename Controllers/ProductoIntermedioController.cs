using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Services;

namespace Proy_back_QBD.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoIntermedioController : ControllerBase
    {
        public readonly ProductoIntermedioService _productoIntermedioService;
        public ProductoIntermedioController(ProductoIntermedioService _productoIntermedioService)
        {
            this._productoIntermedioService = _productoIntermedioService;
        }

        [HttpGet("tabla")]
        public IActionResult Get()
        {
            return Ok(_productoIntermedioService.ListaProductoIntermedio());
        }

        // GET: api/ejemplo/5
        [HttpGet("consumo/{id}")]
        public IActionResult GetById(int id)
        {
            return Ok(new { Id = id });
        }

        // POST: api/ejemplo
        [HttpPost]
        public IActionResult Create([FromBody] object model)
        {
            return CreatedAtAction(nameof(GetById), new { id = 1 }, model);
        }

        // PUT: api/ejemplo/5
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] object model)
        {
            return NoContent();
        }

        // DELETE: api/ejemplo/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return NoContent();
        }
    }
}
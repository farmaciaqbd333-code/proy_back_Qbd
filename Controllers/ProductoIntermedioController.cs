using Microsoft.AspNetCore.Mvc;

namespace Proy_back_QBD.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductoIntermedioController : ControllerBase
    {
        public ProductoIntermedioController()
        {
        }

        // GET: api/ejemplo
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hola desde el controlador");
        }

        // GET: api/ejemplo/5
        [HttpGet("{id}")]
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
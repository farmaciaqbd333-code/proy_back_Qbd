using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class FabricanteController : ControllerBase
    {
        private readonly IFabricanteService _fabricantes;

        public FabricanteController(IFabricanteService fabricantes)
        {
            _fabricantes = fabricantes;
        }

        /// <summary>
        /// Listar todos los fabricantes
        /// </summary>
        [HttpGet]
        [SwaggerResponse(200, "Lista obtenida con éxito", typeof(IEnumerable<FabricanteResponseDto>))]
        public async Task<ActionResult<IEnumerable<FabricanteResponseDto>>> GetFabricantes() =>
            Ok(await _fabricantes.Listar());

        /// <summary>
        /// Obtener un fabricante por su ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(200, "Fabricante obtenido con éxito", typeof(FabricanteResponseDto))]
        [SwaggerResponse(404, "Fabricante no encontrado")]
        public async Task<ActionResult<FabricanteResponseDto>> GetFabricante(int id) =>
            Ok(await _fabricantes.Obtener(id));

        /// <summary>
        /// Listar fabricantes asociados a un proveedor específico
        /// </summary>
        [HttpGet("proveedor/{idProveedor}")]
        [SwaggerResponse(200, "Fabricantes del proveedor obtenidos con éxito", typeof(IEnumerable<FabricanteResponseDto>))]
        public async Task<ActionResult<IEnumerable<FabricanteResponseDto>>> GetFabricantesPorProveedor(int idProveedor) =>
            Ok(await _fabricantes.ListarPorProveedor(idProveedor));

        /// <summary>
        /// Crear un nuevo fabricante y opcionalmente asociarlo a proveedores
        /// </summary>
        [HttpPost]
        [SwaggerResponse(201, "Fabricante creado con éxito", typeof(FabricanteResponseDto))]
        public async Task<ActionResult<FabricanteResponseDto>> PostFabricante(FabricanteCreateDto dto)
        {
            var creado = await _fabricantes.Crear(dto);
            return CreatedAtAction(nameof(GetFabricante), new { id = creado.Id }, creado);
        }

        /// <summary>
        /// Actualizar un fabricante y opcionalmente sincronizar sus proveedores
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerResponse(204, "Fabricante actualizado con éxito")]
        [SwaggerResponse(404, "Fabricante no encontrado")]
        public async Task<IActionResult> PutFabricante(int id, FabricanteUpdateDto dto)
        {
            await _fabricantes.Actualizar(id, dto);
            return NoContent();
        }

        /// <summary>
        /// Eliminar un fabricante
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse(204, "Fabricante eliminado con éxito")]
        [SwaggerResponse(404, "Fabricante no encontrado")]
        public async Task<IActionResult> DeleteFabricante(int id)
        {
            await _fabricantes.Eliminar(id);
            return NoContent();
        }

        /// <summary>
        /// Asociar un fabricante a un proveedor
        /// </summary>
        [HttpPost("asociar")]
        [SwaggerResponse(200, "Asociación establecida con éxito")]
        [SwaggerResponse(404, "Fabricante o proveedor no encontrado")]
        public async Task<IActionResult> AsociarProveedor(AsociarProveedorDto dto)
        {
            await _fabricantes.AsociarProveedor(dto);
            return Ok(new { message = "Fabricante asociado al proveedor con éxito" });
        }

        /// <summary>
        /// Desasociar un fabricante de un proveedor
        /// </summary>
        [HttpPost("desasociar")]
        [SwaggerResponse(200, "Asociación eliminada con éxito")]
        [SwaggerResponse(404, "Fabricante o proveedor no encontrado")]
        public async Task<IActionResult> DesasociarProveedor(AsociarProveedorDto dto)
        {
            await _fabricantes.DesasociarProveedor(dto);
            return Ok(new { message = "Fabricante desasociado del proveedor con éxito" });
        }
    }
}

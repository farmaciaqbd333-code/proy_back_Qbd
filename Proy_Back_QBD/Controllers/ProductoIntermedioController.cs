using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Interface;
using Proy_back_QBD.Request;

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
        public async Task<IActionResult> ListaPrincipal()
        {
            return Ok(await _productoIntermedioService.ListaProductoIntermedio());
        }
        [HttpGet("consumo/{idProductoIntermedio}")]
        public async Task<IActionResult> DetalleConsumo(int idProductoIntermedio)
        {
            return Ok(await _productoIntermedioService.DetalleConsumo(idProductoIntermedio));
        }
        [HttpPost]
        public async Task<IActionResult> Crear(CrearProductoIntermedioReq request)
        {
            return Ok(await _productoIntermedioService.CrearProductoIntermedio(request));
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, ActualizarProductoIntermedioReq request)
        {
            return Ok(await _productoIntermedioService.ActualizarProductoIntermedio(id, request));
        }
        [HttpGet("maestros/{tipoUso}")]
        public async Task<IActionResult> Maestros(string tipoUso)
        {
            return Ok(await _productoIntermedioService.ListaMaestraPI(tipoUso));
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            return Ok(await _productoIntermedioService.EliminarProductoIntermedio(id));
        }
    }
}
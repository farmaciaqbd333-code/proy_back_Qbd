using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Security;

namespace Proy_back_QBD.Controllers
{
    /// <summary>
    /// Lista de precios (Excel PREC. AV). La lógica vive en <see cref="IListaPrecioService"/>.
    /// </summary>
    [ApiController]
    [Route("api/lista-precios")]
    public class ListaPrecioController : ControllerBase
    {
        private readonly IListaPrecioService _listaPrecios;

        public ListaPrecioController(IListaPrecioService listaPrecios)
        {
            _listaPrecios = listaPrecios;
        }

        /// <summary>
        /// Lista los productos activos. Filtros opcionales: texto (código de cualquier lista, producto,
        /// IFAS o indicaciones) y especialidad.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? search, [FromQuery] string? especialidad, [FromQuery] int? take, [FromQuery] string? categoria) =>
            Ok(await _listaPrecios.Listar(search, especialidad, take, categoria));

        /// <summary>Especialidades usadas en la lista, con su cantidad de productos.</summary>
        [HttpGet("especialidades")]
        public async Task<IActionResult> Especialidades() =>
            Ok(await _listaPrecios.Especialidades());

        /// <summary>Crea un producto (solo ADMIN).</summary>
        [HttpPost]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Crear([FromBody] ListaPrecioReq request) =>
            Ok(await _listaPrecios.Crear(request));

        /// <summary>Modifica un producto (solo ADMIN).</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ListaPrecioReq request) =>
            Ok(await _listaPrecios.Actualizar(id, request));

        /// <summary>Quita un producto de la lista (se desactiva, no se borra). Solo ADMIN.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Eliminar(int id)
        {
            await _listaPrecios.Eliminar(id);
            return Ok(new { message = "Producto eliminado." });
        }

        /// <summary>
        /// Importación masiva desde Excel (solo ADMIN). Cada fila se busca por Id; si no tiene Id,
        /// por el código de la lista A. Si no existe, se crea.
        /// </summary>
        [HttpPost("importar")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> Importar([FromBody] ListaPrecioImportReq request) =>
            Ok(await _listaPrecios.Importar(request));
    }
}

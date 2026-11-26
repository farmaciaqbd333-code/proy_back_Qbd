using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Dto.ListaPrecios;
using Proy_back_QBD.Security;
using Proy_back_QBD.Services;

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
        private readonly IProductoMedicoService _productoMedicos;

        public ListaPrecioController(IListaPrecioService listaPrecios, IProductoMedicoService productoMedicos)
        {
            _listaPrecios = listaPrecios;
            _productoMedicos = productoMedicos;
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

        // ===================== Médicos asignados a cada producto =====================

        /// <summary>Todos los médicos del directorio (todas las sedes, sin filtrar por la sede actual).</summary>
        [HttpGet("medicos/directorio")]
        public async Task<IActionResult> MedicosAsignables() =>
            Ok(await _productoMedicos.MedicosAsignables());

        /// <summary>Todas las asignaciones producto ↔ médico (para mostrarlas en la tabla).</summary>
        [HttpGet("medicos/asignaciones")]
        public async Task<IActionResult> Asignaciones() =>
            Ok(await _productoMedicos.Asignaciones());

        /// <summary>Médicos asignados a un producto.</summary>
        [HttpGet("{id:int}/medicos")]
        public async Task<IActionResult> MedicosDelProducto(int id) =>
            Ok(await _productoMedicos.DelProducto(id));

        /// <summary>Asigna el producto a uno o varios médicos (se suman a los que ya tenga). Solo ADMIN.</summary>
        [HttpPost("{id:int}/medicos")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> AsignarMedicos(int id, [FromBody] ProductoMedicosAsignarReq request) =>
            Ok(await _productoMedicos.Asignar(id, request?.MedicoIds ?? new(), JwtTokens.UsuarioId(User)));

        /// <summary>Quita la asignación del producto a un médico. Solo ADMIN.</summary>
        [HttpDelete("{id:int}/medicos/{medicoId:int}")]
        [Authorize(Roles = Roles.SoloAdmin)]
        public async Task<IActionResult> QuitarMedico(int id, int medicoId) =>
            Ok(await _productoMedicos.Quitar(id, medicoId));
    }
}

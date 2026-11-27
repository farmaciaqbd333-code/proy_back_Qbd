using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.AppMedico;
using Proy_back_QBD.Security;
using Proy_back_QBD.Services.AppMedico;

namespace Proy_back_QBD.Controllers
{
    /// <summary>
    /// API de la app móvil de médicos. Todas las rutas (menos el login) exigen un token de MÉDICO
    /// (esquema <see cref="AppMedicoTokens.Esquema"/>); el token del personal no sirve aquí y viceversa.
    /// </summary>
    [ApiController]
    [Route("api/app-medico")]
    [Authorize(Policy = AppMedicoTokens.Politica)]
    public class AppMedicoController : ControllerBase
    {
        private readonly IAppMedicoService _service;

        public AppMedicoController(IAppMedicoService service)
        {
            _service = service;
        }

        private SesionMedico Sesion => new(AppMedicoTokens.AccesoId(User), AppMedicoTokens.Version(User));

        /// <summary>Login del médico con CMP y contraseña. Devuelve el token de la app.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] MedicoLoginReq request) =>
            Ok(await _service.Login(request));

        /// <summary>Datos del médico de la sesión.</summary>
        [HttpGet("perfil")]
        public async Task<IActionResult> Perfil() => Ok(await _service.Perfil(Sesion));

        /// <summary>Cambia la contraseña. Devuelve un token nuevo (las otras sesiones se cierran).</summary>
        [HttpPost("cambiar-contrasena")]
        public async Task<IActionResult> CambiarContrasena([FromBody] MedicoCambiarContrasenaReq request) =>
            Ok(await _service.CambiarContrasena(Sesion, request));

        /// <summary>
        /// Catálogo (Lista de Precios) con el precio de la Lista A. Filtros opcionales y paginación.
        /// Cada ítem dice si es favorito del médico y si QBD se lo asignó (<c>asignado</c>);
        /// <c>soloAsignados=true</c> deja solo los asignados.
        /// </summary>
        [HttpGet("catalogo")]
        public async Task<IActionResult> Catalogo([FromQuery] string? search, [FromQuery] string? especialidad,
            [FromQuery] string? categoria, [FromQuery] bool soloFavoritos = false,
            [FromQuery] int pagina = 1, [FromQuery] int tamano = 50, [FromQuery] bool soloAsignados = false) =>
            Ok(await _service.Catalogo(Sesion, search, especialidad, categoria, soloFavoritos, pagina, tamano, soloAsignados));

        /// <summary>"Mis productos": los productos que QBD le asignó al médico desde Lista de Precios.</summary>
        [HttpGet("mis-productos")]
        public async Task<IActionResult> MisProductos([FromQuery] string? search, [FromQuery] string? especialidad,
            [FromQuery] string? categoria, [FromQuery] int pagina = 1, [FromQuery] int tamano = 200) =>
            Ok(await _service.Catalogo(Sesion, search, especialidad, categoria, false, pagina, tamano, soloAsignados: true));

        /// <summary>Códigos de especialidad usados en el catálogo (DERM, PED, GO...).</summary>
        [HttpGet("catalogo/especialidades")]
        public async Task<IActionResult> Especialidades() => Ok(await _service.EspecialidadesCatalogo(Sesion));

        [HttpPut("favoritos/{productoId:int}")]
        public async Task<IActionResult> AgregarFavorito(int productoId)
        {
            await _service.MarcarFavorito(Sesion, productoId, true);
            return Ok(new { productoId, favorito = true });
        }

        [HttpDelete("favoritos/{productoId:int}")]
        public async Task<IActionResult> QuitarFavorito(int productoId)
        {
            await _service.MarcarFavorito(Sesion, productoId, false);
            return Ok(new { productoId, favorito = false });
        }

        /// <summary>Sedes a las que el médico puede enviar pedidos (primero en las que está registrado).</summary>
        [HttpGet("sedes")]
        public async Task<IActionResult> Sedes() => Ok(await _service.Sedes(Sesion));

        /// <summary>Envía un pedido. Los precios se toman de la Lista A en el servidor.</summary>
        [HttpPost("pedidos")]
        public async Task<IActionResult> CrearPedido([FromBody] PedidoMedicoCreateReq request) =>
            Ok(await _service.CrearPedido(Sesion, request));

        /// <summary>Historial de pedidos del médico (los últimos 200), con su estado.</summary>
        [HttpGet("pedidos")]
        public async Task<IActionResult> MisPedidos() => Ok(await _service.MisPedidos(Sesion));

        [HttpGet("pedidos/{id:int}")]
        public async Task<IActionResult> MiPedido(int id) => Ok(await _service.MiPedido(Sesion, id));

        /// <summary>Anula un pedido que QBD aún no ha recibido (estado PENDIENTE).</summary>
        [HttpPost("pedidos/{id:int}/anular")]
        public async Task<IActionResult> Anular(int id) => Ok(await _service.AnularMiPedido(Sesion, id));
    }

    /// <summary>
    /// Administración de la app de médicos desde el sistema web (token del personal).
    /// </summary>
    [ApiController]
    [Route("api/app-medico/admin")]
    public class AppMedicoAdminController : ControllerBase
    {
        private readonly IAppMedicoService _service;

        public AppMedicoAdminController(IAppMedicoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Asigna o restablece la contraseña de la app de un médico del directorio (Admin, Supervisor, S1).
        /// Sin contraseña en el cuerpo, genera una temporal y la devuelve UNA sola vez.
        /// </summary>
        [HttpPut("acceso/{medicoId:int}")]
        [Authorize(Roles = Roles.GestionUsuarios)]
        public async Task<IActionResult> AsignarAcceso(int medicoId, [FromBody] MedicoAccesoAsignarReq? request) =>
            Ok(await _service.AsignarAcceso(medicoId, request?.Contrasena, JwtTokens.UsuarioId(User)));

        /// <summary>Activa o desactiva el acceso a la app (desactivar cierra sus sesiones).</summary>
        [HttpPatch("acceso/{medicoId:int}/activo")]
        [Authorize(Roles = Roles.GestionUsuarios)]
        public async Task<IActionResult> CambiarActivo(int medicoId, [FromQuery] bool valor)
        {
            await _service.CambiarActivoAcceso(medicoId, valor, JwtTokens.UsuarioId(User));
            return Ok(new { message = valor ? "Acceso activado." : "Acceso desactivado." });
        }

        /// <summary>CMP (normalizado) de los médicos que tienen acceso a la app.</summary>
        [HttpGet("accesos")]
        public async Task<IActionResult> Accesos() => Ok(await _service.Accesos());

        /// <summary>Pedidos hechos desde la app. Filtros: sede, estado y últimos N días.</summary>
        [HttpGet("pedidos")]
        public async Task<IActionResult> Pedidos([FromQuery] int? sedeId, [FromQuery] string? estado, [FromQuery] int? dias) =>
            Ok(await _service.ListarPedidos(sedeId, estado, dias));

        /// <summary>Cantidad de pedidos PENDIENTES (para el aviso en el menú).</summary>
        [HttpGet("pedidos/pendientes")]
        public async Task<IActionResult> Pendientes([FromQuery] int? sedeId) =>
            Ok(new { pendientes = await _service.ContarPendientes(sedeId) });

        /// <summary>Cambia el estado del pedido y, opcionalmente, deja un mensaje para el médico.</summary>
        [HttpPatch("pedidos/{id:int}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] PedidoMedicoEstadoReq request) =>
            Ok(await _service.CambiarEstado(id, request, JwtTokens.UsuarioId(User)));

        /// <summary>
        /// Para convertir el pedido en un pedido de Gestión de Pedidos: devuelve el paciente (el propio médico)
        /// en la sede indicada, registrándolo si todavía no es paciente de esa sede.
        /// </summary>
        [HttpPost("pedidos/{id:int}/paciente")]
        public async Task<IActionResult> PacienteDelMedico(int id, [FromQuery] int sedeId) =>
            Ok(await _service.PacienteDelMedico(id, sedeId, JwtTokens.UsuarioId(User)));
    }
}

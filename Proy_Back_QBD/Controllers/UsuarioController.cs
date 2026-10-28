using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Security;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/usuario")]
public class UsuarioController : ControllerBase
{
    /// <summary>Roles que pueden gestionar usuarios: Admin, Supervisor y S1.</summary>
    private const string RolesGestionUsuarios = Roles.GestionUsuarios;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public UsuarioController(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>Inicia sesión y devuelve los datos del usuario con su token JWT.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerResponse(200, "Operación exitosa", typeof(UsuarioLoginDataRes))]
    public async Task<IActionResult> ValidarCredenciales([FromBody] UsuarioLoginReq request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrEmpty(request.Contrasena))
        {
            return BadRequest(new { message = "Ingrese código y contraseña." });
        }

        UsuarioLoginDataRes? usuario = await _userService.ValidarLogin(request.Codigo, request.Contrasena);
        if (usuario == null)
        {
            return Unauthorized(new { message = "Código o contraseña incorrectos." });
        }

        var (token, expira) = _tokenService.Generar(usuario);

        // Mismos campos de siempre + token (el frontend existente sigue funcionando)
        var respuesta = JsonSerializer.SerializeToNode(usuario, Json)!.AsObject();
        respuesta["token"] = token;
        respuesta["expira"] = expira.ToString("o");
        return Ok(respuesta);
    }

    /// <summary>
    /// Renueva el token de la sesión actual (12 horas más). El frontend lo llama mientras
    /// el usuario trabaja, para que la sesión no se corte de golpe.
    /// </summary>
    [HttpPost("renovar-token")]
    public IActionResult RenovarToken()
    {
        if (JwtTokens.UsuarioId(User) <= 0)
        {
            return Unauthorized(new { message = "Sesión no válida." });
        }
        var (token, expira) = _tokenService.Renovar(User);
        return Ok(new { token, expira = expira.ToString("o") });
    }

    [Authorize(Roles = RolesGestionUsuarios)]
    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa")]
    public async Task<IActionResult> CrearUsuario([FromBody] UsuarioCreateReq request)
    {
        Usuario? usuario = await _userService.Crear(request);
        if (usuario == null)
        {
            return BadRequest(new { message = "No se pudo crear el usuario." });
        }
        return Ok(Resumen(usuario));
    }

    [Authorize(Roles = RolesGestionUsuarios)]
    [HttpDelete("{id}")]
    [SwaggerResponse(200, "Operación exitosa")]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        Usuario? usuario = await _userService.Eliminar(id);
        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }
        return Ok(Resumen(usuario));
    }

    [Authorize(Roles = RolesGestionUsuarios)]
    [HttpPut("{id}")]
    [SwaggerResponse(200, "Operación exitosa")]
    public async Task<IActionResult> ActualizarUsuario(int id, UsuarioUpdateReq request)
    {
        Usuario? usuario = await _userService.Actualizar(id, request);
        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }
        return Ok(Resumen(usuario));
    }

    [Authorize(Roles = RolesGestionUsuarios)]
    [HttpGet]
    [SwaggerResponse(200, "Operación exitosa", typeof(UsuarioListaRes))]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        List<UsuarioListaRes>? usuario = await _userService.Listar();
        return Ok(usuario);
    }

    /// <summary>Datos de un usuario: el propio usuario o quien gestiona usuarios.</summary>
    [HttpGet("{id}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(UsuarioByIdRes))]
    public async Task<IActionResult> ObtenerUsuariosPorId(int id)
    {
        var esPropio = JwtTokens.UsuarioId(User) == id;
        var puedeGestionar = Roles.PuedeGestionarUsuarios(JwtTokens.Rol(User));
        if (!esPropio && !puedeGestionar)
        {
            return Forbid();
        }
        UsuarioByIdRes? usuario = await _userService.ObtenerById(id);
        if (usuario != null && !puedeGestionar)
        {
            usuario.Contrasena = string.Empty; // solo Admin, Supervisor y S1 ven contraseñas
        }
        return Ok(usuario);
    }

    [HttpGet("lista/{sedeId}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(List<AutorizadoEla>))]
    public async Task<IActionResult> ListarUsuario(int sedeId)
    {
        List<AutorizadoEla>? usuario = await _userService.Lista2(sedeId);
        return Ok(usuario);
    }

    [Authorize(Roles = RolesGestionUsuarios)]
    [HttpPut("estado/{id}")]
    [SwaggerResponse(200, "Operación exitosa")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool estado)
    {
        Usuario? usuario = await _userService.CambiarEstado(id, estado);
        if (usuario == null)
        {
            return NotFound("Usuario no encontrado.");
        }
        return Ok(Resumen(usuario));
    }

    /// <summary>Respuesta sin datos sensibles (nunca la contraseña).</summary>
    private static object Resumen(Usuario u) => new
    {
        id = u.Id,
        codigo = u.Codigo,
        tipoId = u.TipoId,
        sedeId = u.SedeId,
        personaId = u.PersonaId,
        idGeneral = u.IdGeneral,
        estado = u.Estado,
    };
}

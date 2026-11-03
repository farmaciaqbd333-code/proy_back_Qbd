using Proy_back_QBD.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Services;
using Swashbuckle.AspNetCore.Annotations;
using AutoMapper;
using System.Diagnostics;
using Proy_back_QBD.Models;
using Microsoft.AspNetCore.Authorization;
using Proy_back_QBD.Security;
using System.Text.Json;
namespace Proy_back_QBD.Controllers;

[ApiController]
[Route("api/asistencia")]
public class AsistenciaController : ControllerBase
{

    private const int TamanoMaximoCalculo = 64 * 1024;

    private readonly IAsistenciaService _asistenciaService;

    public AsistenciaController(IAsistenciaService userService)
    {
        _asistenciaService = userService;
    }

    [HttpPost]
    [SwaggerResponse(200, "Operación exitosa", typeof(Asistencia))]
    public async Task<IActionResult> CrearAsistencia([FromBody] AsistenciaCreateReq request)
    {
        if (request == null)
        {
            return BadRequest("Datos de asistencia no proporcionados");
        }
        request.RegistradoPorId = JwtTokens.UsuarioId(User);
        if (!PuedeVerTodos())
        {
            // Un trabajador puede marcar por otra persona de su misma sede
            int? sede = await _asistenciaService.ObtenerSedeParaMarcar(request.CreadorId);
            if (sede == null)
            {
                return BadRequest("Colaborador no disponible para marcar");
            }
            if (!EsPropioOMismaSede(request.CreadorId, sede.Value))
            {
                return Forbid();
            }
            request.SedeId = sede;
        }
        Asistencia? response = await _asistenciaService.Crear(request);
        if (response == null)
        {
            return BadRequest("Error al crear la asistencia");
        }
        return Ok(response);
    }
    
    /// <summary>
    /// Obtiene las asistencias agrupadas por día dentro de una fecha para un usuario específico.
    /// </summary>
    [HttpPost("{id}/{sedeId}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(AsistenciaByIdRes))]
    public async Task<IActionResult> ObtenerAsistenciaByCodigo(int id, [FromBody] AsistenciaByIdReq request, int sedeId)
    {
        if (request == null || id == null)
        {
            return BadRequest("Código de asistencia o id no proporcionado");
        }
        if (!PuedeVerTodos())
        {
            // Un trabajador consulta a un colaborador activo de su misma sede
            int? sede = await _asistenciaService.ObtenerSedeParaMarcar(id);
            if (sede == null)
            {
                return BadRequest("Colaborador no disponible");
            }
            if (!EsPropioOMismaSede(id, sede.Value))
            {
                return Forbid();
            }
            sedeId = sede.Value;
        }

        AsistenciaByIdRes? response = await _asistenciaService.ObtenerPorId(id, request.Año, request.Mes, sedeId);
        return Ok(response);
    }

    /// <summary>Cálculo de pago guardado de un colaborador en un mes. Contiene el sueldo: solo Admin.</summary>
    [Authorize(Roles = Roles.SoloAdmin)]
    [HttpGet("calculo-pago/{usuarioId}/{anio}/{mes}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(CalculoPagoRes))]
    public async Task<IActionResult> ObtenerCalculoPago(int usuarioId, int anio, int mes)
    {
        if (!PeriodoValido(anio, mes))
        {
            return BadRequest("Período inválido");
        }
        return Ok(await _asistenciaService.ObtenerCalculoPago(usuarioId, anio, mes));
    }

    /// <summary>Crea o reemplaza el cálculo de pago de un colaborador en un mes. Solo Admin.</summary>
    [Authorize(Roles = Roles.SoloAdmin)]
    [HttpPut("calculo-pago/{usuarioId}/{anio}/{mes}")]
    [SwaggerResponse(200, "Operación exitosa", typeof(CalculoPagoRes))]
    public async Task<IActionResult> GuardarCalculoPago(int usuarioId, int anio, int mes, [FromBody] CalculoPagoReq request)
    {
        if (!PeriodoValido(anio, mes))
        {
            return BadRequest("Período inválido");
        }
        if (request.PagoFinal < 0)
        {
            return BadRequest("El pago final no puede ser negativo");
        }
        if (!DatosCalculoValidos(request.Datos))
        {
            return BadRequest("Datos del cálculo inválidos");
        }

        CalculoPagoRes? response = await _asistenciaService.GuardarCalculoPago(
            usuarioId, anio, mes, request, JwtTokens.UsuarioId(User));
        if (response == null)
        {
            return NotFound("Colaborador no encontrado");
        }
        return Ok(response);
    }

    private bool PuedeVerTodos() => Roles.PuedeGestionarUsuarios(JwtTokens.Rol(User));

    private bool EsPropioOMismaSede(int? usuarioId, int sedeUsuario) =>
        usuarioId == JwtTokens.UsuarioId(User) || sedeUsuario == JwtTokens.Sede(User);

    public static bool PeriodoValido(int anio, int mes) => anio is >= 2000 and <= 2100 && mes is >= 1 and <= 12;

    public static bool DatosCalculoValidos(string? datos)
    {
        if (string.IsNullOrWhiteSpace(datos) || datos.Length > TamanoMaximoCalculo)
        {
            return false;
        }
        try
        {
            using JsonDocument documento = JsonDocument.Parse(datos);
            return documento.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

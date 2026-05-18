using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class FabricanteController : ControllerBase
    {
        private readonly ApiContext _context;

        public FabricanteController(ApiContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Listar todos los fabricantes
        /// </summary>
        [HttpGet]
        [SwaggerResponse(200, "Lista obtenida con éxito", typeof(IEnumerable<FabricanteResponseDto>))]
        public async Task<ActionResult<IEnumerable<FabricanteResponseDto>>> GetFabricantes()
        {
            var fabricantes = await _context.Fabricantes
                .Include(f => f.Proveedores)
                .OrderBy(f => f.Nombre)
                .ToListAsync();

            var response = fabricantes.Select(f => new FabricanteResponseDto
            {
                Id = f.Id,
                Codigo = f.Codigo,
                Nombre = f.Nombre,
                Pais = f.Pais,
                Descripcion = f.Descripcion,
                FechaCreacion = f.FechaCreacion,
                IdCreador = f.IdCreador,
                FechaModificacion = f.FechaModificacion,
                IdModificador = f.IdModificador,
                Proveedores = f.Proveedores?.Select(p => new FabricanteProveedorDto
                {
                    Id = p.Id,
                    Nombre = p.Datos,
                    NumeroProv = p.NumeroProv
                }).ToList()
            });

            return Ok(response);
        }

        /// <summary>
        /// Obtener un fabricante por su ID
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerResponse(200, "Fabricante obtenido con éxito", typeof(FabricanteResponseDto))]
        [SwaggerResponse(404, "Fabricante no encontrado")]
        public async Task<ActionResult<FabricanteResponseDto>> GetFabricante(int id)
        {
            var f = await _context.Fabricantes
                .Include(f => f.Proveedores)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (f == null) return NotFound(new { message = "Fabricante no encontrado" });

            var response = new FabricanteResponseDto
            {
                Id = f.Id,
                Codigo = f.Codigo,
                Nombre = f.Nombre,
                Pais = f.Pais,
                Descripcion = f.Descripcion,
                FechaCreacion = f.FechaCreacion,
                IdCreador = f.IdCreador,
                FechaModificacion = f.FechaModificacion,
                IdModificador = f.IdModificador,
                Proveedores = f.Proveedores?.Select(p => new FabricanteProveedorDto
                {
                    Id = p.Id,
                    Nombre = p.Datos,
                    NumeroProv = p.NumeroProv
                }).ToList()
            };

            return Ok(response);
        }

        /// <summary>
        /// Listar fabricantes asociados a un proveedor específico
        /// </summary>
        [HttpGet("proveedor/{idProveedor}")]
        [SwaggerResponse(200, "Fabricantes del proveedor obtenidos con éxito", typeof(IEnumerable<FabricanteResponseDto>))]
        public async Task<ActionResult<IEnumerable<FabricanteResponseDto>>> GetFabricantesPorProveedor(int idProveedor)
        {
            var proveedorExistente = await _context.Proveedores.AnyAsync(p => p.Id == idProveedor);
            if (!proveedorExistente) return NotFound(new { message = "Proveedor no encontrado" });

            var fabricantes = await _context.Fabricantes
                .Where(f => f.Proveedores != null && f.Proveedores.Any(p => p.Id == idProveedor))
                .OrderBy(f => f.Nombre)
                .ToListAsync();

            var response = fabricantes.Select(f => new FabricanteResponseDto
            {
                Id = f.Id,
                Codigo = f.Codigo,
                Nombre = f.Nombre,
                Pais = f.Pais,
                Descripcion = f.Descripcion,
                FechaCreacion = f.FechaCreacion,
                IdCreador = f.IdCreador,
                FechaModificacion = f.FechaModificacion,
                IdModificador = f.IdModificador
            });

            return Ok(response);
        }

        /// <summary>
        /// Crear un nuevo fabricante y opcionalmente asociarlo a proveedores
        /// </summary>
        [HttpPost]
        [SwaggerResponse(201, "Fabricante creado con éxito", typeof(FabricanteResponseDto))]
        public async Task<ActionResult<FabricanteResponseDto>> PostFabricante(FabricanteCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var fabricante = new Fabricante
                {
                    Codigo = dto.Codigo,
                    Nombre = dto.Nombre,
                    Pais = dto.Pais,
                    Descripcion = dto.Descripcion,
                    IdCreador = dto.IdCreador,
                    FechaCreacion = DateTime.Now,
                    Proveedores = new List<Proveedor>()
                };

                if (dto.IdProveedores != null && dto.IdProveedores.Any())
                {
                    var proveedores = await _context.Proveedores
                        .Where(p => dto.IdProveedores.Contains(p.Id))
                        .ToListAsync();
                    
                    foreach (var prov in proveedores)
                    {
                        fabricante.Proveedores.Add(prov);
                    }
                }

                _context.Fabricantes.Add(fabricante);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var response = new FabricanteResponseDto
                {
                    Id = fabricante.Id,
                    Codigo = fabricante.Codigo,
                    Nombre = fabricante.Nombre,
                    Pais = fabricante.Pais,
                    Descripcion = fabricante.Descripcion,
                    FechaCreacion = fabricante.FechaCreacion,
                    IdCreador = fabricante.IdCreador,
                    Proveedores = fabricante.Proveedores?.Select(p => new FabricanteProveedorDto
                    {
                        Id = p.Id,
                        Nombre = p.Datos,
                        NumeroProv = p.NumeroProv
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetFabricante), new { id = fabricante.Id }, response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error al crear el fabricante", error = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar un fabricante y opcionalmente sincronizar sus proveedores
        /// </summary>
        [HttpPut("{id}")]
        [SwaggerResponse(204, "Fabricante actualizado con éxito")]
        [SwaggerResponse(404, "Fabricante no encontrado")]
        public async Task<IActionResult> PutFabricante(int id, FabricanteUpdateDto dto)
        {
            var fabricante = await _context.Fabricantes
                .Include(f => f.Proveedores)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (fabricante == null) return NotFound(new { message = "Fabricante no encontrado" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                fabricante.Codigo = dto.Codigo;
                fabricante.Nombre = dto.Nombre;
                fabricante.Pais = dto.Pais;
                fabricante.Descripcion = dto.Descripcion;
                fabricante.IdModificador = dto.IdModificador;
                fabricante.FechaModificacion = DateTime.Now;

                if (dto.IdProveedores != null)
                {
                    // Limpiar asociaciones anteriores y añadir las nuevas
                    fabricante.Proveedores?.Clear();
                    
                    if (dto.IdProveedores.Any())
                    {
                        var nuevosProveedores = await _context.Proveedores
                            .Where(p => dto.IdProveedores.Contains(p.Id))
                            .ToListAsync();
                        
                        foreach (var prov in nuevosProveedores)
                        {
                            fabricante.Proveedores?.Add(prov);
                        }
                    }
                }

                _context.Entry(fabricante).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error al actualizar el fabricante", error = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar un fabricante
        /// </summary>
        [HttpDelete("{id}")]
        [SwaggerResponse(204, "Fabricante eliminado con éxito")]
        [SwaggerResponse(404, "Fabricante no encontrado")]
        public async Task<IActionResult> DeleteFabricante(int id)
        {
            var fabricante = await _context.Fabricantes.FindAsync(id);
            if (fabricante == null) return NotFound(new { message = "Fabricante no encontrado" });

            // Verificar si hay detalles de compra asociados a este fabricante antes de eliminar
            var tieneDetallesAsociados = await _context.DetalleCompras.AnyAsync(dc => dc.IdFabricante == id);
            if (tieneDetallesAsociados)
            {
                return BadRequest(new { message = "No se puede eliminar el fabricante porque está asociado a detalles de compras registradas." });
            }

            _context.Fabricantes.Remove(fabricante);
            await _context.SaveChangesAsync();

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
            var fabricante = await _context.Fabricantes
                .Include(f => f.Proveedores)
                .FirstOrDefaultAsync(f => f.Id == dto.IdFabricante);

            if (fabricante == null) return NotFound(new { message = "Fabricante no encontrado" });

            var proveedor = await _context.Proveedores.FindAsync(dto.IdProveedor);
            if (proveedor == null) return NotFound(new { message = "Proveedor no encontrado" });

            if (fabricante.Proveedores == null)
            {
                fabricante.Proveedores = new List<Proveedor>();
            }

            if (fabricante.Proveedores.Any(p => p.Id == dto.IdProveedor))
            {
                return BadRequest(new { message = "La relación ya existe" });
            }

            fabricante.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

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
            var fabricante = await _context.Fabricantes
                .Include(f => f.Proveedores)
                .FirstOrDefaultAsync(f => f.Id == dto.IdFabricante);

            if (fabricante == null) return NotFound(new { message = "Fabricante no encontrado" });

            var proveedor = fabricante.Proveedores?.FirstOrDefault(p => p.Id == dto.IdProveedor);
            if (proveedor == null) return NotFound(new { message = "Relación no encontrada entre el fabricante y el proveedor" });

            fabricante.Proveedores?.Remove(proveedor);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Fabricante desasociado del proveedor con éxito" });
        }
    }
}

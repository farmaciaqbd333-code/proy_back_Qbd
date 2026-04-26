using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedorController : Controller
    {
        private readonly ApiContext _context;

        public ProveedorController(ApiContext context)
        {
            _context = context;
        }

        // Obtener un proveedor por ID
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProveedorDto>>> GetAll()
        {
            var proveedores = await _context.Proveedores
                .Select(p => new ProveedorDto
                {
                    IdProveedor = p.IdProveedor,
                    CodigoProv = p.CodigoProv,
                    Datos = p.Datos,
                    Direccion = p.Direccion,
                    Telefono = p.Telefono,
                    Referencia = p.Referencia,
                    CodigoProvedor = p.CodigoProvedor,
                    FechaCreacion = p.FechaCreacion,
                    IdCreador = p.IdCreador
                })
                .ToListAsync();

            return Ok(proveedores);
        }

        // GET: api/proveedores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProveedorDto>> GetById(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
                return NotFound($"Proveedor con ID {id} no encontrado.");

            var dto = new ProveedorDto
            {
                IdProveedor = proveedor.IdProveedor,
                CodigoProv = proveedor.CodigoProv,
                Datos = proveedor.Datos,
                Direccion = proveedor.Direccion,
                Telefono = proveedor.Telefono,
                Referencia = proveedor.Referencia,
                CodigoProvedor = proveedor.CodigoProvedor,
                FechaCreacion = proveedor.FechaCreacion,
                IdCreador = proveedor.IdCreador
            };

            return Ok(dto);
        }

        // POST: api/proveedores
        [HttpPost]
        public async Task<ActionResult<ProveedorDto>> Create([FromBody] ProveedorCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var proveedor = new Proveedor
            {
                CodigoProv = dto.CodigoProv,
                Datos = dto.Datos,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Referencia = dto.Referencia,
                CodigoProvedor = dto.CodigoProvedor,
                IdCreador = dto.IdCreador
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            var responseDto = new ProveedorDto
            {
                IdProveedor = proveedor.IdProveedor,
                CodigoProv = proveedor.CodigoProv,
                Datos = proveedor.Datos,
                Direccion = proveedor.Direccion,
                Telefono = proveedor.Telefono,
                Referencia = proveedor.Referencia,
                CodigoProvedor = proveedor.CodigoProvedor,
                FechaCreacion = proveedor.FechaCreacion,
                IdCreador = proveedor.IdCreador
            };

            return CreatedAtAction(nameof(GetById), new { id = proveedor.IdProveedor }, responseDto);
        }

        // PUT: api/proveedores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProveedorUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
                return NotFound($"Proveedor con ID {id} no encontrado.");

            // Actualizamos los campos
            proveedor.CodigoProv = dto.CodigoProv;
            proveedor.Datos = dto.Datos;
            proveedor.Direccion = dto.Direccion;
            proveedor.Telefono = dto.Telefono;
            proveedor.Referencia = dto.Referencia;
            proveedor.CodigoProvedor = dto.CodigoProvedor;
            // NO actualizamos FechaCreacion ni IdCreador

            await _context.SaveChangesAsync();

            return NoContent(); // 204 No Content
        }

        // DELETE: api/proveedores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
                return NotFound($"Proveedor con ID {id} no encontrado.");

            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
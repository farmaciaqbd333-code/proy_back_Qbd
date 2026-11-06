using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class ProveedorService : IProveedorService
    {
        private readonly ApiContext _context;

        public ProveedorService(ApiContext context)
        {
            _context = context;
        }

        public Task<List<ProveedorDto>> Listar() =>
            _context.Proveedores
                .Select(p => new ProveedorDto
                {
                    IdProveedor = p.Id,
                    CodigoProv = p.NumeroProv,
                    Datos = p.Datos,
                    Direccion = p.Direccion,
                    Telefono = p.Telefono,
                    Referencia = p.Referencia,
                    CodigoProvedor = p.CodigoProvedor,
                    FechaCreacion = p.FechaCreacion,
                    IdCreador = p.IdCreador
                })
                .ToListAsync();

        public async Task<ProveedorDto> Obtener(int id) => ADto(await Buscar(id));

        public async Task<ProveedorDto> Crear(ProveedorCreateDto dto)
        {
            var proveedor = new Proveedor
            {
                NumeroProv = dto.CodigoProv,
                Datos = dto.Datos,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Referencia = dto.Referencia,
                CodigoProvedor = dto.CodigoProvedor,
                IdCreador = dto.IdCreador
            };

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            return ADto(proveedor);
        }

        public async Task Actualizar(int id, ProveedorUpdateDto dto)
        {
            var proveedor = await Buscar(id);

            proveedor.NumeroProv = dto.CodigoProv;
            proveedor.Datos = dto.Datos;
            proveedor.Direccion = dto.Direccion;
            proveedor.Telefono = dto.Telefono;
            proveedor.Referencia = dto.Referencia;
            proveedor.CodigoProvedor = dto.CodigoProvedor;
            // No se actualizan FechaCreacion ni IdCreador

            await _context.SaveChangesAsync();
        }

        public async Task Eliminar(int id)
        {
            var proveedor = await Buscar(id);
            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
        }

        private async Task<Proveedor> Buscar(int id) =>
            await _context.Proveedores.FindAsync(id)
            ?? throw new NotFoundException($"Proveedor con ID {id} no encontrado.");

        private static ProveedorDto ADto(Proveedor p) => new()
        {
            IdProveedor = p.Id,
            CodigoProv = p.NumeroProv,
            Datos = p.Datos,
            Direccion = p.Direccion,
            Telefono = p.Telefono,
            Referencia = p.Referencia,
            CodigoProvedor = p.CodigoProvedor,
            FechaCreacion = p.FechaCreacion,
            IdCreador = p.IdCreador
        };
    }
}

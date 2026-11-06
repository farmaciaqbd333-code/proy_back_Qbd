using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Services
{
    public class FabricanteService : IFabricanteService
    {
        private readonly ApiContext _context;

        public FabricanteService(ApiContext context)
        {
            _context = context;
        }

        public async Task<List<FabricanteResponseDto>> Listar()
        {
            var fabricantes = await _context.Fabricantes
                .Include(f => f.Proveedores)
                .OrderBy(f => f.Nombre)
                .ToListAsync();
            return fabricantes.Select(ADto).ToList();
        }

        public async Task<FabricanteResponseDto> Obtener(int id) =>
            ADto(await BuscarConProveedores(id));

        public async Task<List<FabricanteResponseDto>> ListarPorProveedor(int idProveedor)
        {
            if (!await _context.Proveedores.AnyAsync(p => p.Id == idProveedor))
                throw new NotFoundException("Proveedor no encontrado");

            var fabricantes = await _context.Fabricantes
                .Where(f => f.Proveedores!.Any(p => p.Id == idProveedor)) // sin "!= null": EF no puede traducirlo a SQL
                .OrderBy(f => f.Nombre)
                .ToListAsync();
            return fabricantes.Select(ADto).ToList();
        }

        public async Task<FabricanteResponseDto> Crear(FabricanteCreateDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var fabricante = new Fabricante
            {
                Codigo = dto.Codigo,
                Nombre = dto.Nombre,
                Pais = dto.Pais,
                Descripcion = dto.Descripcion,
                IdCreador = dto.IdCreador,
                FechaCreacion = DateTime.Now,
                Proveedores = await ProveedoresPorId(dto.IdProveedores)
            };

            _context.Fabricantes.Add(fabricante);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ADto(fabricante);
        }

        public async Task Actualizar(int id, FabricanteUpdateDto dto)
        {
            var fabricante = await BuscarConProveedores(id);

            await using var transaction = await _context.Database.BeginTransactionAsync();

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
                foreach (var prov in await ProveedoresPorId(dto.IdProveedores))
                {
                    fabricante.Proveedores?.Add(prov);
                }
            }

            _context.Entry(fabricante).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task Eliminar(int id)
        {
            var fabricante = await _context.Fabricantes.FindAsync(id)
                ?? throw new NotFoundException("Fabricante no encontrado");

            if (await _context.CompraInsumos.AnyAsync(dc => dc.IdFabricante == id))
                throw new BadRequestException("No se puede eliminar el fabricante porque está asociado a detalles de compras registradas.");

            _context.Fabricantes.Remove(fabricante);
            await _context.SaveChangesAsync();
        }

        public async Task AsociarProveedor(AsociarProveedorDto dto)
        {
            var fabricante = await BuscarConProveedores(dto.IdFabricante);

            var proveedor = await _context.Proveedores.FindAsync(dto.IdProveedor)
                ?? throw new NotFoundException("Proveedor no encontrado");

            fabricante.Proveedores ??= new List<Proveedor>();
            if (fabricante.Proveedores.Any(p => p.Id == dto.IdProveedor))
                throw new BadRequestException("La relación ya existe");

            fabricante.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
        }

        public async Task DesasociarProveedor(AsociarProveedorDto dto)
        {
            var fabricante = await BuscarConProveedores(dto.IdFabricante);

            var proveedor = fabricante.Proveedores?.FirstOrDefault(p => p.Id == dto.IdProveedor)
                ?? throw new NotFoundException("Relación no encontrada entre el fabricante y el proveedor");

            fabricante.Proveedores!.Remove(proveedor);
            await _context.SaveChangesAsync();
        }

        // ===================== Ayudas =====================

        private async Task<Fabricante> BuscarConProveedores(int id) =>
            await _context.Fabricantes
                .Include(f => f.Proveedores)
                .FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new NotFoundException("Fabricante no encontrado");

        private async Task<List<Proveedor>> ProveedoresPorId(List<int>? ids)
        {
            if (ids == null || ids.Count == 0) return new List<Proveedor>();
            return await _context.Proveedores.Where(p => ids.Contains(p.Id)).ToListAsync();
        }

        private static FabricanteResponseDto ADto(Fabricante f) => new()
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
    }
}

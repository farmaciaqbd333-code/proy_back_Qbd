using System;
using System.Collections.Generic;

namespace proy_back_Qbd.Models
{
    public class FabricanteCreateDto
    {
        public string? Codigo { get; set; }
        public required string Nombre { get; set; }
        public string? Pais { get; set; }
        public string? Descripcion { get; set; }
        public required int IdCreador { get; set; }
        
        // Opcional: Lista de IDs de proveedores a los que asociar al crear
        public List<int>? IdProveedores { get; set; }
    }

    public class FabricanteUpdateDto
    {
        public string? Codigo { get; set; }
        public required string Nombre { get; set; }
        public string? Pais { get; set; }
        public string? Descripcion { get; set; }
        public required int IdModificador { get; set; }
        
        // Opcional: Lista de IDs de proveedores a los que asociar al actualizar
        public List<int>? IdProveedores { get; set; }
    }

    public class FabricanteResponseDto
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }
        public required string Nombre { get; set; }
        public string? Pais { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int IdCreador { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? IdModificador { get; set; }
        
        public List<FabricanteProveedorDto>? Proveedores { get; set; }
    }

    public class FabricanteProveedorDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? NumeroProv { get; set; }
    }

    // DTO para la relación de asociación individual
    public class AsociarProveedorDto
    {
        public int IdProveedor { get; set; }
        public int IdFabricante { get; set; }
    }
}

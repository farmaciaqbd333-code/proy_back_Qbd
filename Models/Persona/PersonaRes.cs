using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Dto.Response
{
    public class PersonaRes
    {
        public int? Id { get; set; }
        public string? NombreCompleto { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string? Dni { get; set; }
        public string? Direccion { get; set; }
        public string? Sede { get; set; }
        public int? SedeId { get; set; }
        public string? Telefono { get; set; }
    }
    public class PersMedRes
    {
        public int? Id { get; set; }
        public string? NombreCompleto { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string? Sede { get; set; }
        public int? SedeId { get; set; }
        public string? Telefono { get; set; }
    }
    public class PersonaRes2
    {
        public int? Id { get; set; }
        public string? NombreCompleto { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public string? Dni { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
    } 
    public class PersMedRes2
    {
        public int? Id { get; set; }
        public string? NombreCompleto { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public int? SedeId { get; set; }
        public string? Telefono { get; set; }
    } 
}
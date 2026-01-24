using System.Text.Json.Serialization;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Response
{
    public class MedicoFindAllResponse
    {
        public int? Id { get; set; }
        public string? DesEspecialidad { get; set; }
        public int? EspecialidadId { get; set; }
        public string? NumeroEspecialidad { get; set; }
        public PersMedRes2? Persona { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Cmp { get; set; }
    }
    public class MedicoCreateResponse
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Medico? MedicoRes { get; set; }
    }
    public class MedicoUpdateResponse
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Medico? MedicoRes { get; set; }
    }

    public class MedicoFindIdResponse
    {
        public int? Id { get; set; }  // Puede ser nulo
        public int? EspecialidadId { get; set; }
        public string? NumeroEspecialidad { get; set; }
        public PersMedRes? PersonaFk { get; set; }  // Puede ser nulo
        public string? Cmp { get; set; }
    }
    
}
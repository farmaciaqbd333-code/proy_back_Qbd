using System.Text.Json.Serialization;
using Proy_back_QBD.Models;

namespace Proy_back_QBD.Dto.Response
{
    public class PacienteFindAllResponse
    {
        public int? Id { get; set; }
        public string? Apoderado { get; set; }
        public string? DniApoderado { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Edad { get; set; }
        public string? Direccion { get; set; }
        public string? FechaCumple { get; set; }
        public string? Telefono { get; set; }
        public PersonaRes2? Persona { get; set; }
        public bool? CondicionFecha { get; set; }
    }
    public class PacienteCreateResponse
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Paciente? PacienteRes { get; set; }
    }
    public class PacienteUpdateResponse
    {
        public string? Msg { get; set; }  // Puede ser nulo      
        public Paciente? PacienteRes { get; set; }
    }

    public class PacienteFindIdResponse
    {
        public int? Id { get; set; }  // Puede ser nulo
        public string? Apoderado { get; set; }
        public string? DniApoderado { get; set; }
        public string? Direccion { get; set; }
        public PersonaRes? PersonaFk { get; set; }  // Puede ser nulo
        public bool? CondicionFecha { get; set; }
    }
    
}
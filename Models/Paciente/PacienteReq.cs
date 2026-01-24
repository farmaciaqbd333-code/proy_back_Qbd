using System.Text.Json.Serialization;

namespace Proy_back_QBD.Dto.Request
{
    public class PacienteCreateReq
    {

        public string? Apoderado { get; set; }
        public string? DniApoderado { get; set; }
        public int? CreadorId { get; set; }
        public int? SedeId { get; set; }
        public PersonaCreateReq? Persona { get; set; }
        public bool? CondicionFecha { get; set; }
    }
    public class PacienteUpdateReq 
    {
        public string? Apoderado { get; set; }
        public string? DniApoderado { get; set; }
        public int? ModificadorId { get; set; }        
        public PersonaUpdateReq? Persona { get; set; }
        public bool? CondicionFecha { get; set; }
    }
}

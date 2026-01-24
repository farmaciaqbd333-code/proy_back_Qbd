using System.Text.Json.Serialization;

namespace Proy_back_QBD.Dto.Request
{
    public class MedicoCreateReq
    {
        public int? EspecialidadId { get; set; }  // Puede ser nulo      
        public string? NumeroEspecialidad { get; set; }  // Puede ser nulo      
        public int? CreadorId { get; set; }
        public int? SedeId { get; set; }
        public PersMedCreateReq? PersonaCReq { get; set; }
        public string? Cmp { get; set; }
    }
    public class MedicoUpdateReq
    {
        public int? EspecialidadId { get; set; }  // Puede ser nulo      
        public string? NumeroEspecialidad { get; set; }  // Puede ser nulo      
        public int? ModificadorId { get; set; }
        public PersMedUpdateReq? PersonaCReq { get; set; }  // Puede ser nulo    
        public string? Cmp { get; set; }  // Puede ser nulo        
    }
}
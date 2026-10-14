namespace Proy_back_QBD.Dto.Response
{
    public class SedeCreateRes
    {
        public int? Id { get; set; }
    }
    public class SedeFindAllResponse
    {
        public int Id { get; set; }  // Puede ser nulo
        public required string Nombre { get; set; }
        public string? Direccion { get; set; }  // Puede ser nulo               
        public string? Encargado { get; set; }  // ID original del encargado               
        public string? EncargadoId { get; set; } // ID del encargado
        public string? EncargadoNombre { get; set; } // Nombre completo del encargado
        public string? Telefono { get; set; }  // Puede ser nulo   
    }
    public class GeneralRes
    {
        public int? Meta { get; set; }
        public string? MsgTerminado { get; set; }
        public string? MsgCumple { get; set; }
        public string? MsgSeguimiento { get; set; }
        public string? MsgProceso { get; set; }
        public string? MsgGpt { get; set; }
    }
}

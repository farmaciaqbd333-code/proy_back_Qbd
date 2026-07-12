using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Dto.Response
{
    public class AsistenciaCreateRes
    {
        public TimeOnly? HoraMarcada { get; set; }
        public TimeSpan? Diferencia { get; set; }
    }

     public class FechaConHoras
    {
        public string? Dia { get; set; }
        public string? HoraEntrada { get; set; }
        public string? HoraSalida { get; set; }
        public string? HoraAlmuerzo { get; set; }
        public string? HoraRegreso { get; set; }
    }
    public class AsistenciaByIdRes
    {
        public string? NombreCompleto { get; set; }
        public TimeOnly? Almuerzo { get; set; }
        public TimeOnly? Entrada { get; set; }
        public TimeOnly? Regreso { get; set; }
        public TimeOnly? Salida { get; set; }
        public List<FechaConHoras>? Asistencias { get; set; }
    }
}
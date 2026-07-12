using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Request
{
    public class CobroCreateReq
    {
        public int? PedidoId { get; set; }
        public int? SedeId { get; set; }
        public string? Periodo { get; set; }
        public string? Modalidad { get; set; }
        public string? Turno { get; set; }
        public decimal? Importe { get; set; }
        public int? CreadorId { get; set; }
    }
    public class CobroUpdateReq
    {
        public string? Modalidad { get; set; }
        public string? Turno { get; set; }
        public DateTime FechaCreacion { get; set; }
        public decimal Importe { get; set; }
        public int? ModificadorId { get; set; }
    }
    public class CobroByIdReq
    {
        [Required]
        public int Año { get; set; }
        [Required]
        public int Mes { get; set; }
    }
    public class CajaFindAllReq
    {
        public DateOnly FechaInicio { get; set; }
        public DateOnly FechaFinal { get; set; }
    }
    public class UltimosCobros
    {
        public int PedidoId { get; set; }
        public int CobroId { get; set; }
    }
}
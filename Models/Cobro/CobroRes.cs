using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proy_back_QBD.Response
{
    using System.ComponentModel.DataAnnotations.Schema;
    using global::Proy_back_QBD.Models;

    namespace Proy_back_QBD.Dto.Response
    {

        public class CobroCreateRes
        {
            public string? Msg { get; set; }
            public Cobro? Cobro { get; set; }
        }
        public class CobroByPedido
        {
            public int? Id { get; set; }
            public string? CUO { get; set; }
            public DateTime? FechaCreacion { get; set; }
            public string? Turno { get; set; }
            public string? Modalidad { get; set; }
            public string? NroOperacion { get; set; }
            public decimal? Importe { get; set; }
        }

        public class CajaFindAllRes
        {
            public List<Movimientos>? Movimientos { get; set; }
            public RecaudacionDelDia? RecaudacionDelDia { get; set; }
            public RPagosDelDia? RPagosDelDia { get; set; }
            public RPagosAnteriores? RPagosAnteriores { get; set; }
            public BQPagosDelDia? BQPagos { get; set; }
            public Ventas? Ventas { get; set; }
            public List<DeudasPendientes>? Deudas { get; set; }
        }
        public class Movimientos
        {
            public string? CUO_R { get; set; }
            public string? CUO_C { get; set; }
            public DateOnly? FechaCobro { get; set; }
            public string? Dni { get; set; }
            public string? Paciente { get; set; }
            public DateOnly? FechaPedido { get; set; }
            public string? Estado { get; set; }
            public string? Modalidad { get; set; }
            public decimal? Importe { get; set; }
            public TimeOnly? Hora { get; set; }
            public string? Turno { get; set; }
            public string? BolFac { get; set; }
            public List<Movimientos2>? Lista { get; set; }
        }
        public class Movimientos2
        {
            public string? CUO_R { get; set; }
            public string? CUO_C { get; set; }
            public DateOnly? FechaCobro { get; set; }
            public string? Dni { get; set; }
            public string? Paciente { get; set; }
            public DateOnly? FechaPedido { get; set; }
            public string? Estado { get; set; }
            public string? Modalidad { get; set; }
            public decimal? Importe { get; set; }
            public TimeOnly? Hora { get; set; }
            public string? Turno { get; set; }
            public string? BolFac { get; set; }
        }
        public class MovTerm
        {
            public string? Modalidad { get; set; }
            public decimal? Importe { get; set; }
        }

        public class DeudasPendientes
        {
            public string? CUO_R { get; set; }
            public DateOnly? FechaPedido { get; set; }
            public string? Recibo { get; set; }
            public string? Dni { get; set; }
            public string? Paciente { get; set; }
            public string? Telefono { get; set; }
            public decimal? Importe { get; set; }
            public decimal? Adelanto { get; set; }
            public decimal? Saldo { get; set; }
            public string? BolFac { get; set; }
        }
        public class RecaudacionDelDia
        {
            public decimal? Total { get; set; } = 0;
            public decimal? Efectivo { get; set; } = 0;
            public decimal? Electronico { get; set; } = 0;
        }
        public class RPagosDelDia
        {
            public decimal? Total { get; set; } = 0;
            public decimal? Efectivo { get; set; } = 0;
            public decimal? Electronico { get; set; } = 0;
        }
        public class RPagosAnteriores
        {
            public decimal? Total { get; set; } = 0;
            public decimal? Efectivo { get; set; } = 0;
            public decimal? Electronico { get; set; } = 0;
        }
        public class BQPagosDelDia
        {
            public decimal? Total { get; set; } = 0;
            public decimal? Efectivo { get; set; } = 0;
            public decimal? Electronico { get; set; } = 0;
        }
        public class Ventas
        {
            public decimal? Total { get; set; } = 0;
            public decimal? Adelantos { get; set; } = 0;
            public decimal? Saldo { get; set; } = 0;
        }
    }
}
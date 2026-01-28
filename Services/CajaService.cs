using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Response;
using Proy_back_QBD.Response.Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Util;

namespace Proy_back_QBD.Services
{
    public class CajaService : ICajaService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public CajaService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


        public async Task<CajaFindAllRes?> Obtener(CajaFindAllReq request, int sedeId)
        {
            RecaudacionDelDia recaudDia = new();
            RPagosDelDia pagosDia = new();
            RPagosAnteriores pagosAnteriores = new();
            BQPagosDelDia bqPagos = new();
            Ventas ventas = new Ventas();
            List<DeudasPendientes> DeudasP = new();
            DateOnly FecFinal = request.FechaFinal.AddDays(1);
            var peruOffset = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time").GetUtcOffset(DateTime.UtcNow);
            List<Cobro> caja = await _context.Cobros
                    .Include(i => i.Pedido.Paciente.Persona)
                    .Include(i => i.Pedido.Formulas)
                    .Include(i => i.Pedido.ProdTerms)
                    .Include(i => i.Pedido.Cobros)
                    .Where(w =>
                        DateOnly.FromDateTime(w.FechaCreacion.AddMinutes(peruOffset.TotalMinutes)) >= request.FechaInicio
                        && DateOnly.FromDateTime(w.FechaCreacion.AddMinutes(peruOffset.TotalMinutes)) <= request.FechaFinal && w.SedeId == sedeId
                        )
                    .ToListAsync();

            List<Movimientos> movsTotal = caja
            .Where(w => w.Pedido.Estado != "DEVUELTO")
            .OrderByDescending(odb => odb.Id)
            .Select(s => new Movimientos
            {
                CUO_R = "P" + s.PedidoId,
                CUO_C = "C" + s.Id,
                FechaCobro = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s.FechaCreacion)),
                Dni = !string.IsNullOrEmpty(s.Pedido.Paciente.DniApoderado)
      ? s.Pedido.Paciente.DniApoderado
      : s.Pedido.Paciente.Persona.Dni,
                Paciente = s.Pedido.Paciente.Persona.NombreCompleto,
                FechaPedido = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s.Pedido.FechaCreacion)),
                Modalidad = s.Modalidad,
                Estado = s.Pedido.Estado,
                Importe = s.Importe,
                Hora = TimeOnly.FromDateTime(ZonaHoraria.AjustarZona(s.FechaCreacion)),
                Turno = s.Turno,
                BolFac = s.Pedido.ComprobanteElectronico,
                Lista = s.Pedido.Cobros.Where(w => w.Id != s.Id).Select(
                    s2 => new Movimientos2
                    {
                        CUO_R = "P" + s2.PedidoId,
                        CUO_C = "C" + s2.Id,
                        FechaCobro = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s2.FechaCreacion)),
                        Dni = !string.IsNullOrEmpty(s2.Pedido.Paciente.DniApoderado)
                            ? s2.Pedido.Paciente.DniApoderado
                            : s2.Pedido.Paciente.Persona.Dni,
                        Paciente = s2.Pedido.Paciente.Persona.NombreCompleto,
                        FechaPedido = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s2.Pedido.FechaCreacion)),
                        Modalidad = s2.Modalidad,
                        Estado = s2.Pedido.Estado,
                        Importe = s2.Importe,
                        Hora = TimeOnly.FromDateTime(ZonaHoraria.AjustarZona(s2.FechaCreacion)),
                        Turno = s2.Turno,
                        BolFac = s2.Pedido.ComprobanteElectronico,

                    }).ToList()
            })
            .ToList();
            List<Movimientos> movsTotal2 = caja
            .Where(w => w.Pedido.Estado != "DEVUELTO" && w.Pedido.ComprobanteElectronico != null)
            .OrderByDescending(odb => odb.Id)
            .Select(s => new Movimientos
            {
                CUO_R = "P" + s.PedidoId,
                CUO_C = "C" + s.Id,
                FechaCobro = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s.FechaCreacion)),
                Dni = !string.IsNullOrEmpty(s.Pedido.Paciente.DniApoderado)
      ? s.Pedido.Paciente.DniApoderado
      : s.Pedido.Paciente.Persona.Dni,
                Paciente = s.Pedido.Paciente.Persona.NombreCompleto,
                FechaPedido = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s.Pedido.FechaCreacion)),
                Modalidad = s.Modalidad,
                Estado = s.Pedido.Estado,
                Importe = s.Importe,
                Hora = TimeOnly.FromDateTime(ZonaHoraria.AjustarZona(s.FechaCreacion)),
                Turno = s.Turno,
                BolFac = s.Pedido.ComprobanteElectronico,
                Lista = s.Pedido.Cobros.Where(w => w.Id != s.Id).Select(
                    s2 => new Movimientos2
                    {
                        CUO_R = "P" + s2.PedidoId,
                        CUO_C = "C" + s2.Id,
                        FechaCobro = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s2.FechaCreacion)),
                        Dni = !string.IsNullOrEmpty(s2.Pedido.Paciente.DniApoderado)
                            ? s2.Pedido.Paciente.DniApoderado
                            : s2.Pedido.Paciente.Persona.Dni,
                        Paciente = s2.Pedido.Paciente.Persona.NombreCompleto,
                        FechaPedido = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(s2.Pedido.FechaCreacion)),
                        Modalidad = s2.Modalidad,
                        Estado = s2.Pedido.Estado,
                        Importe = s2.Importe,
                        Hora = TimeOnly.FromDateTime(ZonaHoraria.AjustarZona(s2.FechaCreacion)),
                        Turno = s2.Turno,
                        BolFac = s2.Pedido.ComprobanteElectronico,

                    }).ToList()
            })
            .ToList();
            List<MovTerm> movsHoy = caja
            .Where(w => w.Pedido.Estado != "DEVUELTO" && DateOnly.FromDateTime(w.Pedido.FechaCreacion.AddMinutes(peruOffset.TotalMinutes)) == request.FechaFinal)
            .OrderByDescending(odb => odb.Id)
            .Select(s => new MovTerm
            {
                Modalidad = s.Modalidad,
                Importe = s.Importe

            })
            .ToList();

            List<int> pedidosId = caja
            .Where(w => w.Pedido.Estado != "DEVUELTO")
            .Select(s => s.PedidoId)
            .ToList();

            List<MovTerm> MovsAnt = await _context.Cobros
            .Where(w => pedidosId.Contains(w.PedidoId) &&
            DateOnly.FromDateTime(w.Pedido.FechaCreacion.AddMinutes(peruOffset.TotalMinutes)) < request.FechaInicio &&
            DateOnly.FromDateTime(w.FechaCreacion.AddMinutes(peruOffset.TotalMinutes)) == request.FechaFinal && w.SedeId == sedeId
             )
            .Select(s => new MovTerm
            {
                Modalidad = s.Modalidad,
                Importe = s.Importe,
            }).ToListAsync();

            List<int> idCajaTerms = caja
            .Where(w => w.Pedido.Saldo == 0 && !string.IsNullOrWhiteSpace(w.Pedido.ComprobanteElectronico))
            .Select(s => s.PedidoId).ToList();

            List<int> idMovsTerm = caja
                       .Where(w => w.Pedido.Estado != "DEVUELTO" && w.Pedido.Saldo == 0 && !string.IsNullOrWhiteSpace(w.Pedido.ComprobanteElectronico))
                       .Select(s => s.PedidoId)
                       .ToList();
            List<int> idCobroBQ = caja
                       .Where(w => w.Pedido.Estado != "DEVUELTO" && w.Pedido.Saldo == 0 && !string.IsNullOrWhiteSpace(w.Pedido.ComprobanteElectronico))
                       .Select(s => s.Id)
                       .ToList();

            List<UltimosCobros?> UltimosCobros = await _context.Cobros
.Where(w => idMovsTerm.Contains(w.PedidoId) && w.SedeId == sedeId)
.GroupBy(gb => gb.PedidoId)
.Select(s => new UltimosCobros
{
    PedidoId = s.Key,
    CobroId = s.Max(x => x.Id)
})
.ToListAsync();


            List<int> pedidoBQ = new();

            foreach (var item in UltimosCobros)
            {
                foreach (var item2 in idCobroBQ)
                {
                    if (item.CobroId == item2)
                    {
                        pedidoBQ.Add(item.PedidoId);
                    }
                }
            }

            List<MovTerm> movsTerm = new();

            DateOnly hoy = DateOnly.FromDateTime(ZonaHoraria.AjustarZona(DateTime.Now));

            movsTerm = await _context.Cobros
            .Include(i => i.Pedido)
           .Where(w => pedidoBQ.Contains(w.PedidoId) && w.SedeId == sedeId)
           .Select(s => new MovTerm
           {
               Modalidad = s.Modalidad,
               Importe = s.Importe
           })
           .ToListAsync();


            List<Movimientos> movimientos = new List<Movimientos>();
            if (movsTotal != null) movimientos.AddRange(movsTotal);

            foreach (var item in movsHoy)
            {
                if (item.Modalidad.Trim().ToUpper() == "YAPE"
                || item.Modalidad.Trim().ToUpper() == "PLIN"
                || item.Modalidad.Trim().ToUpper() == "DEPÓSITO"
                || item.Modalidad.Trim().ToUpper() == "TRANSFERENCIA"
                || item.Modalidad.Trim().ToUpper() == "TARJETA DE CRÉDITO"
                || item.Modalidad.Trim().ToUpper() == "TARJETA DE DÉBITO"
                )
                {
                    pagosDia.Electronico += item.Importe;
                    recaudDia.Electronico += item.Importe;
                }
                else
                {
                    pagosDia.Efectivo += item.Importe;
                    recaudDia.Efectivo += item.Importe;
                }
                pagosDia.Total += item.Importe;
            }
            foreach (var item in movsTerm)
            {
                if (item.Modalidad.Trim().ToUpper() == "YAPE"
                || item.Modalidad.Trim().ToUpper() == "PLIN"
                || item.Modalidad.Trim().ToUpper() == "DEPÓSITO"
                || item.Modalidad.Trim().ToUpper() == "TRANSFERENCIA"
                || item.Modalidad.Trim().ToUpper() == "TARJETA DE CRÉDITO"
                || item.Modalidad.Trim().ToUpper() == "TARJETA DE DÉBITO"
                )
                {
                    bqPagos.Electronico += item.Importe;
                }
                else
                {
                    bqPagos.Efectivo += item.Importe;
                }
                bqPagos.Total += item.Importe;
            }

            foreach (var item in MovsAnt)
            {
                if (item.Modalidad.Trim().ToUpper() == "YAPE"
                || item.Modalidad.Trim().ToUpper() == "PLIN"
                || item.Modalidad.Trim().ToUpper() == "DEPOSITO"
                || item.Modalidad.Trim().ToUpper() == "TRANSFERENCIA"
                || item.Modalidad.Trim().ToUpper() == "TARJETA DE CRÉDITO"
                || item.Modalidad.Trim().ToUpper() == "TARJETA DE DÉBITO"
                )
                {
                    recaudDia.Electronico += item.Importe;
                    pagosAnteriores.Electronico += item.Importe;
                }
                else
                {
                    recaudDia.Efectivo += item.Importe;
                    pagosAnteriores.Efectivo += item.Importe;
                }
                pagosAnteriores.Total += item.Importe;
            }

            recaudDia.Total = 0;
            recaudDia.Total += pagosDia.Total + pagosAnteriores.Total;

            List<Pedido> ventasP = await _context.Pedidos
                   .Include(i => i.Paciente.Persona)
                   .Where(w =>
                       DateOnly.FromDateTime(w.FechaCreacion.AddMinutes(peruOffset.TotalMinutes)) >= request.FechaInicio
                       && DateOnly.FromDateTime(w.FechaCreacion.AddMinutes(peruOffset.TotalMinutes)) < FecFinal && w.SedeId == sedeId
                       )
                   .ToListAsync();

            ventas.Total = 0;
            ventas.Total = ventasP.Sum(p => p.Total);
            ventas.Adelantos = ventasP.Sum(p => p.Adelanto);
            ventas.Saldo = ventasP.Sum(p => p.Saldo);

            DeudasP = ventasP
            .Where(w => w.Saldo != 0)
            .Select(s => new DeudasPendientes
            {
                CUO_R = "BDRP-" + s.Id,
                FechaPedido = DateOnly.FromDateTime(s.FechaCreacion),
                Recibo = s.Id.ToString(),
                Dni = s.Paciente.Persona.Dni ?? s.Paciente.DniApoderado,
                Paciente = s.Paciente.Persona.NombreCompleto,
                Telefono = s.Paciente.Persona.Telefono,
                Importe = s.Total,
                Adelanto = s.Adelanto,
                Saldo = s.Saldo,
                BolFac = s.ComprobanteElectronico,
            }).ToList();

            CajaFindAllRes? response = new CajaFindAllRes
            {
                Movimientos = movimientos,
                RecaudacionDelDia = recaudDia,
                RPagosDelDia = pagosDia,
                RPagosAnteriores = pagosAnteriores,
                BQPagos = bqPagos,
                Ventas = ventas,
                Deudas = DeudasP
            };

            return response;
        }
    }

}

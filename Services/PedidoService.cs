using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;

using Proy_back_QBD.Models;
using Proy_back_QBD.Util;

namespace Proy_back_QBD.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public PedidoService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PedidoUpdateResponse?> Actualizar(int id, int sedeId, PedidoUpdateReq request)
        {
            string? reciboF = await _context.Pedidos
            .Where(w => w.Id == id && w.SedeId == sedeId)
            .Select(s => s.Recibo).FirstOrDefaultAsync();
            if (reciboF.Trim() != request.Recibo.Trim())
            {
                bool validacion = await _context.Pedidos.AnyAsync(a => a.Recibo == request.Recibo);
                if (validacion)
                {
                    return null;
                }
            }

            PedidoUpdateResponse response = new PedidoUpdateResponse();
            Pedido? pedido = await _context.Pedidos
            .Include(i => i.Formulas)
            .Include(i => i.ProdTerms)
            .Where(p => p.Id == id && p.SedeId == sedeId)
            .FirstOrDefaultAsync();

            if (pedido == null)
            {
                response.Msg = "no se encontró";
                return response;
            }

            if (request.Estado != pedido.Estado)
            {
                bool validar = true;
                if (pedido.Estado.ToUpper() == "DEVUELTO")
                {
                    validar = false;
                    foreach (var item in pedido.Formulas)
                    {
                        if (item.Estado.ToUpper() != "DEVUELTO")
                        {
                            validar = true;
                        }
                    }
                    foreach (var item in pedido.ProdTerms)
                    {
                        if (item.Estado.ToUpper() != "DEVUELTO")
                        {
                            validar = true;
                        }
                    }
                }
                if (validar)
                {
                    if (pedido.Formulas != null && pedido.Formulas.Count > 0)
                    {
                        foreach (var item in pedido.Formulas)
                        {
                            if (item.Estado.ToUpper() != "DEVUELTO") item.Estado = request.Estado.ToUpper();
                        }
                    }
                    if (pedido.ProdTerms != null && pedido.ProdTerms.Count > 0)
                    {
                        if (request.Estado.ToUpper() != "EN PROCESO")
                        {
                            if (request.Estado.ToUpper() == "TERMINADO")
                            {
                                request.Estado = "PT";
                            }
                            foreach (var item in pedido.ProdTerms)
                            {
                                if (item.Estado.ToUpper() != "DEVUELTO") item.Estado = request.Estado.ToUpper();
                            }
                        }
                    }
                }
                else
                {
                    response.Msg = "Primero cambie un producto terminado o formula a otro estado que no sea devuelto";
                    return response;
                }
                pedido.Estado = request.Estado.ToUpper();
                if (pedido.Formulas == null && pedido.Formulas.Count == 0) pedido.Estado = "PT";
            }
            _mapper.Map(request, pedido);
            response.Msg = "Pedido Actualizado";
            response.PedidoRes = pedido;
            await _context.SaveChangesAsync();
            return response;
        }
        public async Task<string?> ActEstado(int id, int sedeId, string request)
        {
            string estado = request.ToUpper().Trim();
            string response;
            Pedido? pedido = await _context.Pedidos
            .Include(i => i.Formulas)
            .FirstOrDefaultAsync(p => p.Id == id && p.SedeId == sedeId);

            if (pedido == null)
            {
                response = "no se encontró";
                return response;
            }
            if (pedido.Formulas != null)
            {
                if (estado != pedido.Estado)
                {
                    foreach (var item in pedido.Formulas)
                    {
                        if (item.Estado != "DEVUELTO")
                        {
                            item.Estado = estado;
                        }
                    }
                }
                else
                {
                    response = "Es el mismo estado";
                    return response;
                }
            }

            if (pedido.ProdTerms != null)
            {
                if (estado == "ENTREGADO")
                {
                    foreach (var item in pedido.ProdTerms)
                    {
                        if (item.Estado != "DEVUELTO")
                        {
                            item.Estado = estado;
                        }
                    }
                }
            }
            pedido.Estado = estado;
            response = "Estado Actualizado";
            await _context.SaveChangesAsync();
            return response;
        }

        public async Task<PedidoCreateRes?> Crear(PedidoCreateReq request)
        {
            bool validacion = await _context.Pedidos.AnyAsync(aa => aa.Recibo == request.Recibo);
            if (validacion)
            {
                return null;
            }
            if (request.ProductosTerminados == null && request.Formulas == null)
            {
                return null;
            }
            if (request.ProductosTerminados.Count() == 0 && request.Formulas.Count() == 0)
            {
                return null;
            }
            PedidoCreateRes response = new PedidoCreateRes();
            List<Formula> formulaList = new();
            List<ProdTerm> prodTermList = new();
            var zonaPeru = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            DateTime horaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaPeru);
            var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(horaLocal.Date, zonaPeru);
            var finDiaUtc = TimeZoneInfo.ConvertTimeToUtc(horaLocal.Date.AddDays(1), zonaPeru);

            int correlativo = await _context.Formulas
                .Where(w => w.FechaCreacion >= inicioDiaUtc && w.FechaCreacion < finDiaUtc)
                .CountAsync() + 1;

            var codLote =
           inicioDiaUtc.Year.ToString().Substring(2, 2) +
           inicioDiaUtc.Month.ToString("D2") + // El mes con 2 dígitos
           inicioDiaUtc.Day.ToString("D2");

            int c = 0;

            if (request.Formulas != null && request.Formulas.Count > 0)
            {
                foreach (var item in request.Formulas)
                {
                    string prefijo = request.SedeId switch
                    {
                        3 => "P",   // PUNO
                        2 => "PJ",  // JULIACA
                        4 => "T",   // TACNA
                        1 => "L",   // LIMA
                        12 => "CS",  // SICUANI
                        _ => "FM"   // por seguridad
                    };
                    Formula formula = _mapper.Map<Formula>(item);
                    formula.Estado = "PENDIENTE";
                    formula.Reportado = "PENDIENTE";
                    formula.SedeId = request.SedeId;
                    formula.Lote = prefijo + "-" + codLote + (correlativo + c).ToString();
                    c++;
                    formulaList.Add(formula);
                }
            }
            if (request.ProductosTerminados != null && request.ProductosTerminados.Count > 0)
            {
                foreach (var item in request.ProductosTerminados)
                {
                    ProdTerm prodTerm = _mapper.Map<ProdTerm>(item);
                    prodTerm.Estado = "PT";
                    prodTerm.SedeId = request.SedeId;
                    prodTermList.Add(prodTerm);
                }
            }
            decimal total = SumaPedido(formulaList, prodTermList);

            Pedido pedido = _mapper.Map<Pedido>(request);
            pedido.Total = total;
            pedido.Saldo = total;
            if (request.Formulas == null || request.Formulas.Count == 0)
            {
                pedido.Estado = "PT";
            }
            else
            {
                pedido.Estado = "PENDIENTE";
            }
            pedido.ModificadorId = pedido.CreadorId;
            response.PedidoRes = pedido;
            response.Msg = "Pedido creado exitosamente.";

            await _context.Pedidos.AddAsync(pedido);
            await _context.SaveChangesAsync();

            foreach (var item in prodTermList)
            {
                item.ModificadorId = item.CreadorId;
                item.PedidoId = pedido.Id;
                item.SedeId = pedido.SedeId;
                await _context.ProdTerms.AddAsync(item);
            }

            foreach (var item in formulaList)
            {
                item.ModificadorId = item.CreadorId;
                item.PedidoId = pedido.Id;
                item.SedeId = pedido.SedeId;
                await _context.Formulas.AddAsync(item);
            }

            await _context.SaveChangesAsync();

            return response;
        }

        public async Task<Pedido?> ActComprobante(int id, int sedeId, string? comprobante)
        {
            Pedido? pedido = await _context.Pedidos
            .Include(p => p.Formulas)
            .Include(p => p.ProdTerms)
           .FirstOrDefaultAsync(a => a.Id == id && a.SedeId == sedeId);
            if (pedido == null) return null;

            pedido.ComprobanteElectronico = comprobante;

            await _context.SaveChangesAsync();

            return pedido;
        }

        public async Task<List<PedidoFindAllResponse?>> Listar(int sedeId)
        {

            var response = await _context.Pedidos
                .Include(a => a.Paciente.Persona)
                .Include(a => a.Medico.Persona)
                .Include(a => a.Medico.Especialidad)
                .Include(a => a.Creador)
                .Include(a => a.Cobros)
                .Include(a => a.Formulas)
                .Include(a => a.ProdTerms)
                .OrderByDescending(a => a.FechaCreacion) // Ordenar por fecha (puedes cambiar el criterio)
                .Where(w => w.SedeId == sedeId)
                .Select(a => new PedidoFindAllResponse
                {
                    Id = a.Id,
                    Cuo = $"P{a.Id}",
                    FechaCreacion = ZonaHoraria.AjustarZona(a.FechaCreacion),
                    Dni = a.Paciente.Persona.Dni,
                    DniApoderado = a.Paciente.DniApoderado,
                    Paciente = a.Paciente.Persona.NombreCompleto,
                    PacienteId = a.PacienteId,
                    Edad = PacienteService.CalcularEdad(a.Paciente.Persona.FechaNacimiento),
                    Recibo = a.Recibo,
                    Celular = a.Paciente.Persona.Telefono,
                    Medico = $"{a.Medico.Persona.NombreCompleto}",
                    Especialidad = a.Medico.Especialidad.Nombre,
                    Cmp = a.Medico.Cmp,
                    Total = a.Total,
                    Adelanto = a.Adelanto,
                    Saldo = a.Saldo,
                    Estado = a.Estado,
                    FechaEntrega = ZonaHoraria.AjustarZona(a.FechaEntrega),
                    Usuario = a.Creador.Codigo,
                    ComprobanteElectronico = a.ComprobanteElectronico,
                })
                .ToListAsync();

            return response;
        }

        // public static string? CalcularEstado(List<Formula> Formulas)
        // {

        //     string? resultado = "ENTREGADO";

        //     foreach (var formula in Formulas)
        //     {
        //         if (formula.Estado.Trim().ToUpper().Equals("PENDIENTE"))
        //         {
        //             resultado = "PENDIENTE";
        //         }
        //         else if (formula.Estado.Trim().ToUpper().Equals("EN PROCESO"))
        //         {
        //             resultado = "EN PROCESO";
        //         }
        //         else if (formula.Estado.Trim().ToUpper().Equals("TERMINADO"))
        //         {
        //             resultado = "TERMINADO";
        //         }
        //         else if (formula.Estado.Trim().ToUpper().Equals("ENTREGADO"))
        //         {
        //             resultado = "ENTREGADO";
        //         }
        //     }

        //     return resultado;
        // }

        public async Task<PedidoFindIdResponse?> ObtenerById(int id, int sedeId)
        {

            Pedido? pedido = await _context.Pedidos
            .Include(a => a.Formulas)
            .Include(a => a.ProdTerms)
            .ThenInclude(ti => ti.Producto)
            .FirstOrDefaultAsync(p => p.Id == id && p.SedeId == sedeId);
            if (pedido == null)
            {
                return null;
            }
            pedido.FechaEntrega = ZonaHoraria.AjustarZona(pedido.FechaEntrega);
            PedidoFindIdResponse response = _mapper.Map<PedidoFindIdResponse>(pedido);
            foreach (var item in response.Formulas)
            {
                item.Codigo = "REG-" + item.Id;
            }

            return response;

        }

        public static decimal SumaPedido(List<Formula>? listaForm, List<ProdTerm>? listaProdTerm)
        {
            decimal total = 0;

            if (listaForm == null || listaProdTerm == null)
            {
                throw new ArgumentNullException("Las listas no pueden ser null.");
            }

            if (listaForm.Count() != 0 || listaForm != null)
            {
                foreach (var formula in listaForm)
                {
                    if (formula.Estado.ToUpper().Trim() != "DEVUELTO")
                    {
                        total += formula.Costo * formula.Cantidad;
                    }

                }
            }
            if (listaProdTerm.Count() != 0 || listaProdTerm != null)
            {
                foreach (var prod in listaProdTerm)
                {
                    if (prod.Estado.ToUpper().Trim() != "DEVUELTO")
                    {
                        total += prod.Costo * prod.Cantidad;
                    }

                }
            }

            return total;
        }
        public static decimal SumaCobro(List<Cobro>? listaCobro)
        {
            decimal total = 0;
            if (listaCobro.Count() == 0 || listaCobro == null)
            {
                return 0;
            }
            foreach (var cobro in listaCobro)
            {
                total += cobro.Importe;
            }
            return total;
        }

        public async Task<List<PedidoLabFindAllRes2?>> ListarLab(int sedeId)
        {
            var response = await _context.Pedidos
                .Include(i => i.Formulas)
               .OrderByDescending(a => a.FechaCreacion) // Ordenar por fecha (puedes cambiar el criterio)
               .Where(w => w.SedeId == sedeId && w.Formulas.Count() > 0)
               .Select(a => new PedidoLabFindAllRes2
               {
                   Id = a.Id,
                   Cuo = $"P{a.Id}"
               })
               .ToListAsync();
            if (response == null)
            {
                return null;
            }

            return response;
        }

        public async Task<int> ContFormM(int sedeId, int mes, int? anio = null)
        {
            int year = anio ?? DateTime.Now.Year;
            var response = await _context.Pedidos
                    .Include(i => i.Formulas)
                    .Where(w => w.SedeId == sedeId && w.Formulas.Count() > 0)
                    .Where(w => w.FechaCreacion.Month == mes && w.FechaCreacion.Year == year).ToListAsync();

            int totalFormulas = await _context.Pedidos
.Where(w => w.SedeId == sedeId && w.Formulas.Count() > 0)
.Where(w => w.FechaCreacion.Month == mes && w.FechaCreacion.Year == year)
.SumAsync(w => w.Formulas.Count());
            return totalFormulas;

        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Proy_back_QBD.Services.Interfaces;
using proy_back_Qbd.Exceptions;

namespace Proy_back_QBD.Services
{
    public class CotizacionService : ICotizacionService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;

        public CotizacionService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<CotizacionRes>> Listar(int? sedeId, DateOnly? fechaInicio, DateOnly? fechaFin, string? search, string? estado)
        {
            var query = _context.Cotizaciones
                .Include(c => c.Detalles)
                .Include(c => c.Sede)
                .Include(c => c.Creador)
                .AsQueryable();

            if (sedeId.HasValue && sedeId.Value > 0)
            {
                query = query.Where(c => c.SedeId == sedeId.Value);
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(c => c.Fecha >= fechaInicio.Value);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(c => c.Fecha <= fechaFin.Value);
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(c => c.Estado.ToLower() == estado.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Numero.ToLower().Contains(term) ||
                    c.RucDni.ToLower().Contains(term) ||
                    c.Denominacion.ToLower().Contains(term) ||
                    (c.CpeRelacionado != null && c.CpeRelacionado.ToLower().Contains(term))
                );
            }

            var cotizaciones = await query
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            return _mapper.Map<List<CotizacionRes>>(cotizaciones);
        }

        public async Task<CotizacionRes?> ObtenerPorId(int id)
        {
            var cotizacion = await _context.Cotizaciones
                .Include(c => c.Detalles)
                .Include(c => c.Sede)
                .Include(c => c.Creador)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cotizacion == null) return null;

            return _mapper.Map<CotizacionRes>(cotizacion);
        }

        public async Task<string> ObtenerSiguienteNumero(int? sedeId)
        {
            var numeros = await _context.Cotizaciones
                .Select(c => c.Numero)
                .ToListAsync();

            int max = 0;
            foreach (var n in numeros)
            {
                if (int.TryParse(n, out int val) && val > max)
                {
                    max = val;
                }
            }

            return (max > 0 ? max + 1 : 1001).ToString();
        }

        public async Task<CotizacionRes?> Crear(CotizacionCreateReq request, int usuarioId)
        {
            if (string.IsNullOrWhiteSpace(request.Numero))
            {
                request.Numero = await ObtenerSiguienteNumero(request.SedeId);
            }

            var cotizacion = _mapper.Map<Cotizacion>(request);
            cotizacion.CreadorId = usuarioId;
            cotizacion.ModificadorId = usuarioId;
            cotizacion.FechaCreacion = DateTime.Now;
            cotizacion.FechaModificacion = DateTime.Now;

            if (request.Fecha.HasValue)
            {
                cotizacion.Fecha = request.Fecha.Value;
            }
            else
            {
                cotizacion.Fecha = DateOnly.FromDateTime(DateTime.Now);
            }

            // Mapear detalles
            cotizacion.Detalles = request.Detalles.Select(d => new CotizacionDetalle
            {
                ProductoId = d.ProductoId,
                FormulaId = d.FormulaId,
                Codigo = d.Codigo,
                Descripcion = d.Descripcion,
                Unidad = string.IsNullOrWhiteSpace(d.Unidad) ? "NIU" : d.Unidad,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento,
                TipoProducto = NormalizarTipoProducto(d.TipoProducto),
                FormaFarmaceutica = d.FormaFarmaceutica,
                GPorMl = d.GPorMl,
                UnidadMedida = d.UnidadMedida,
                Subtotal = d.Subtotal
            }).ToList();

            // Recalcular montos si no se especificaron
            if (cotizacion.Total <= 0 && cotizacion.Detalles.Count > 0)
            {
                decimal subtotal = cotizacion.Detalles.Sum(d => d.Subtotal);
                decimal descTotal = cotizacion.DescuentoGlobal + cotizacion.DescuentoItem;
                decimal baseImponible = Math.Max(0, subtotal - descTotal);
                cotizacion.Total = baseImponible;
                cotizacion.OpGravada = Math.Round(baseImponible / 1.18m, 2);
                cotizacion.Igv = Math.Round(baseImponible - cotizacion.OpGravada, 2);
            }

            AplicarAdelanto(cotizacion, request.Adelanto, request.AdelantoMedioPago);

            _context.Cotizaciones.Add(cotizacion);
            await _context.SaveChangesAsync();

            return await ObtenerPorId(cotizacion.Id);
        }

        public async Task<CotizacionRes?> Actualizar(int id, CotizacionUpdateReq request, int usuarioId)
        {
            var cotizacion = await _context.Cotizaciones
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cotizacion == null) return null;

            cotizacion.Numero = !string.IsNullOrWhiteSpace(request.Numero) ? request.Numero : cotizacion.Numero;
            if (request.Fecha.HasValue) cotizacion.Fecha = request.Fecha.Value;
            cotizacion.SedeId = request.SedeId;
            cotizacion.PacienteId = request.PacienteId;
            cotizacion.RucDni = request.RucDni;
            cotizacion.Denominacion = request.Denominacion;
            cotizacion.Direccion = request.Direccion;
            cotizacion.Telefono = request.Telefono;
            cotizacion.Correo = request.Correo;
            cotizacion.Moneda = request.Moneda ?? "S/";
            cotizacion.DescuentoGlobalPorc = request.DescuentoGlobalPorc;
            cotizacion.DescuentoGlobal = request.DescuentoGlobal;
            cotizacion.DescuentoItem = request.DescuentoItem;
            cotizacion.DescuentoTotal = request.DescuentoTotal;
            cotizacion.OpExonerada = request.OpExonerada;
            cotizacion.OpInafecta = request.OpInafecta;
            cotizacion.OpGravada = request.OpGravada;
            cotizacion.Igv = request.Igv;
            cotizacion.OpGratuita = request.OpGratuita;
            cotizacion.OtrosCargos = request.OtrosCargos;
            cotizacion.Total = request.Total;
            cotizacion.Estado = !string.IsNullOrWhiteSpace(request.Estado) ? request.Estado : cotizacion.Estado;
            cotizacion.CpeRelacionado = request.CpeRelacionado;
            cotizacion.ValidezDias = request.ValidezDias;
            cotizacion.FormaPago = request.FormaPago;
            cotizacion.Observaciones = request.Observaciones;
            // Una cotización ya convertida en pedido conserva su adelanto: ya pasó a ser un cobro del pedido
            if (!string.Equals(cotizacion.Estado, "Convertido", StringComparison.OrdinalIgnoreCase))
            {
                AplicarAdelanto(cotizacion, request.Adelanto, request.AdelantoMedioPago);
            }
            else if (cotizacion.Adelanto > cotizacion.Total)
            {
                throw new BadRequestException("El total no puede ser menor al adelanto ya cobrado.");
            }
            cotizacion.ModificadorId = usuarioId;
            cotizacion.FechaModificacion = DateTime.Now;

            // Reemplazar detalles
            _context.CotizacionesDetalles.RemoveRange(cotizacion.Detalles);
            cotizacion.Detalles = request.Detalles.Select(d => new CotizacionDetalle
            {
                CotizacionId = id,
                ProductoId = d.ProductoId,
                FormulaId = d.FormulaId,
                Codigo = d.Codigo,
                Descripcion = d.Descripcion,
                Unidad = string.IsNullOrWhiteSpace(d.Unidad) ? "NIU" : d.Unidad,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Descuento = d.Descuento,
                TipoProducto = NormalizarTipoProducto(d.TipoProducto),
                FormaFarmaceutica = d.FormaFarmaceutica,
                GPorMl = d.GPorMl,
                UnidadMedida = d.UnidadMedida,
                Subtotal = d.Subtotal
            }).ToList();

            await _context.SaveChangesAsync();

            return await ObtenerPorId(id);
        }

        /// <summary>
        /// Valida y guarda el adelanto: no negativo, no mayor al total, con medio de pago.
        /// La fecha del adelanto se actualiza solo cuando cambia el monto.
        /// </summary>
        private static void AplicarAdelanto(Cotizacion cotizacion, decimal adelanto, string? medioPago)
        {
            adelanto = Math.Round(adelanto, 2);
            if (adelanto < 0)
                throw new BadRequestException("El adelanto no puede ser negativo.");
            if (adelanto > Math.Round(cotizacion.Total, 2))
                throw new BadRequestException($"El adelanto (S/ {adelanto:0.00}) no puede ser mayor al total de la cotización (S/ {cotizacion.Total:0.00}).");

            if (adelanto == 0)
            {
                cotizacion.Adelanto = 0;
                cotizacion.AdelantoMedioPago = null;
                cotizacion.AdelantoFecha = null;
                return;
            }

            if (adelanto != cotizacion.Adelanto || cotizacion.AdelantoFecha == null)
            {
                cotizacion.AdelantoFecha = DateTime.Now;
            }
            cotizacion.Adelanto = adelanto;
            cotizacion.AdelantoMedioPago = string.IsNullOrWhiteSpace(medioPago) ? "Efectivo" : medioPago.Trim();
        }

        public async Task<bool> CambiarEstado(int id, string nuevoEstado, int usuarioId, int? pedidoId = null)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return false;

            cotizacion.Estado = nuevoEstado;
            if (pedidoId.HasValue && pedidoId.Value > 0) cotizacion.PedidoId = pedidoId.Value;
            cotizacion.ModificadorId = usuarioId;
            cotizacion.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AsignarCpe(int id, string cpe, int usuarioId)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return false;

            cotizacion.CpeRelacionado = cpe;
            cotizacion.Estado = "Facturado";
            cotizacion.ModificadorId = usuarioId;
            cotizacion.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarcarEnviado(int id, bool enviado, int usuarioId)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return false;

            cotizacion.EnviadoCliente = enviado;
            cotizacion.ModificadorId = usuarioId;
            cotizacion.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Eliminar(int id)
        {
            var cotizacion = await _context.Cotizaciones.FindAsync(id);
            if (cotizacion == null) return false;

            _context.Cotizaciones.Remove(cotizacion);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Tipo de producto en mayúsculas (máx. 20 caracteres); null si viene vacío.</summary>
        private static string? NormalizarTipoProducto(string? tipo)
        {
            var t = (tipo ?? "").Trim().ToUpperInvariant();
            if (t.Length == 0) return null;
            return t.Length > 20 ? t.Substring(0, 20) : t;
        }
    }
}

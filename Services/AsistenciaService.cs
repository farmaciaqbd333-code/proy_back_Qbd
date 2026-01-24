using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Data;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Models;
using AutoMapper;
using Proy_back_QBD.Request;
using System.Diagnostics;
using System.Globalization;

namespace Proy_back_QBD.Services
{
    public class AsistenciaService : IAsistenciaService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public AsistenciaService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;

        }

        public async Task<AsistenciaByIdRes?> ObtenerPorId(int id, int año, int mes, int sedeId)
        {
            if (id == null || año <= 0 || mes <= 0 || mes > 12)
            {
                return null;
            }
            DateTime fechaFiltro = new DateTime(año, mes, 1, 0, 0, 0);
            
            var lista = await _context.Asistencias
                .Where(a => a.CreadorId == id && a.FechaCreacion.AddHours(-5).Year == año  && a.FechaCreacion.AddHours(-5).Month == mes && a.SedeId == sedeId)
                .GroupBy(a => a.FechaCreacion.AddHours(-5).Date)
                .Select(g => new FechaConHoras
                {
                    Dia = g.Key.ToString("dddd", new CultureInfo("es-ES")) + "-" + g.Key.Day,

                    HoraEntrada = g.Where(x => x.Tipo.Equals("entrada"))
                       .OrderBy(x => x.HoraMarcada)
                       .Select(x => x.HoraMarcada.ToString())
                       .FirstOrDefault(),

                    HoraSalida = g.Where(x => x.Tipo.Equals("salida"))
                      .OrderByDescending(x => x.HoraMarcada)
                      .Select(x => x.HoraMarcada.ToString())
                      .FirstOrDefault(),
                    HoraAlmuerzo = g.Where(x => x.Tipo.Equals("almuerzo"))
                      .OrderByDescending(x => x.HoraMarcada)
                      .Select(x => x.HoraMarcada.ToString())
                      .FirstOrDefault(),
                    HoraRegreso = g.Where(x => x.Tipo.Equals("regreso"))
                      .OrderByDescending(x => x.HoraMarcada)
                      .Select(x => x.HoraMarcada.ToString())
                      .FirstOrDefault(),
                })
                .ToListAsync();

            if (lista == null || lista.Count == 0)
            {
                return null;
            }

            AsistenciaByIdRes? response = await _context.Usuarios
                .Include(a => a.Persona)
                .Where(w => w.Id == id && w.SedeId == sedeId)
                .Select(a => new AsistenciaByIdRes
                {
                    NombreCompleto = $"{a.Persona.NombreCompleto}",
                    Entrada = a.HorarioEntrada,
                    Salida = a.HorarioSalida,
                    Almuerzo = a.HorarioAlmuerzo,
                    Regreso = a.HorarioRegreso,
                    Asistencias = lista
                }).FirstOrDefaultAsync();

            if (response == null)
            {
                return null;
            }
            response.Asistencias = lista;
            return response;
        }


        public async Task<Asistencia?> Crear(AsistenciaCreateReq request)
        {
            string? tipoAsistencia = request.Tipo;
            if (tipoAsistencia == null)
            {
                return null;
            }
            tipoAsistencia = tipoAsistencia.ToLower();
            TimeOnly? horaAsignada = await _context.Usuarios
            .Where(a => a.Id == request.CreadorId)
            .Select(a => tipoAsistencia.Equals("entrada")
                ? a.HorarioEntrada     // Si es "entrada", selecciona HorarioEntrada
                : (tipoAsistencia.Equals("salida")
                    ? a.HorarioSalida   // Si es "salida", selecciona HorarioSalida
                    : (tipoAsistencia.Equals("almuerzo")
                    ? a.HorarioAlmuerzo   // Si es "salida", selecciona HorarioSalida
                    : (tipoAsistencia.Equals("regreso")
                    ? a.HorarioRegreso   // Si es "salida", selecciona HorarioSalida
                    : null // Si no es ni "entrada" ni "salida", devuelve el valor por defecto
                  ) // Si no es ni "entrada" ni "salida", devuelve el valor por defecto
                  ) // Si no es ni "entrada" ni "salida", devuelve el valor por defecto
                  )).FirstOrDefaultAsync();
            if (horaAsignada == null)
            {
                return null;
            }
            TimeSpan diferencia;
            DateTime horaAsignada2 = DateTime.Today.Add(horaAsignada.GetValueOrDefault().ToTimeSpan());
            DateTime horaMarcada = DateTime.Now;
            Asistencia asistencia = _mapper.Map<Asistencia>(request);
            diferencia = (horaAsignada2 - horaMarcada).Duration();
            Debug.WriteLine(diferencia);
            if (tipoAsistencia.Trim().ToUpper().Equals("ENTRADA") && horaMarcada > horaAsignada2)
            {
                asistencia.TiempoAtraso = diferencia;
            }
            else
            {
                asistencia.TiempoAtraso = null;
            }

            if (tipoAsistencia.Trim().ToUpper().Equals("SALIDA") && horaMarcada > horaAsignada2)
            {
                asistencia.TiempoExtra = diferencia;
            }
            else
            {
                asistencia.TiempoExtra = null;
            }
            asistencia.HoraMarcada = TimeOnly.FromDateTime(horaMarcada);
            asistencia.HoraAsignada = horaAsignada;
            asistencia.ModificadorId = asistencia.CreadorId;
            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync();
            return asistencia;
        }

    }
}
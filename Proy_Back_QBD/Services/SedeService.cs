using Proy_back_QBD.Request;
using Microsoft.AspNetCore.Mvc;
using Proy_back_QBD.Data;
using Proy_back_QBD.Models;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Dto.Response;
using Microsoft.AspNetCore.Http.HttpResults;
using AutoMapper;

namespace Proy_back_QBD.Services
{
    public class SedeService : ISedeService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public SedeService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Sede?> Crear(Sede sede)
        {
            await _context.Sedes.AddAsync(sede);
            await _context.SaveChangesAsync();
            return sede;
        }

        public async Task<Sede?> Actualizar(int id, SedeUpdateReq request)
        {
            Sede? req = await _context.Sedes.FirstOrDefaultAsync(foda => foda.Id == id);
            if (req == null) return null;
            _mapper.Map(request, req);
            await _context.SaveChangesAsync();
            return req;
        }

        public async Task<List<SedeFindAllResponse?>> Obtener()
        {
            var sedes = await _context.Sedes.ToListAsync();
            var usuarios = await _context.Usuarios.Include(u => u.Persona).ToListAsync();
            var personas = await _context.Personas.ToListAsync();

            var usuarioDict = usuarios.Where(u => u.Persona != null)
                                      .ToDictionary(u => u.Id.ToString(), u => u.Persona!.NombreCompleto ?? "");
            var personaDict = personas.ToDictionary(p => p.Id.ToString(), p => p.NombreCompleto ?? "");

            List<SedeFindAllResponse> response = sedes.Select(a => {
                string encargadoId = a.Encargado ?? "";
                string encargadoNombre = "";

                if (!string.IsNullOrWhiteSpace(a.Encargado))
                {
                    string key = a.Encargado.Trim();
                    // Prioridad 1: Buscar por ID de usuario (clave guardada al seleccionar el responsable)
                    if (usuarioDict.TryGetValue(key, out var nombreUsuario) && !string.IsNullOrWhiteSpace(nombreUsuario))
                    {
                        encargadoNombre = nombreUsuario;
                    }
                    // Prioridad 2: Buscar por ID de persona por compatibilidad histórica
                    else if (personaDict.TryGetValue(key, out var nombrePersona) && !string.IsNullOrWhiteSpace(nombrePersona))
                    {
                        encargadoNombre = nombrePersona;
                    }
                    else
                    {
                        encargadoNombre = key;
                    }
                }

                return new SedeFindAllResponse
                {
                    Id = a.Id,
                    Nombre = a.Nombre,
                    Direccion = a.Direccion,
                    Encargado = encargadoId,
                    EncargadoId = encargadoId,
                    EncargadoNombre = encargadoNombre,
                    Telefono = a.Telefono,
                };
            }).ToList();

            return response;
        }

        public async Task<SedeFindAllResponse?> ObtenerPorId(int id)
        {
            var a = await _context.Sedes.FirstOrDefaultAsync(s => s.Id == id);
            if (a == null) return null;

            string encargadoId = a.Encargado ?? "";
            string encargadoNombre = "";

            if (!string.IsNullOrWhiteSpace(a.Encargado))
            {
                string key = a.Encargado.Trim();
                var usuario = await _context.Usuarios.Include(u => u.Persona).FirstOrDefaultAsync(u => u.Id.ToString() == key);
                if (usuario?.Persona?.NombreCompleto != null)
                {
                    encargadoNombre = usuario.Persona.NombreCompleto;
                }
                else
                {
                    var persona = await _context.Personas.FirstOrDefaultAsync(p => p.Id.ToString() == key);
                    encargadoNombre = persona?.NombreCompleto ?? key;
                }
            }

            return new SedeFindAllResponse
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Direccion = a.Direccion,
                Encargado = encargadoId,
                EncargadoId = encargadoId,
                EncargadoNombre = encargadoNombre,
                Telefono = a.Telefono,
            };
        }

        public async Task<GeneralRes?> ObtGeneral(int id)
        {
            GeneralRes? response = await _context.Sedes
                        .Where(w => w.Id == id)
                        .Select(s => new GeneralRes
                        {
                            Meta = s.Meta,
                            MsgGpt = s.MsgGpt,
                            MsgCumple = s.MsgCumple,
                            MsgSeguimiento = s.MsgSeguimiento,
                            MsgProceso = s.MsgEnProceso,
                            MsgTerminado = s.MsgTerminado
                        }).FirstOrDefaultAsync();

            if (response == null)
            {
                return null;
            }

            return response;
        }

        public async Task<string?> ActualizarGeneral(int id, GeneralReq request)
        {
            var entidad = await _context.Sedes.FirstOrDefaultAsync(f => f.Id == id);
            if (entidad == null)
            {
                return null;
            }
            if (request.MsgSeguimiento != null)
            {
                entidad.MsgSeguimiento = request.MsgSeguimiento;
            }
            if (request.MsgTerminado != null)
            {
                entidad.MsgTerminado = request.MsgTerminado;
            }
            if (request.MsgCumple != null)
            {
                entidad.MsgCumple = request.MsgCumple;
            }
            if (request.MsgCumple != null)
            {
                entidad.MsgEnProceso = request.MsgEnProceso;
            }
            if (request.MsgGpt != null)
            {
                entidad.MsgGpt = request.MsgGpt;
            }
            if (request.Meta != null)
            {
                entidad.Meta = request.Meta;
            }

            _context.Sedes.Update(entidad);
            await _context.SaveChangesAsync();

            return "registro exitoso";
        }

    }
}

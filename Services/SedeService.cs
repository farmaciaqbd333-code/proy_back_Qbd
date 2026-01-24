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
            _mapper.Map(request, req);
            await _context.SaveChangesAsync();
            return req;
        }

        public async Task<List<SedeFindAllResponse?>> Obtener()
        {
            List<SedeFindAllResponse>? response = await _context.Sedes
            .Select(a => new SedeFindAllResponse
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Direccion = a.Direccion,
                Encargado = a.Encargado,
                Telefono = a.Telefono,
            }).ToListAsync();
            return response;
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
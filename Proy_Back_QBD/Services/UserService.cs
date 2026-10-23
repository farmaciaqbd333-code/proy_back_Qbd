using proy_back_Qbd.Exceptions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.Request;
using Proy_back_QBD.Dto.Response;
using Proy_back_QBD.Models;
using Proy_back_QBD.Request;
using Proy_back_QBD.Security;

namespace Proy_back_QBD.Services
{
    public class UserService : IUserService
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public UserService(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<UsuarioLoginDataRes?> ValidarLogin(string codigo, string contrasena)
        {
            var codigoLimpio = (codigo ?? string.Empty).Trim();
            if (codigoLimpio.Length == 0 || string.IsNullOrEmpty(contrasena))
            {
                return null;
            }

            Usuario? usuario = await _context.Usuarios
                .Include(a => a.Persona)
                .Include(a => a.Sede)
                .Include(a => a.Tipo)
                .FirstOrDefaultAsync(a => a.Codigo != null && a.Codigo.Trim() == codigoLimpio && a.Estado == true);

            if (usuario == null || !Contrasenas.Verificar(contrasena, usuario.Contrasena))
            {
                return null;
            }

            // Contraseñas sin cifrar (decisión del negocio): si alguna quedó cifrada,
            // se vuelve a guardar en texto para que el administrador pueda verla.
            if (Contrasenas.EsHash(usuario.Contrasena))
            {
                usuario.Contrasena = contrasena.Trim();
                await _context.SaveChangesAsync();
            }

            return new UsuarioLoginDataRes
            {
                NombreCompleto = usuario.Persona?.NombreCompleto,
                TipoUsuario = usuario.Tipo?.Nombre,
                TipoId = usuario.Tipo?.Id ?? usuario.TipoId,
                Sede = usuario.Sede?.Nombre,
                Dni = usuario.Persona?.Dni,
                SedeId = usuario.Sede?.Id ?? usuario.SedeId,
                Id = usuario.Id,
                IdGeneral = usuario.IdGeneral,
                Estado = usuario.Estado,
                HorarioSalida = usuario.HorarioSalida,
            };
        }
        public async Task<Usuario?> Crear(UsuarioCreateReq request)
        {
            Persona persona = _mapper.Map<Persona>(request.Persona);
            persona.ModificadorId = persona.CreadorId;
            await _context.Personas.AddAsync(persona);
            await _context.SaveChangesAsync();
            Usuario usuario = _mapper.Map<Usuario>(request);
            if (string.IsNullOrWhiteSpace(usuario.Contrasena))
            {
                throw new BadRequestException("La contraseña es obligatoria.");
            }
            usuario.Contrasena = usuario.Contrasena.Trim();
            usuario.PersonaId = persona.Id;
            usuario.Modificador = persona.Creador;
            usuario.Codigo = $"{ObtenerIniciales(persona.NombreCompleto)}{persona.FechaNacimiento:ddMM}";
            await _context.Usuarios.AddAsync(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }
        public async Task<Usuario?> Eliminar(int id)
        {
            Usuario? usuario = await _context.Usuarios.FirstOrDefaultAsync(a => a.Id == id);
            if (usuario == null)
            {
                return null;
            }
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<Usuario?> Actualizar(int id, UsuarioUpdateReq request)
        {
            Usuario? usuario = await _context.Usuarios
            .Include(a => a.Persona)
            .FirstOrDefaultAsync(a => a.Id == id);
            if (usuario == null)
            {
                return null;
            }
            var contrasenaAnterior = usuario.Contrasena;
            _mapper.Map(request, usuario);
            // Contraseña vacía = no se cambia; una nueva se guarda tal cual
            usuario.Contrasena = string.IsNullOrWhiteSpace(request.Contrasena)
                ? contrasenaAnterior
                : request.Contrasena.Trim();
            usuario.Codigo = $"{ObtenerIniciales(usuario.Persona.NombreCompleto)}{usuario.Persona.FechaNacimiento:ddMM}";
            _mapper.Map(request.Persona, usuario.Persona);
            await _context.SaveChangesAsync();
            return usuario;
        }

        public async Task<List<UsuarioListaRes>?> Listar()
        {
            List<UsuarioListaRes>? response = await _context.Usuarios
            .Where(a => a.TipoId != 1)
            .Include(a => a.Tipo)
            .Include(a => a.Sede)
            .Select(a => new UsuarioListaRes
            {
                Id = a.Id,
                IdGeneral = a.IdGeneral,
                Contrasena = a.Contrasena, // visible para quien gestiona usuarios (el controlador la oculta a los demás)
                HorarioEntrada = a.HorarioEntrada,
                HorarioAlmuerzo = a.HorarioAlmuerzo,
                HorarioRegreso = a.HorarioRegreso,
                HorarioSalida = a.HorarioSalida,
                CQFP = a.CQFP,
                TipoUsuario = a.Tipo.Nombre,
                TipoUsuarioId = a.TipoId,
                Codigo = a.Codigo,
                Estado = a.Estado,
                PersonaRes = new PersonaRes
                {
                    Id = a.Persona.Id,
                    NombreCompleto = a.Persona.NombreCompleto,
                    FechaNacimiento = a.Persona.FechaNacimiento,
                    Dni = a.Persona.Dni,
                    Sede = a.Sede.Nombre,
                    SedeId = a.SedeId,
                    Telefono = a.Persona.Telefono,
                }
            })
            .ToListAsync();
            return response;
        }
        public async Task<UsuarioByIdRes?> ObtenerById(int id)
        {
            UsuarioByIdRes? response = await _context.Usuarios
            .Where(a => a.Id == id)
            .Include(a => a.Tipo)
            .Include(a => a.Sede)
            .Select(a => new UsuarioByIdRes
            {
                Id = a.Id,
                IdGeneral = a.IdGeneral,
                Contrasena = a.Contrasena, // visible para quien gestiona usuarios (el controlador la oculta a los demás)
                HorarioEntrada = a.HorarioEntrada,
                HorarioAlmuerzo = a.HorarioAlmuerzo,
                HorarioRegreso = a.HorarioRegreso,
                HorarioSalida = a.HorarioSalida,
                CQFP = a.CQFP,
                TipoUsuario = a.Tipo.Nombre,
                Codigo = a.Codigo,
                Estado = a.Estado,
                PersonaRes = new PersonaRes
                {
                    Id = a.Persona.Id,
                    NombreCompleto = a.Persona.NombreCompleto,
                    FechaNacimiento = a.Persona.FechaNacimiento,
                    Dni = a.Persona.Dni,
                    Sede = a.Sede.Nombre,
                    Telefono = a.Persona.Telefono,
                }
            })
            .FirstOrDefaultAsync();
            return response;
        }
        private string ObtenerIniciales(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                return string.Empty;

            var palabras = nombreCompleto
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return string.Concat(palabras.Select(p => char.ToUpper(p[0])));
        }

        public async Task<List<AutorizadoEla>?> Lista2(int sedeId)
        {
            var sede = await _context.Sedes.FirstOrDefaultAsync(s => s.Id == sedeId);
            bool esCentral = sedeId == 15 || (sede != null && sede.Nombre != null && sede.Nombre.ToUpper().Contains("CENTRAL"));

            var query = _context.Usuarios
                .Include(i => i.Persona)
                .AsQueryable();

            if (esCentral)
            {
                // Incluir usuarios que:
                // 1. Pertenezcan a la sede Central directamente (SedeId == sedeId)
                // 2. O que les hayan dado acceso a Central (IdGeneral == 1 => tiene acceso a módulo central)
                // 3. O que sean administradores (TipoId == 1)
                query = query.Where(w => w.SedeId == sedeId || w.IdGeneral == 1 || w.TipoId == 1);
            }
            else
            {
                query = query.Where(w => w.SedeId == sedeId);
            }

            List<AutorizadoEla>? lista = await query
                .Where(w => w.Estado == true && w.Persona != null)
                .Select(s => new AutorizadoEla
                {
                    Id = s.Id,
                    Rol = s.TipoId,
                    NombreCompleto = s.Persona.NombreCompleto,
                })
                .ToListAsync();

            if (lista == null)
            {
                return null;
            }
            return lista;
        }

        public async Task<Usuario?> CambiarEstado(int id, bool estado)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return null;
            }

            usuario.Estado = estado;
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }
    }
}
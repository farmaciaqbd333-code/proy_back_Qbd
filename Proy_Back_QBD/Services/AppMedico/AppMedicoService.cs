using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using proy_back_Qbd.Exceptions;
using Proy_back_QBD.Data;
using Proy_back_QBD.Dto.AppMedico;
using Proy_back_QBD.Models;
using Proy_back_QBD.Models.AppMedico;
using Proy_back_QBD.Security;

namespace Proy_back_QBD.Services.AppMedico
{
    /// <summary>Sesión de un médico tomada de su token.</summary>
    public readonly record struct SesionMedico(int AccesoId, int Version);

    public interface IAppMedicoService
    {
        // Médico (app móvil)
        Task<MedicoLoginRes> Login(MedicoLoginReq request);
        Task<MedicoPerfilRes> Perfil(SesionMedico sesion);
        Task<MedicoLoginRes> CambiarContrasena(SesionMedico sesion, MedicoCambiarContrasenaReq request);
        Task<CatalogoRes> Catalogo(SesionMedico sesion, string? search, string? especialidad, string? categoria, bool soloFavoritos, int pagina, int tamano, bool soloAsignados = false);
        Task<List<string>> EspecialidadesCatalogo(SesionMedico sesion);
        Task MarcarFavorito(SesionMedico sesion, int productoId, bool favorito);
        Task<List<MedicoSedeRes>> Sedes(SesionMedico sesion);
        Task<PedidoMedicoRes> CrearPedido(SesionMedico sesion, PedidoMedicoCreateReq request);
        Task<List<PedidoMedicoRes>> MisPedidos(SesionMedico sesion);
        Task<PedidoMedicoRes> MiPedido(SesionMedico sesion, int id);
        Task<PedidoMedicoRes> AnularMiPedido(SesionMedico sesion, int id);

        // Personal QBD (sistema web)
        Task<MedicoAccesoAsignarRes> AsignarAcceso(int medicoId, string? contrasena, int usuarioId);
        Task CambiarActivoAcceso(int medicoId, bool activo, int usuarioId);
        Task<List<MedicoAccesoEstadoRes>> Accesos();
        Task<List<PedidoMedicoRes>> ListarPedidos(int? sedeId, string? estado, int? dias);
        Task<int> ContarPendientes(int? sedeId);
        Task<PedidoMedicoRes> CambiarEstado(int id, PedidoMedicoEstadoReq request, int usuarioId);
        Task<PacienteDelMedicoRes> PacienteDelMedico(int pedidoMedicoId, int sedeId, int usuarioId);
    }

    /// <summary>Excepción que el middleware traduce a 401 (sesión del médico no válida).</summary>
    public class NoAutorizadoException : Exception
    {
        public NoAutorizadoException(string message) : base(message) { }
    }

    public class AppMedicoService : IAppMedicoService
    {
        private readonly ApiContext _context;
        private readonly string _secreto;

        public AppMedicoService(ApiContext context, IOptions<JwtOptions> jwt)
        {
            _context = context;
            _secreto = jwt.Value.Secret;
        }

        private static DateTime Ahora() => EstadoTurno.AhoraLima();

        // ===================== Datos del médico =====================

        /// <summary>Filas del directorio (una por sede) con ese CMP, la más antigua primero.</summary>
        private async Task<List<FilaMedicoCmp>> FilasDelCmp(string cmpNormal)
        {
            if (cmpNormal.Length == 0) return new();
            var filas = await AppMedicoConsultas.FilasDelCmp(_context, cmpNormal).ToListAsync();
            return filas.Where(f => AppMedicoReglas.NormalizarCmp(f.Cmp) == cmpNormal).OrderBy(f => f.Id).ToList();
        }

        private async Task<MedicoAcceso> Sesion(SesionMedico sesion)
        {
            var acceso = await _context.Set<MedicoAcceso>().FirstOrDefaultAsync(a => a.Id == sesion.AccesoId);
            if (acceso == null || !acceso.Activo || acceso.VersionToken != sesion.Version)
                throw new NoAutorizadoException("Tu sesión ya no es válida. Vuelve a iniciar sesión.");
            return acceso;
        }

        private async Task<(MedicoAcceso Acceso, List<FilaMedicoCmp> Filas)> SesionConFilas(SesionMedico sesion)
        {
            var acceso = await Sesion(sesion);
            var filas = await FilasDelCmp(acceso.Cmp);
            if (filas.Count == 0)
                throw new NoAutorizadoException("Tu CMP ya no figura en el directorio de médicos de QBD.");
            return (acceso, filas);
        }

        private async Task<List<string>> CodigosEspecialidadLista()
        {
            var filas = await _context.ListaPrecios.AsNoTracking().Where(p => p.Activo)
                .Select(p => new { p.Especialidad1, p.Especialidad2, p.Especialidad3 }).ToListAsync();
            return filas.SelectMany(f => new[] { f.Especialidad1, f.Especialidad2, f.Especialidad3 })
                .Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!.Trim())
                .GroupBy(e => e).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
        }

        private async Task<MedicoPerfilRes> ArmarPerfil(MedicoAcceso acceso, List<FilaMedicoCmp> filas)
        {
            var principal = filas[0];
            return new MedicoPerfilRes
            {
                Cmp = acceso.Cmp,
                MedicoId = principal.Id,
                Nombre = (principal.Nombre ?? "").Trim(),
                Especialidad = principal.Especialidad,
                EspecialidadesSugeridas = AppMedicoReglas.EspecialidadesSugeridas(principal.Especialidad, await CodigosEspecialidadLista()),
                SedeId = principal.SedeId > 0 ? principal.SedeId : null,
                DebeCambiarContrasena = acceso.DebeCambiar,
            };
        }

        private async Task<MedicoLoginRes> RespuestaLogin(MedicoAcceso acceso, List<FilaMedicoCmp> filas)
        {
            var perfil = await ArmarPerfil(acceso, filas);
            var (token, expira) = AppMedicoTokens.Generar(acceso.Id, acceso.Cmp, acceso.VersionToken, perfil.Nombre, _secreto);
            return new MedicoLoginRes { Token = token, Expira = expira, Medico = perfil };
        }

        // ===================== Login y contraseña =====================

        public async Task<MedicoLoginRes> Login(MedicoLoginReq request)
        {
            const string incorrecto = "CMP o contraseña incorrectos.";
            var cmp = AppMedicoReglas.NormalizarCmp(request?.Cmp);
            if (cmp.Length == 0 || string.IsNullOrWhiteSpace(request?.Contrasena))
                throw new BadRequestException("Ingresa tu CMP y tu contraseña.");

            var acceso = await _context.Set<MedicoAcceso>().FirstOrDefaultAsync(a => a.Cmp == cmp);
            if (acceso == null) throw new NoAutorizadoException(incorrecto);

            var ahora = Ahora();
            if (acceso.BloqueadoHasta != null && acceso.BloqueadoHasta > ahora)
                throw new NoAutorizadoException("Demasiados intentos fallidos. Intenta de nuevo en unos minutos.");

            if (!AppMedicoReglas.Verificar(request!.Contrasena, acceso.ContrasenaHash))
            {
                acceso.IntentosFallidos++;
                if (acceso.IntentosFallidos >= AppMedicoReglas.MaxIntentos)
                {
                    acceso.BloqueadoHasta = ahora.Add(AppMedicoReglas.Bloqueo);
                    acceso.IntentosFallidos = 0;
                }
                await _context.SaveChangesAsync();
                throw new NoAutorizadoException(incorrecto);
            }

            if (!acceso.Activo)
                throw new NoAutorizadoException("Tu acceso a la app está desactivado. Comunícate con QBD.");

            var filas = await FilasDelCmp(cmp);
            if (filas.Count == 0)
                throw new NoAutorizadoException("Tu CMP no figura en el directorio de médicos de QBD.");

            acceso.IntentosFallidos = 0;
            acceso.BloqueadoHasta = null;
            acceso.UltimoIngreso = ahora;
            await _context.SaveChangesAsync();

            return await RespuestaLogin(acceso, filas);
        }

        public async Task<MedicoPerfilRes> Perfil(SesionMedico sesion)
        {
            var (acceso, filas) = await SesionConFilas(sesion);
            return await ArmarPerfil(acceso, filas);
        }

        public async Task<MedicoLoginRes> CambiarContrasena(SesionMedico sesion, MedicoCambiarContrasenaReq request)
        {
            var (acceso, filas) = await SesionConFilas(sesion);
            if (!AppMedicoReglas.Verificar(request?.Actual, acceso.ContrasenaHash))
                throw new BadRequestException("La contraseña actual no es correcta.");
            var error = AppMedicoReglas.ValidarContrasena(request?.Nueva);
            if (error != null) throw new BadRequestException(error);
            if (request!.Nueva!.Trim() == request.Actual!.Trim())
                throw new BadRequestException("La contraseña nueva debe ser distinta de la actual.");

            acceso.ContrasenaHash = AppMedicoReglas.Hashear(request.Nueva);
            acceso.DebeCambiar = false;
            acceso.VersionToken++; // cierra la sesión en otros teléfonos
            acceso.FechaModificacion = Ahora();
            acceso.ModificadorId = null;
            await _context.SaveChangesAsync();

            return await RespuestaLogin(acceso, filas);
        }

        // ===================== Catálogo =====================

        public async Task<CatalogoRes> Catalogo(SesionMedico sesion, string? search, string? especialidad, string? categoria,
            bool soloFavoritos, int pagina, int tamano, bool soloAsignados = false)
        {
            var acceso = await Sesion(sesion);
            pagina = Math.Max(1, pagina);
            tamano = tamano <= 0 ? 50 : Math.Min(tamano, 200);

            var favoritos = (await _context.Set<MedicoProductoFavorito>().AsNoTracking()
                .Where(f => f.Cmp == acceso.Cmp).Select(f => f.ListaPrecioId).ToListAsync()).ToHashSet();
            var asignados = await ProductosAsignados(acceso.Cmp);

            HashSet<int>? soloIds = null;
            if (soloFavoritos) soloIds = new HashSet<int>(favoritos);
            if (soloAsignados)
            {
                if (soloIds == null) soloIds = new HashSet<int>(asignados);
                else soloIds.IntersectWith(asignados);
            }

            var query = AppMedicoConsultas.Catalogo(_context, search, especialidad, categoria, soloIds);

            int total = await query.CountAsync();
            var filas = await query.OrderBy(p => p.Producto).ThenBy(p => p.Id)
                .Skip((pagina - 1) * tamano).Take(tamano).ToListAsync();

            return new CatalogoRes
            {
                Total = total,
                Pagina = pagina,
                TamanoPagina = tamano,
                Items = filas.Select(p => AItem(p, favoritos.Contains(p.Id), asignados.Contains(p.Id))).ToList(),
            };
        }

        /// <summary>
        /// Productos que QBD le asignó al médico desde Lista de Precios: por su CMP o por cualquiera
        /// de sus filas del directorio (una por sede).
        /// </summary>
        private async Task<HashSet<int>> ProductosAsignados(string cmpNormal)
        {
            var filaIds = (await FilasDelCmp(cmpNormal)).Select(f => f.Id).ToList();
            return (await _context.Set<MedicoProductoAsignado>().AsNoTracking()
                .Where(a => a.Cmp == cmpNormal || filaIds.Contains(a.MedicoId))
                .Select(a => a.ListaPrecioId).Distinct().ToListAsync()).ToHashSet();
        }

        private static CatalogoItemRes AItem(ListaPrecio p, bool favorito, bool asignado = false) => new()
        {
            Id = p.Id,
            Codigo = p.CodigoA,
            Producto = p.Producto,
            Ifas = p.Ifas,
            FormaFarmaceutica = p.FormaFarmaceutica,
            Enfermedades = p.Enfermedades,
            Indicaciones = p.Indicaciones,
            Presentacion = p.Presentacion,
            Categoria = p.Categoria,
            Especialidades = new[] { p.Especialidad1, p.Especialidad2, p.Especialidad3 }
                .Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!.Trim()).ToList(),
            Precio = p.PrecioA, // el médico ve la Lista A (MED)
            Favorito = favorito,
            Asignado = asignado,
        };

        public async Task<List<string>> EspecialidadesCatalogo(SesionMedico sesion)
        {
            await Sesion(sesion);
            return await CodigosEspecialidadLista();
        }

        public async Task MarcarFavorito(SesionMedico sesion, int productoId, bool favorito)
        {
            var acceso = await Sesion(sesion);
            var set = _context.Set<MedicoProductoFavorito>();
            var existente = await set.FirstOrDefaultAsync(f => f.Cmp == acceso.Cmp && f.ListaPrecioId == productoId);
            if (favorito && existente == null)
            {
                if (!await _context.ListaPrecios.AnyAsync(p => p.Id == productoId && p.Activo))
                    throw new NotFoundException("El producto no existe o ya no está en la lista.");
                set.Add(new MedicoProductoFavorito { Cmp = acceso.Cmp, ListaPrecioId = productoId, FechaCreacion = Ahora() });
                await _context.SaveChangesAsync();
            }
            else if (!favorito && existente != null)
            {
                set.Remove(existente);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<MedicoSedeRes>> Sedes(SesionMedico sesion)
        {
            var (_, filas) = await SesionConFilas(sesion);
            var registradas = filas.Select(f => f.SedeId).ToHashSet();
            var sedes = await _context.Sedes.AsNoTracking().OrderBy(s => s.Id)
                .Select(s => new { s.Id, s.Nombre }).ToListAsync();
            return sedes
                .Select(s => new MedicoSedeRes { Id = s.Id, Nombre = (s.Nombre ?? $"Sede {s.Id}").Trim(), Registrado = registradas.Contains(s.Id) })
                .OrderByDescending(s => s.Registrado).ThenBy(s => s.Id)
                .ToList();
        }

        // ===================== Pedidos del médico =====================

        public async Task<PedidoMedicoRes> CrearPedido(SesionMedico sesion, PedidoMedicoCreateReq request)
        {
            var (acceso, filas) = await SesionConFilas(sesion);
            if (request?.Items == null || request.Items.Count == 0)
                throw new BadRequestException("El pedido no tiene productos.");
            if (request.Items.Count > AppMedicoReglas.MaxItemsPorPedido)
                throw new BadRequestException($"Un pedido puede tener como máximo {AppMedicoReglas.MaxItemsPorPedido} productos.");
            if (request.Items.Any(i => i.Cantidad < 1 || i.Cantidad > AppMedicoReglas.MaxCantidad))
                throw new BadRequestException($"La cantidad de cada producto debe estar entre 1 y {AppMedicoReglas.MaxCantidad}.");
            if (request.Items.Any(i => (i.Observacion?.Length ?? 0) > 500))
                throw new BadRequestException("La observación de un producto no puede pasar de 500 caracteres.");
            if ((request.Observacion?.Length ?? 0) > 1000)
                throw new BadRequestException("La observación del pedido no puede pasar de 1000 caracteres.");

            var nombrePaciente = string.IsNullOrWhiteSpace(request.PacienteNombre) ? null : request.PacienteNombre.Trim();
            if ((nombrePaciente?.Length ?? 0) > 200)
                throw new BadRequestException("El nombre del paciente es demasiado largo.");
            var dni = string.IsNullOrWhiteSpace(request.PacienteDni) ? null : request.PacienteDni.Trim();
            if (dni != null && !AppMedicoReglas.DocumentoValido(dni))
                throw new BadRequestException("El documento del paciente debe tener entre 8 y 12 letras o números (DNI: 8 dígitos).");

            int sedeId = request.SedeId ?? filas[0].SedeId;
            if (!await _context.Sedes.AnyAsync(s => s.Id == sedeId))
                throw new BadRequestException("Elige una sede válida.");
            var medicoId = (filas.FirstOrDefault(f => f.SedeId == sedeId) ?? filas[0]).Id;

            // Precios SIEMPRE desde la base (Lista A), nunca desde la app
            var ids = request.Items.Select(i => i.ProductoId).Distinct().ToList();
            var productos = await _context.ListaPrecios.AsNoTracking()
                .Where(p => ids.Contains(p.Id) && p.Activo).ToDictionaryAsync(p => p.Id);
            var faltan = ids.Where(id => !productos.ContainsKey(id)).ToList();
            if (faltan.Count > 0)
                throw new BadRequestException("Algunos productos ya no están disponibles. Actualiza el catálogo y vuelve a intentar.");

            var ahora = Ahora();
            var pedido = new PedidoMedico
            {
                MedicoId = medicoId,
                Cmp = acceso.Cmp,
                SedeId = sedeId,
                PacienteNombre = nombrePaciente,
                PacienteDni = dni,
                Observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim(),
                Estado = EstadosPedidoMedico.Pendiente,
                FechaCreacion = ahora,
                FechaModificacion = ahora,
            };
            foreach (var item in request.Items)
            {
                var p = productos[item.ProductoId];
                var precio = p.PrecioA ?? 0m;
                pedido.Detalles.Add(new PedidoMedicoDetalle
                {
                    ListaPrecioId = p.Id,
                    Codigo = p.CodigoA,
                    Producto = p.Producto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precio,
                    Subtotal = precio * item.Cantidad,
                    Observacion = string.IsNullOrWhiteSpace(item.Observacion) ? null : item.Observacion.Trim(),
                });
            }
            pedido.Total = pedido.Detalles.Sum(d => d.Subtotal);

            _context.Set<PedidoMedico>().Add(pedido);
            await _context.SaveChangesAsync();
            return await Detalle(pedido.Id);
        }

        public async Task<List<PedidoMedicoRes>> MisPedidos(SesionMedico sesion)
        {
            var acceso = await Sesion(sesion);
            return await Listar(_context.Set<PedidoMedico>().Where(p => p.Cmp == acceso.Cmp), conDetalles: true, limite: 200);
        }

        public async Task<PedidoMedicoRes> MiPedido(SesionMedico sesion, int id)
        {
            var acceso = await Sesion(sesion);
            if (!await _context.Set<PedidoMedico>().AnyAsync(p => p.Id == id && p.Cmp == acceso.Cmp))
                throw new NotFoundException("No se encontró el pedido.");
            return await Detalle(id);
        }

        public async Task<PedidoMedicoRes> AnularMiPedido(SesionMedico sesion, int id)
        {
            var acceso = await Sesion(sesion);
            var pedido = await _context.Set<PedidoMedico>().FirstOrDefaultAsync(p => p.Id == id && p.Cmp == acceso.Cmp)
                ?? throw new NotFoundException("No se encontró el pedido.");
            if (pedido.Estado != EstadosPedidoMedico.Pendiente)
                throw new BadRequestException("Solo puedes anular un pedido que QBD aún no ha recibido. Comunícate con la farmacia.");
            pedido.Estado = EstadosPedidoMedico.Anulado;
            pedido.Respuesta = "Anulado por el médico.";
            pedido.FechaModificacion = Ahora();
            await _context.SaveChangesAsync();
            return await Detalle(id);
        }

        // ===================== Lectura común =====================

        private async Task<PedidoMedicoRes> Detalle(int id) =>
            (await Listar(_context.Set<PedidoMedico>().Where(p => p.Id == id), conDetalles: true, limite: 1)).FirstOrDefault()
            ?? throw new NotFoundException("No se encontró el pedido.");

        private async Task<List<PedidoMedicoRes>> Listar(IQueryable<PedidoMedico> query, bool conDetalles, int limite)
        {
            var pedidos = await query.AsNoTracking()
                .OrderByDescending(p => p.FechaCreacion).ThenByDescending(p => p.Id)
                .Take(limite)
                .Select(p => new PedidoMedicoRes
                {
                    Id = p.Id,
                    Estado = p.Estado,
                    Respuesta = p.Respuesta,
                    FechaCreacion = p.FechaCreacion,
                    FechaModificacion = p.FechaModificacion,
                    SedeId = p.SedeId,
                    Sede = p.Sede != null ? p.Sede.Nombre : null,
                    PacienteNombre = p.PacienteNombre,
                    PacienteDni = p.PacienteDni,
                    Observacion = p.Observacion,
                    Total = p.Total,
                    CantidadItems = p.Detalles.Count,
                    PedidoId = p.PedidoId,
                    MedicoId = p.MedicoId,
                    Cmp = p.Cmp,
                    Medico = p.Medico != null && p.Medico.Persona != null ? p.Medico.Persona.NombreCompleto : null,
                    Especialidad = p.Medico != null && p.Medico.Especialidad != null ? p.Medico.Especialidad.Nombre : null,
                })
                .ToListAsync();

            // Pedidos ya convertidos: el estado lo da el pedido de Gestión de Pedidos (laboratorio, entrega...)
            var enlazados = pedidos.Where(p => p.PedidoId is > 0).Select(p => p.PedidoId!.Value).Distinct().ToList();
            if (enlazados.Count > 0)
            {
                var estadosReales = await _context.Pedidos.AsNoTracking()
                    .Where(x => enlazados.Contains(x.Id))
                    .Select(x => new { x.Id, x.Estado })
                    .ToDictionaryAsync(x => x.Id, x => x.Estado);
                foreach (var p in pedidos)
                {
                    if (p.PedidoId is not > 0 || !estadosReales.TryGetValue(p.PedidoId.Value, out var real)) continue;
                    p.PedidoEstado = real;
                    p.Estado = EstadosPedidoMedico.DesdePedido(real, p.Estado);
                }
            }

            if (conDetalles && pedidos.Count > 0)
            {
                var ids = pedidos.Select(p => p.Id).ToList();
                var detalles = await _context.Set<PedidoMedicoDetalle>().AsNoTracking()
                    .Where(d => ids.Contains(d.PedidoMedicoId))
                    .OrderBy(d => d.Id)
                    .ToListAsync();
                var porPedido = detalles.ToLookup(d => d.PedidoMedicoId);
                foreach (var p in pedidos)
                {
                    p.Detalles = porPedido[p.Id].Select(d => new PedidoMedicoDetalleRes
                    {
                        Id = d.Id,
                        ProductoId = d.ListaPrecioId,
                        Codigo = d.Codigo,
                        Producto = d.Producto,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Subtotal = d.Subtotal,
                        Observacion = d.Observacion,
                    }).ToList();
                }
            }
            foreach (var p in pedidos) p.Medico = p.Medico?.Trim();
            return pedidos;
        }

        // ===================== Personal QBD =====================

        public async Task<MedicoAccesoAsignarRes> AsignarAcceso(int medicoId, string? contrasena, int usuarioId)
        {
            var medico = await _context.Medicos.AsNoTracking().FirstOrDefaultAsync(m => m.Id == medicoId)
                ?? throw new NotFoundException("No se encontró el médico.");
            var cmp = AppMedicoReglas.NormalizarCmp(medico.Cmp);
            if (cmp.Length == 0)
                throw new BadRequestException("El médico no tiene CMP registrado: primero completa su CMP en el directorio.");

            string? temporal = null;
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                temporal = AppMedicoReglas.GenerarTemporal();
                contrasena = temporal;
            }
            var error = AppMedicoReglas.ValidarContrasena(contrasena);
            if (error != null) throw new BadRequestException(error);

            var ahora = Ahora();
            var set = _context.Set<MedicoAcceso>();
            var acceso = await set.FirstOrDefaultAsync(a => a.Cmp == cmp);
            bool nuevo = acceso == null;
            if (acceso == null)
            {
                acceso = new MedicoAcceso { Cmp = cmp, FechaCreacion = ahora, VersionToken = 1 };
                set.Add(acceso);
            }
            else
            {
                acceso.VersionToken++; // cierra las sesiones abiertas con la contraseña anterior
            }
            acceso.ContrasenaHash = AppMedicoReglas.Hashear(contrasena!);
            acceso.Activo = true;
            acceso.DebeCambiar = true;
            acceso.IntentosFallidos = 0;
            acceso.BloqueadoHasta = null;
            acceso.FechaModificacion = ahora;
            acceso.ModificadorId = usuarioId > 0 ? usuarioId : null;
            await _context.SaveChangesAsync();

            return new MedicoAccesoAsignarRes
            {
                Cmp = cmp,
                ContrasenaTemporal = temporal,
                Message = nuevo
                    ? "Acceso a la app creado. El médico deberá cambiar la contraseña al entrar."
                    : "Contraseña restablecida. El médico deberá cambiarla al entrar.",
            };
        }

        public async Task CambiarActivoAcceso(int medicoId, bool activo, int usuarioId)
        {
            var medico = await _context.Medicos.AsNoTracking().FirstOrDefaultAsync(m => m.Id == medicoId)
                ?? throw new NotFoundException("No se encontró el médico.");
            var cmp = AppMedicoReglas.NormalizarCmp(medico.Cmp);
            var acceso = await _context.Set<MedicoAcceso>().FirstOrDefaultAsync(a => a.Cmp == cmp)
                ?? throw new NotFoundException("El médico aún no tiene acceso a la app.");
            if (acceso.Activo == activo) return;
            acceso.Activo = activo;
            if (!activo) acceso.VersionToken++;
            acceso.FechaModificacion = Ahora();
            acceso.ModificadorId = usuarioId > 0 ? usuarioId : null;
            await _context.SaveChangesAsync();
        }

        public Task<List<MedicoAccesoEstadoRes>> Accesos() =>
            _context.Set<MedicoAcceso>().AsNoTracking().OrderBy(a => a.Cmp)
                .Select(a => new MedicoAccesoEstadoRes { Cmp = a.Cmp, Activo = a.Activo, DebeCambiar = a.DebeCambiar, UltimoIngreso = a.UltimoIngreso })
                .ToListAsync();

        public async Task<List<PedidoMedicoRes>> ListarPedidos(int? sedeId, string? estado, int? dias)
        {
            var query = _context.Set<PedidoMedico>().AsQueryable();
            if (sedeId is > 0) query = query.Where(p => p.SedeId == sedeId);
            var e = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToUpperInvariant();
            // Los convertidos toman el estado del pedido real: se filtran después de calcularlo
            if (e != null) query = query.Where(p => p.Estado == e || p.PedidoId != null);
            if (dias is > 0)
            {
                var desde = Ahora().AddDays(-dias.Value);
                query = query.Where(p => p.FechaCreacion >= desde);
            }
            var lista = await Listar(query, conDetalles: true, limite: 500);
            return e == null ? lista : lista.Where(p => p.Estado == e).ToList();
        }

        public Task<int> ContarPendientes(int? sedeId)
        {
            var query = _context.Set<PedidoMedico>().Where(p => p.Estado == EstadosPedidoMedico.Pendiente);
            if (sedeId is > 0) query = query.Where(p => p.SedeId == sedeId);
            return query.CountAsync();
        }

        public async Task<PedidoMedicoRes> CambiarEstado(int id, PedidoMedicoEstadoReq request, int usuarioId)
        {
            var pedido = await _context.Set<PedidoMedico>().FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new NotFoundException("No se encontró el pedido.");
            var estado = request?.Estado?.Trim().ToUpperInvariant();
            if (!EstadosPedidoMedico.EsValido(estado))
                throw new BadRequestException($"Estado no válido. Use: {string.Join(", ", EstadosPedidoMedico.Todos)}.");
            if ((pedido.Estado is EstadosPedidoMedico.Anulado or EstadosPedidoMedico.Entregado) && pedido.Estado != estado)
                throw new BadRequestException($"El pedido ya está {pedido.Estado}: no se puede cambiar de estado.");
            if (estado == EstadosPedidoMedico.Anulado && string.IsNullOrWhiteSpace(request!.Respuesta))
                throw new BadRequestException("Indica el motivo de la anulación (el médico lo verá en la app).");
            if ((request!.Respuesta?.Length ?? 0) > 1000)
                throw new BadRequestException("El mensaje para el médico no puede pasar de 1000 caracteres.");
            if (pedido.PedidoId is > 0 && estado != pedido.Estado)
                throw new BadRequestException($"Este pedido ya es el pedido P{pedido.PedidoId}: su estado avanza desde Gestión de Pedidos.");
            if (request.PedidoId is > 0 && !await _context.Pedidos.AnyAsync(p => p.Id == request.PedidoId))
                throw new BadRequestException("El N° de pedido de Gestión de Pedidos no existe.");
            if (request.PedidoId is > 0 && await _context.Set<PedidoMedico>()
                    .AnyAsync(p => p.PedidoId == request.PedidoId && p.Id != id))
                throw new BadRequestException($"El pedido P{request.PedidoId} ya está enlazado a otro pedido de médico.");

            pedido.Estado = estado!;
            if (request.Respuesta != null) pedido.Respuesta = string.IsNullOrWhiteSpace(request.Respuesta) ? null : request.Respuesta.Trim();
            if (request.PedidoId is > 0) pedido.PedidoId = request.PedidoId;
            pedido.FechaModificacion = Ahora();
            pedido.ModificadorId = usuarioId > 0 ? usuarioId : null;
            await _context.SaveChangesAsync();
            return await Detalle(id);
        }

        /// <summary>
        /// El paciente del pedido es el propio médico. Busca al médico como paciente de la sede (misma persona
        /// o mismo DNI) y, si no existe, lo registra usando su misma persona.
        /// </summary>
        public async Task<PacienteDelMedicoRes> PacienteDelMedico(int pedidoMedicoId, int sedeId, int usuarioId)
        {
            var pedido = await _context.Set<PedidoMedico>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == pedidoMedicoId)
                ?? throw new NotFoundException("No se encontró el pedido.");
            if (pedido.PedidoId is > 0)
                throw new BadRequestException($"Este pedido ya se convirtió en el pedido P{pedido.PedidoId}.");
            if (pedido.Estado is EstadosPedidoMedico.Anulado or EstadosPedidoMedico.Entregado)
                throw new BadRequestException($"El pedido está {pedido.Estado}: no se puede convertir.");
            if (sedeId <= 0) sedeId = pedido.SedeId ?? 0;
            if (!await _context.Sedes.AnyAsync(s => s.Id == sedeId))
                throw new BadRequestException("Elige una sede válida.");

            var medico = await _context.Medicos.AsNoTracking().Include(m => m.Persona)
                .FirstOrDefaultAsync(m => m.Id == pedido.MedicoId)
                ?? throw new NotFoundException("El médico del pedido ya no está en el directorio.");
            if (medico.PersonaId is not > 0 || medico.Persona == null)
                throw new BadRequestException("El médico no tiene sus datos personales registrados: complétalos en Médicos.");

            var personaId = medico.PersonaId.Value;
            var dni = medico.Persona.Dni?.Trim();
            var paciente = await _context.Pacientes.Include(p => p.Persona)
                .Where(p => p.SedeId == sedeId)
                .Where(p => p.PersonaId == personaId || (!string.IsNullOrEmpty(dni) && p.Persona != null && p.Persona.Dni == dni))
                .OrderBy(p => p.PersonaId == personaId ? 0 : 1).ThenBy(p => p.Id)
                .FirstOrDefaultAsync();

            var creado = false;
            if (paciente == null)
            {
                var autor = usuarioId > 0 ? usuarioId : medico.CreadorId;
                paciente = new Paciente { PersonaId = personaId, SedeId = sedeId, CreadorId = autor, ModificadorId = autor };
                _context.Pacientes.Add(paciente);
                await _context.SaveChangesAsync();
                creado = true;
            }

            return new PacienteDelMedicoRes
            {
                PacienteId = paciente.Id ?? 0,
                Dni = dni,
                Nombre = medico.Persona.NombreCompleto?.Trim(),
                Creado = creado,
                MedicoId = pedido.MedicoId,
                Cmp = pedido.Cmp,
            };
        }
    }
}

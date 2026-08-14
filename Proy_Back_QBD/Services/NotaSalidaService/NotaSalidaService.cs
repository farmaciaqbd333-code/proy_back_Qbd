using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Dto.NotaSalida;
using proy_back_Qbd.Models;
using proy_back_Qbd.Services.Interfaces.INotaSalidaService;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;

namespace Proy_back_QBD.Services.NotaSalidaService
{
    public partial class NotaSalidaService : INotaSalidaService
    {
        private readonly ApiContext _context;
        private readonly ILogger<NotaSalidaService> _logger;
        public NotaSalidaService(ApiContext context, ILogger<NotaSalidaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> Crear(CreateReq request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var notaSalida = new NotaSalida
                {
                    FechaSalida = request.FechaSalida,
                    IdSedeOrigen = request.IdSedeOrigen,
                    Estado = "PENDIENTE",
                    IdSedeDestino = request.IdSedeDestino,
                    Observacion = request.Observacion,
                    IdCreador = request.IdCreador
                };

                _context.NotaSalidas.Add(notaSalida);
                await _context.SaveChangesAsync();

                foreach (var item in request.ListaFamilias)
                {
                    switch (item.Familia.ToUpper())
                    {
                        case "MP":
                            await CrearDetalleInsumo(notaSalida.Id, request, item);
                            break;

                        case "ME":
                            await CrearDetalleEmpaque(notaSalida.Id, request, item);
                            break;

                        case "ECO":
                            await CrearDetalleEconomato(notaSalida.Id, request, item);
                            break;

                        case "PT":
                            await CrearDetalleProducto(notaSalida.Id, request, item);
                            break;

                        default:
                            throw new Exception($"Familia '{item.Familia}' no válida.");
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return notaSalida.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<List<NotaSalidaListaRes>> ObtenerLista(int idSede)
        {
            return await _context.NotaSalidas
                .AsNoTracking()
                .Include(n => n.SedeDestino)
                .Include(n => n.SedeOrigen)
                .Include(n => n.Creador)
                .OrderByDescending(n => n.FechaCreacion)
                .Where(w => idSede == 0 || w.IdSedeOrigen == idSede)
                .Select(n => new NotaSalidaListaRes
                {
                    IdNotaSalida = n.Id,
                    Codigo = UtilFamilia.CodigoNotaSalida(n.Id),
                    FechaCreacion = n.FechaCreacion,
                    Destino = n.SedeDestino != null ? n.SedeDestino.Nombre ?? string.Empty : string.Empty,
                    Responsable = n.Creador != null ? n.Creador!.Persona!.NombreCompleto! : "",
                    Observacion = n.Observacion
                })
                .ToListAsync();
        }


        public async Task Actualizar(int id, CreateReq request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var notaSalida = await _context.NotaSalidas
                    .Include(x => x.NotaSalidaInsumos)
                    .Include(x => x.NotaSalidaEmpaques)
                    .Include(x => x.NotaSalidaEconomatos)
                    .Include(x => x.NotaSalidaProductos)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (notaSalida == null)
                    throw new Exception("Nota salida no encontrada.");

                // 1. Revertir stock anterior
                await RevertirStockInsumo(notaSalida.Id);
                await RevertirStockEmpaque(notaSalida.Id);
                await RevertirStockEconomato(notaSalida.Id);
                await RevertirStockProducto(notaSalida.Id);


                // 2. Eliminar detalles anteriores
                _context.NotaSalidaInsumos.RemoveRange(notaSalida.NotaSalidaInsumos);
                _context.NotaSalidaEmpaques.RemoveRange(notaSalida.NotaSalidaEmpaques);
                _context.NotaSalidaEconomatos.RemoveRange(notaSalida.NotaSalidaEconomatos);
                _context.NotaSalidaProductos.RemoveRange(notaSalida.NotaSalidaProductos);


                // 3. Actualizar cabecera
                notaSalida.FechaSalida = request.FechaSalida;
                notaSalida.IdSedeOrigen = request.IdSedeOrigen;
                notaSalida.IdSedeDestino = request.IdSedeDestino;
                notaSalida.Observacion = request.Observacion;


                await _context.SaveChangesAsync();


                // 4. Crear nuevamente detalles y stock
                foreach (var item in request.ListaFamilias)
                {
                    switch (item.Familia.ToUpper())
                    {
                        case "MP":
                            await CrearDetalleInsumo(notaSalida.Id, request, item);
                            break;

                        case "ME":
                            await CrearDetalleEmpaque(notaSalida.Id, request, item);
                            break;

                        case "ECO":
                            await CrearDetalleEconomato(notaSalida.Id, request, item);
                            break;

                        case "PT":
                            await CrearDetalleProducto(notaSalida.Id, request, item);
                            break;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        public async Task Eliminar(int id)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var notaSalida = await _context.NotaSalidas
                    .Include(x => x.NotaSalidaInsumos)
                    .Include(x => x.NotaSalidaEmpaques)
                    .Include(x => x.NotaSalidaEconomatos)
                    .Include(x => x.NotaSalidaProductos)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (notaSalida == null)
                    throw new Exception("Nota salida no encontrada.");


                // Revertir movimientos de stock
                await EliminarStockInsumo(notaSalida.NotaSalidaInsumos);
                await EliminarStockEmpaque(notaSalida.NotaSalidaEmpaques);
                await EliminarStockEconomato(notaSalida.NotaSalidaEconomatos);
                await EliminarStockProducto(notaSalida.NotaSalidaProductos);



                // Eliminar detalles
                _context.NotaSalidaInsumos.RemoveRange(notaSalida.NotaSalidaInsumos);
                _context.NotaSalidaEmpaques.RemoveRange(notaSalida.NotaSalidaEmpaques);
                _context.NotaSalidaEconomatos.RemoveRange(notaSalida.NotaSalidaEconomatos);
                _context.NotaSalidaProductos.RemoveRange(notaSalida.NotaSalidaProductos);


                // Eliminar cabecera
                _context.NotaSalidas.Remove(notaSalida);


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<List<RegistrosListaRes>> ObtenerDatosXRegistro(string registro, int idSede)
        {
            string familia = registro.Substring(0, registro.IndexOf("-")).Trim();
            int idRegistro = int.Parse(registro.Substring(registro.IndexOf("-") + 1).Trim());

            return familia switch
            {

                "MP" => await _context.CompraInsumos
                    .Where(x => x.Id == idRegistro &&
                    (!x.FechaVencimiento.HasValue || x.FechaVencimiento.Value.Date >= DateTime.Today) &&
                    x.StockInsumos
                        .Where(s => s.IdSede == idSede)
                        .Sum(s => s.StockDisponible) > 0)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.IdInsumo,
                        DescripcionArticulo = x.Insumo.Descripcion,
                        CodigoArticulo = UtilFamilia.CodigoInsumo(x.Id)
                    })
                    .ToListAsync(),

                "ME" => await _context.CompraEmpaques
                    .Where(x => x.Id == idRegistro &&
                    (!x.FechaVencimiento.HasValue || x.FechaVencimiento.Value.Date >= DateTime.Today) &&
                    x.StockEmpaques
                        .Where(s => s.IdSede == idSede)
                        .Sum(s => s.StockDisponible) > 0)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.Id,
                        DescripcionArticulo = x.Empaque.Descripcion ?? "",
                        CodigoArticulo = UtilFamilia.CodigoEmpaque(x.Id)
                    })
                    .ToListAsync(),

                "ECO" => await _context.CompraEconomatos
                    .Where(x => x.Id == idRegistro &&
                    x.StockEconomatos
                        .Where(s => s.IdSede == idSede)
                        .Sum(s => s.StockDisponible) > 0)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.Id,
                        DescripcionArticulo = x.Economato.Descripcion ?? "",
                        CodigoArticulo = UtilFamilia.CodigoEconomato(x.Id)
                    })
                    .ToListAsync(),

                "PT" => await _context.CompraProductos
                    .Where(x => x.Id == idRegistro &&
                    (!x.FechaVencimiento.HasValue || x.FechaVencimiento.Value.Date >= DateTime.Today) &&
                    x.StockProductoTerminados
                        .Where(s => s.IdSede == idSede)
                        .Sum(s => s.StockDisponible) > 0)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.Id,
                        DescripcionArticulo = x.Producto.Descripcion ?? "",
                        CodigoArticulo = UtilFamilia.CodigoProducto(x.Id)
                    })
                    .ToListAsync(),

                _ => new List<RegistrosListaRes>()

            };

        }

        public async Task<List<RegistrosRes>> ObtenerRegistrosXFamilia(ObtenerRegistroReq request)
        {
            return request.Familia.ToUpper() switch
            {
                "PT" => await _context.CompraProductos
                    .Include(i => i.StockProductoTerminados)
                    .Where(w => w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                "MP" => await _context.CompraInsumos
                .Include(i => i.StockInsumos)
                .Where(w => w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                "ECO" => await _context.CompraEconomatos
                .Include(i => i.StockEconomatos)
                .Where(w => w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                "ME" => await _context.CompraEmpaques
                .Include(i => i.StockEmpaques)
                .Where(w => w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                _ => throw new ArgumentException("Familia no válida.")
            };
        }

        public async Task<List<NotaSalidaDetalleRes>> ObtenerDetalles(int idNotaSalida)
        {
            var resultado = new List<NotaSalidaDetalleRes>();

            // 1. Insumos (MP)
            var insumos = await _context.NotaSalidaInsumos
                .AsNoTracking()
                .Include(x => x.CompraInsumos)
                    .ThenInclude(ci => ci!.Insumo)
                .Where(x => x.IdNotaSalida == idNotaSalida)
                .ToListAsync();

            foreach (var item in insumos)
            {
                var idInsumo = item.CompraInsumos?.IdInsumo ?? 0;
                resultado.Add(new NotaSalidaDetalleRes
                {
                    Familia = "MP",
                    Codigo = idInsumo > 0 ? UtilFamilia.CodigoInsumo(idInsumo) : "",
                    DescripcionQBD = item.CompraInsumos?.Insumo?.Descripcion ?? "",
                    Registro = item.IdCompraInsumo.HasValue ? Alfanumerico.ConvertToBase36(item.IdCompraInsumo.Value) : "",
                    Cantidad = item.Cantidad,
                    Um = !string.IsNullOrEmpty(item.Um) ? item.Um.ToUpper() : (item.CompraInsumos?.Um?.ToUpper() ?? "G"),
                    // Tara = item.Tara,
                    // PesoNeto = item.PesoNeto,
                    // PesoBruto = item.PesoBruto,
                    Lote = item.CompraInsumos?.Lote ?? item.Lote ?? "",
                    FFabric = item.CompraInsumos?.FechaFabricacion?.ToString("yyyy-MM-dd") ?? "",
                    FVcto = item.CompraInsumos?.FechaVencimiento?.ToString("yyyy-MM-dd") ?? ""
                });
            }

            // 2. Empaques (ME)
            var empaques = await _context.NotaSalidaEmpaques
                .AsNoTracking()
                .Include(x => x.CompraEmpaques)
                    .ThenInclude(ce => ce!.Empaque)
                .Where(x => x.IdNotaSalida == idNotaSalida)
                .ToListAsync();

            foreach (var item in empaques)
            {
                var idEmpaque = item.CompraEmpaques?.IdEmpaque ?? 0;
                resultado.Add(new NotaSalidaDetalleRes
                {
                    Familia = "ME",
                    Codigo = idEmpaque > 0 ? UtilFamilia.CodigoEmpaque(idEmpaque) : "",
                    DescripcionQBD = item.CompraEmpaques?.Empaque?.Descripcion ?? "",
                    Registro = item.IdCompraEmpaque.HasValue ? Alfanumerico.ConvertToBase36(item.IdCompraEmpaque.Value) : "",
                    Cantidad = item.Cantidad,
                    Um = !string.IsNullOrEmpty(item.Um) ? item.Um.ToUpper() : "UND",
                    Tara = 0,
                    PesoNeto = 0,
                    PesoBruto = 0,
                    Lote = item.CompraEmpaques?.Lote ?? item.Lote ?? "",
                    FFabric = item.CompraEmpaques?.FechaFabricacion?.ToString("yyyy-MM-dd") ?? "",
                    FVcto = item.CompraEmpaques?.FechaVencimiento?.ToString("yyyy-MM-dd") ?? ""
                });
            }

            // 3. Economatos (ECO)
            var economatos = await _context.NotaSalidaEconomatos
                .AsNoTracking()
                .Include(x => x.CompraEconomato)
                    .ThenInclude(ce => ce!.Economato)
                .Where(x => x.IdNotaSalida == idNotaSalida)
                .ToListAsync();

            foreach (var item in economatos)
            {
                var idEco = item.CompraEconomato?.IdEconomato ?? 0;
                resultado.Add(new NotaSalidaDetalleRes
                {
                    Familia = "ECO",
                    Codigo = idEco > 0 ? UtilFamilia.CodigoEconomato(idEco) : "",
                    DescripcionQBD = item.CompraEconomato?.Economato?.Descripcion ?? "",
                    Registro = item.IdCompraEconomato.HasValue ? Alfanumerico.ConvertToBase36(item.IdCompraEconomato.Value) : "",
                    Cantidad = item.Cantidad,
                    Um = !string.IsNullOrEmpty(item.Um) ? item.Um.ToUpper() : "UND",
                    Tara = 0,
                    PesoNeto = 0,
                    PesoBruto = 0,
                    Lote = item.Lote ?? "",
                    FFabric = "",
                    FVcto = ""
                });
            }

            // 4. Productos Terminados (PT)
            var productos = await _context.NotaSalidaProductos
                .AsNoTracking()
                .Include(x => x.CompraProducto)
                    .ThenInclude(cp => cp!.Producto)
                .Where(x => x.IdNotaSalida == idNotaSalida)
                .ToListAsync();

            foreach (var item in productos)
            {
                var idPt = item.CompraProducto?.IdProducto ?? 0;
                resultado.Add(new NotaSalidaDetalleRes
                {
                    Familia = "PT",
                    Codigo = idPt > 0 ? UtilFamilia.CodigoProducto(idPt) : "",
                    DescripcionQBD = item.CompraProducto?.Producto?.Descripcion ?? "",
                    Registro = item.IdCompraProducto.HasValue ? Alfanumerico.ConvertToBase36(item.IdCompraProducto.Value) : "",
                    Cantidad = item.Cantidad,
                    Um = !string.IsNullOrEmpty(item.Um) ? item.Um.ToUpper() : "UND",
                    Tara = 0,
                    PesoNeto = 0,
                    PesoBruto = 0,
                    Lote = item.CompraProducto?.Lote ?? item.Lote ?? "",
                    FFabric = item.CompraProducto?.FechaFabricacion?.ToString("yyyy-MM-dd") ?? "",
                    FVcto = item.CompraProducto?.FechaVencimiento?.ToString("yyyy-MM-dd") ?? ""
                });
            }

            return resultado;
        }

        public async Task Confirmar(ConfirmarReq request)
        {
            _logger.LogInformation(
                "Inicio confirmación NotaSalida. SedeOrigen: {SedeOrigen}, SedeDestino: {SedeDestino}",
                request?.IdSedeOrigen,
                request?.IdSedeDestino);

            // Validaciones generales
            if (request == null)
            {
                _logger.LogWarning("Confirmación rechazada: request nulo.");
                throw new ArgumentNullException(nameof(request));
            }

            if (request.IdSedeOrigen <= 0)
            {
                _logger.LogWarning("Confirmación rechazada: sede origen inválida {SedeOrigen}",
                    request.IdSedeOrigen);

                throw new Exception("Debe indicar la sede origen.");
            }

            if (request.IdSedeDestino <= 0)
            {
                _logger.LogWarning("Confirmación rechazada: sede destino inválida {SedeDestino}",
                    request.IdSedeDestino);

                throw new Exception("Debe indicar la sede destino.");
            }

            if (request.IdSedeOrigen == request.IdSedeDestino)
            {
                _logger.LogWarning(
                    "Confirmación rechazada: sede origen y destino iguales. Sede: {Sede}",
                    request.IdSedeOrigen);

                throw new Exception("La sede origen y destino no pueden ser iguales.");
            }

            if (!(request.Insumos?.Any() == true ||
                  request.Economatos?.Any() == true ||
                  request.Empaques?.Any() == true ||
                  request.Productos?.Any() == true))
            {
                _logger.LogWarning("Confirmación rechazada: no se enviaron artículos.");

                throw new Exception("Debe enviar al menos una familia de artículos.");
            }


            var articulos = (request.Insumos ?? [])
                .Concat(request.Economatos ?? [])
                .Concat(request.Empaques ?? [])
                .Concat(request.Productos ?? [])
                .ToList();


            _logger.LogInformation(
                "Artículos recibidos para confirmar: {CantidadArticulos}. " +
                "Insumos: {Insumos}, Economatos: {Economatos}, Empaques: {Empaques}, Productos: {Productos}",
                articulos.Count,
                request.Insumos?.Count ?? 0,
                request.Economatos?.Count ?? 0,
                request.Empaques?.Count ?? 0,
                request.Productos?.Count ?? 0);


            foreach (var item in articulos)
            {
                if (item.IdNotaSalidaArticulo <= 0)
                {
                    _logger.LogWarning(
                        "Artículo inválido. IdNotaSalidaArticulo: {IdNotaSalidaArticulo}",
                        item.IdNotaSalidaArticulo);

                    throw new Exception("Existe un artículo con IdNotaSalidaArticulo inválido.");
                }

                if (item.IdCompraArticulo <= 0)
                {
                    _logger.LogWarning(
                        "Artículo inválido. IdCompraArticulo: {IdCompraArticulo}",
                        item.IdCompraArticulo);

                    throw new Exception("Existe un artículo con IdCompraArticulo inválido.");
                }

                if (item.CantidadRecibida <= 0)
                {
                    _logger.LogWarning(
                        "Cantidad inválida. IdCompraArticulo: {IdCompraArticulo}, Cantidad: {Cantidad}",
                        item.IdCompraArticulo,
                        item.CantidadRecibida);

                    throw new Exception(
                        $"La cantidad recibida debe ser mayor a cero. IdCompraArticulo: {item.IdCompraArticulo}");
                }
            }


            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation(
                    "Procesando movimiento de stock. Origen: {Origen}, Destino: {Destino}",
                    request.IdSedeOrigen,
                    request.IdSedeDestino);


                if (request.Insumos?.Any() == true)
                    await ProcesarInsumos(request.Insumos, request.IdSedeOrigen, request.IdSedeDestino);

                if (request.Economatos?.Any() == true)
                    await ProcesarEconomatos(request.Economatos, request.IdSedeOrigen, request.IdSedeDestino);

                if (request.Empaques?.Any() == true)
                    await ProcesarEmpaques(request.Empaques, request.IdSedeOrigen, request.IdSedeDestino);

                if (request.Productos?.Any() == true)
                    await ProcesarProductos(request.Productos, request.IdSedeOrigen, request.IdSedeDestino);


                await _context.SaveChangesAsync();

                await transaction.CommitAsync();


                _logger.LogInformation(
                    "Confirmación realizada correctamente. Origen: {Origen}, Destino: {Destino}",
                    request.IdSedeOrigen,
                    request.IdSedeDestino);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Error confirmando NotaSalida. Origen: {Origen}, Destino: {Destino}",
                    request.IdSedeOrigen,
                    request.IdSedeDestino);

                throw;
            }
        }
    }
}
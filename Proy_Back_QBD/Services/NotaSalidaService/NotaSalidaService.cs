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
                        case "PI":
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
                .OrderByDescending(n => n.FechaCreacion)
                .Where(w => w.IdSedeDestino == idSede)
                .Select(n => new NotaSalidaListaRes
                {
                    IdNotaSalida = n.Id,
                    Codigo = UtilFamilia.CodigoNotaSalida(n.Id),
                    FechaCreacion = n.FechaCreacion,
                    Destino = n.SedeDestino != null ? n.SedeDestino.Nombre ?? string.Empty : string.Empty,
                    Responsable = n.Creador != null ? n.Creador!.Persona!.NombreCompleto! : "",
                    Observacion = n.Observacion,
                    Estado = (n.Estado == "RECIBIDO" || n.Estado == "RECEPCIONADO" ||
                              n.NotaSalidaInsumos.Any(x => x.CantidadRecibida != null && x.CantidadRecibida > 0) ||
                              n.NotaSalidaEmpaques.Any(x => x.CantidadRecibida > 0) ||
                              n.NotaSalidaEconomatos.Any(x => x.CantidadRecibida > 0) ||
                              n.NotaSalidaProductos.Any(x => x.CantidadRecibida > 0))
                              ? "RECIBIDO"
                              : "PROCESANDO"
                })
                .ToListAsync();
        }

        public async Task<List<NotaSalidaListaRes>> ObtenerListaPorSedeOrigen(int idSedeOrigen)
        {
            return await _context.NotaSalidas
                .AsNoTracking()
                .OrderByDescending(n => n.FechaCreacion)
                .Where(w => w.IdSedeOrigen == idSedeOrigen)
                .Select(n => new NotaSalidaListaRes
                {
                    IdNotaSalida = n.Id,
                    Codigo = UtilFamilia.CodigoNotaSalida(n.Id),
                    FechaCreacion = n.FechaCreacion,
                    Destino = n.SedeDestino != null ? n.SedeDestino.Nombre ?? string.Empty : string.Empty,
                    Responsable = n.Creador != null ? n.Creador!.Persona!.NombreCompleto! : "",
                    Observacion = n.Observacion,
                    Estado = (n.Estado == "RECIBIDO" || n.Estado == "RECEPCIONADO" ||
                              n.NotaSalidaInsumos.Any(x => x.CantidadRecibida != null && x.CantidadRecibida > 0) ||
                              n.NotaSalidaEmpaques.Any(x => x.CantidadRecibida > 0) ||
                              n.NotaSalidaEconomatos.Any(x => x.CantidadRecibida > 0) ||
                              n.NotaSalidaProductos.Any(x => x.CantidadRecibida > 0))
                              ? "RECIBIDO"
                              : "PROCESANDO"
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
                        case "PI":
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
            if (string.IsNullOrWhiteSpace(registro))
                return new List<RegistrosListaRes>();

            string regUpper = registro.Trim().ToUpper();
            string familia = "";
            int idRegistro = 0;

            if (regUpper.StartsWith("ECO"))
            {
                familia = "ECO";
                string rest = regUpper.Substring(3).TrimStart('-', ' ');
                if (!int.TryParse(rest, out idRegistro))
                    idRegistro = Alfanumerico.ConvertFromBase36(rest);
            }
            else if (regUpper.StartsWith("MP") || regUpper.StartsWith("ME") || regUpper.StartsWith("PT") || regUpper.StartsWith("PI"))
            {
                familia = regUpper.Substring(0, 2);
                string rest = regUpper.Substring(2).TrimStart('-', ' ');
                if (!int.TryParse(rest, out idRegistro))
                    idRegistro = Alfanumerico.ConvertFromBase36(rest);
            }
            else if (regUpper.Contains("-"))
            {
                var parts = regUpper.Split('-');
                familia = parts[0].Trim();
                string rest = parts[1].Trim();
                if (!int.TryParse(rest, out idRegistro))
                    idRegistro = Alfanumerico.ConvertFromBase36(rest);
            }
            else
            {
                if (!int.TryParse(regUpper, out idRegistro))
                    idRegistro = Alfanumerico.ConvertFromBase36(regUpper);
            }

            if (string.IsNullOrEmpty(familia))
                familia = "MP";

            return familia switch
            {
                "MP" => await _context.CompraInsumos
                    .Where(x => x.Id == idRegistro)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.IdInsumo,
                        DescripcionArticulo = x.Insumo != null ? x.Insumo.Descripcion : "",
                        CodigoArticulo = UtilFamilia.CodigoInsumo(x.IdInsumo)
                    })
                    .ToListAsync(),

                "ME" => await _context.CompraEmpaques
                    .Where(x => x.Id == idRegistro)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.IdEmpaque,
                        DescripcionArticulo = x.Empaque != null ? (x.Empaque.Descripcion ?? "") : "",
                        CodigoArticulo = UtilFamilia.CodigoEmpaque(x.IdEmpaque)
                    })
                    .ToListAsync(),

                "ECO" => await _context.CompraEconomatos
                    .Where(x => x.Id == idRegistro)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.IdEconomato,
                        DescripcionArticulo = x.Economato != null ? (x.Economato.Descripcion ?? "") : "",
                        CodigoArticulo = UtilFamilia.CodigoEconomato(x.IdEconomato)
                    })
                    .ToListAsync(),

                "PT" => await _context.CompraProductos
                    .Where(x => x.Id == idRegistro)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.IdProducto,
                        DescripcionArticulo = x.Producto != null ? (x.Producto.Descripcion ?? "") : "",
                        CodigoArticulo = UtilFamilia.CodigoProducto(x.IdProducto)
                    })
                    .ToListAsync(),

                "PI" => await _context.ProductosIntermedios
                    .Where(x => x.Id == idRegistro)
                    .Select(x => new RegistrosListaRes
                    {
                        IdArticulo = x.IdInsumo,
                        DescripcionArticulo = x.Insumo != null ? x.Insumo.Descripcion : "",
                        CodigoArticulo = UtilFamilia.CodigoProductoIntermedio(x.IdInsumo)
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
                    .Where(w => request.IdSede == 0 || request.IdSede == 15 || w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                "MP" => await _context.CompraInsumos
                    .Include(i => i.StockInsumos)
                    .Where(w => request.IdSede == 0 || request.IdSede == 15 || w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                "ECO" => await _context.CompraEconomatos
                    .Include(i => i.StockEconomatos)
                    .Where(w => request.IdSede == 0 || request.IdSede == 15 || w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                "ME" => await _context.CompraEmpaques
                    .Include(i => i.StockEmpaques)
                    .Where(w => request.IdSede == 0 || request.IdSede == 15 || w.Compra.IdSede == request.IdSede)
                    .Select(x => new RegistrosRes
                    {
                        IdRegistro = x.Id,
                        CodRegistro = Alfanumerico.ConvertToBase36(x.Id)
                    })
                    .ToListAsync(),

                "PI" => await _context.ProductosIntermedios
                    .Include(i => i.StockInsumo)
                    .Where(w => request.IdSede == 0 || request.IdSede == 15 || w.IdSede == request.IdSede || (w.StockInsumo != null && w.StockInsumo.IdSede == request.IdSede))
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
                .Include(x => x.PaqueteNotaSalidaInsumos)
                .Where(x => x.IdNotaSalida == idNotaSalida)
                .ToListAsync();

            foreach (var item in insumos)
            {
                var idInsumo = item.CompraInsumos?.IdInsumo ?? 0;
                resultado.Add(new NotaSalidaDetalleRes
                {
                    IdNotaSalidaArticulo = item.Id,
                    IdCompraArticulo = item.IdCompraInsumo ?? 0,
                    Familia = "MP",
                    Codigo = idInsumo > 0 ? UtilFamilia.CodigoInsumo(idInsumo) : "",
                    DescripcionQBD = item.CompraInsumos?.Insumo?.Descripcion ?? "",
                    Registro = item.IdCompraInsumo.HasValue ? Alfanumerico.ConvertToBase36(item.IdCompraInsumo.Value) : "",
                    Cantidad = item.Cantidad,
                    CantidadRecibida = item.CantidadRecibida,
                    Observacion = item.Observacion,
                    Um = !string.IsNullOrEmpty(item.Um) ? item.Um.ToUpper() : (item.CompraInsumos?.Um?.ToUpper() ?? "G"),
                    // Tara = item.Tara,
                    // PesoNeto = item.PesoNeto,
                    // PesoBruto = item.PesoBruto,
                    Lote = item.CompraInsumos?.Lote ?? item.Lote ?? "",
                    FFabric = item.CompraInsumos?.FechaFabricacion?.ToString("yyyy-MM-dd") ?? "",
                    FVcto = item.CompraInsumos?.FechaVencimiento?.ToString("yyyy-MM-dd") ?? "",
                    Paquetes = item.PaqueteNotaSalidaInsumos?.Select(p => new NotaSalidaDetallePaqueteRes
                    {
                        IdPaquete = (int)p.Id,
                        CantidadPaquete = p.CantidadPaquete,
                        Peso = p.Peso,
                        Tara = p.Tara,
                        Um = p.Um,
                        PesoNeto = p.PesoNeto,
                        PesoBruto = p.PesoBruto,
                        CantidadPaqueteRecibida = p.CantidadPaqueteRecibida,
                        PesoRecibida = p.PesoRecibida,
                        TaraRecibida = p.TaraRecibida,
                        PesoNetoRecibida = p.PesoNetoRecibida,
                        PesoBrutoRecibida = p.PesoBrutoRecibida,
                        IdVerificador = p.IdVerificador
                    }).ToList() ?? new List<NotaSalidaDetallePaqueteRes>()
                });
            }

            // 2. Empaques (ME)
            var empaques = await _context.NotaSalidaEmpaques
                .AsNoTracking()
                .Include(x => x.CompraEmpaques)
                    .ThenInclude(ce => ce!.Empaque)
                .Include(x => x.PaqueteNotaSalidaEmpaques)
                .Where(x => x.IdNotaSalida == idNotaSalida)
                .ToListAsync();

            foreach (var item in empaques)
            {
                var idEmpaque = item.CompraEmpaques?.IdEmpaque ?? 0;
                resultado.Add(new NotaSalidaDetalleRes
                {
                    IdNotaSalidaArticulo = item.Id,
                    IdCompraArticulo = item.IdCompraEmpaque ?? 0,
                    Familia = "ME",
                    Codigo = idEmpaque > 0 ? UtilFamilia.CodigoEmpaque(idEmpaque) : "",
                    DescripcionQBD = item.CompraEmpaques?.Empaque?.Descripcion ?? "",
                    Registro = item.IdCompraEmpaque.HasValue ? Alfanumerico.ConvertToBase36(item.IdCompraEmpaque.Value) : "",
                    Cantidad = item.Cantidad,
                    CantidadRecibida = item.CantidadRecibida,
                    Observacion = item.Observacion,
                    Um = !string.IsNullOrEmpty(item.Um) ? item.Um.ToUpper() : "UND",
                    Tara = 0,
                    PesoNeto = 0,
                    PesoBruto = 0,
                    Lote = item.CompraEmpaques?.Lote ?? item.Lote ?? "",
                    FFabric = item.CompraEmpaques?.FechaFabricacion?.ToString("yyyy-MM-dd") ?? "",
                    FVcto = item.CompraEmpaques?.FechaVencimiento?.ToString("yyyy-MM-dd") ?? "",
                    Paquetes = item.PaqueteNotaSalidaEmpaques?.Select(p => new NotaSalidaDetallePaqueteRes
                    {
                        IdPaquete = (int)p.Id,
                        CantidadPaquete = p.CantidadPaquete,
                        Peso = p.Peso,
                        Tara = p.Tara,
                        Um = p.Um,
                        PesoNeto = p.PesoNeto,
                        PesoBruto = p.PesoBruto,
                        CantidadPaqueteRecibida = p.CantidadPaqueteRecibida,
                        PesoRecibida = p.PesoRecibida,
                        TaraRecibida = p.TaraRecibida,
                        PesoNetoRecibida = p.PesoNetoRecibida,
                        PesoBrutoRecibida = p.PesoBrutoRecibida,
                        IdVerificador = p.IdVerificador
                    }).ToList() ?? new List<NotaSalidaDetallePaqueteRes>()
                });
            }

            // 3. Economatos (ECO)
            var economatos = await _context.NotaSalidaEconomatos
                .AsNoTracking()
                .Include(x => x.CompraEconomato)
                    .ThenInclude(ce => ce!.Economato)
                .Include(x => x.PaqueteNotaSalidaEconomatos)
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
                    FVcto = "",
                    Paquetes = item.PaqueteNotaSalidaEconomatos?.Select(p => new NotaSalidaDetallePaqueteRes
                    {
                        IdPaquete = (int)p.Id,
                        CantidadPaquete = p.CantidadPaquete,
                        Peso = p.Peso,
                        Tara = p.Tara,
                        Um = p.Um,
                        PesoNeto = p.PesoNeto,
                        PesoBruto = p.PesoBruto,
                        CantidadPaqueteRecibida = p.CantidadPaqueteRecibida,
                        PesoRecibida = p.PesoRecibida,
                        TaraRecibida = p.TaraRecibida,
                        PesoNetoRecibida = p.PesoNetoRecibida,
                        PesoBrutoRecibida = p.PesoBrutoRecibida,
                        IdVerificador = p.IdVerificador
                    }).ToList() ?? new List<NotaSalidaDetallePaqueteRes>()
                });
            }

            // 4. Productos Terminados (PT)
            var productos = await _context.NotaSalidaProductos
                .AsNoTracking()
                .Include(x => x.CompraProducto)
                    .ThenInclude(cp => cp!.Producto)
                .Include(x => x.PaqueteNotaSalidaProductos)
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
                    FVcto = item.CompraProducto?.FechaVencimiento?.ToString("yyyy-MM-dd") ?? "",
                    Paquetes = item.PaqueteNotaSalidaProductos?.Select(p => new NotaSalidaDetallePaqueteRes
                    {
                        IdPaquete = (int)p.Id,
                        CantidadPaquete = p.CantidadPaquete,
                        Peso = p.Peso,
                        Tara = p.Tara,
                        Um = p.Um,
                        PesoNeto = p.PesoNeto,
                        PesoBruto = p.PesoBruto,
                        CantidadPaqueteRecibida = p.CantidadPaqueteRecibida,
                        PesoRecibida = p.PesoRecibida,
                        TaraRecibida = p.TaraRecibida,
                        PesoNetoRecibida = p.PesoNetoRecibida,
                        PesoBrutoRecibida = p.PesoBrutoRecibida,
                        IdVerificador = p.IdVerificador
                    }).ToList() ?? new List<NotaSalidaDetallePaqueteRes>()
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

                int? idNotaSalida = request.IdNotaSalida;
                if (!idNotaSalida.HasValue && request.Insumos?.Any() == true && request.Insumos[0].IdNotaSalidaArticulo > 0)
                {
                    var art = await _context.NotaSalidaInsumos.FirstOrDefaultAsync(x => x.Id == request.Insumos[0].IdNotaSalidaArticulo);
                    if (art != null) idNotaSalida = art.IdNotaSalida;
                    else
                    {
                        var nsDirect = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == request.Insumos[0].IdNotaSalidaArticulo);
                        if (nsDirect != null) idNotaSalida = nsDirect.Id;
                    }
                }
                else if (!idNotaSalida.HasValue && request.Empaques?.Any() == true && request.Empaques[0].IdNotaSalidaArticulo > 0)
                {
                    var art = await _context.NotaSalidaEmpaques.FirstOrDefaultAsync(x => x.Id == request.Empaques[0].IdNotaSalidaArticulo);
                    if (art != null) idNotaSalida = art.IdNotaSalida;
                    else
                    {
                        var nsDirect = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == request.Empaques[0].IdNotaSalidaArticulo);
                        if (nsDirect != null) idNotaSalida = nsDirect.Id;
                    }
                }
                else if (!idNotaSalida.HasValue && request.Economatos?.Any() == true && request.Economatos[0].IdNotaSalidaArticulo > 0)
                {
                    var art = await _context.NotaSalidaEconomatos.FirstOrDefaultAsync(x => x.Id == request.Economatos[0].IdNotaSalidaArticulo);
                    if (art != null) idNotaSalida = art.IdNotaSalida;
                    else
                    {
                        var nsDirect = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == request.Economatos[0].IdNotaSalidaArticulo);
                        if (nsDirect != null) idNotaSalida = nsDirect.Id;
                    }
                }
                else if (!idNotaSalida.HasValue && request.Productos?.Any() == true && request.Productos[0].IdNotaSalidaArticulo > 0)
                {
                    var art = await _context.NotaSalidaProductos.FirstOrDefaultAsync(x => x.Id == request.Productos[0].IdNotaSalidaArticulo);
                    if (art != null) idNotaSalida = art.IdNotaSalida;
                    else
                    {
                        var nsDirect = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == request.Productos[0].IdNotaSalidaArticulo);
                        if (nsDirect != null) idNotaSalida = nsDirect.Id;
                    }
                }

                if (idNotaSalida.HasValue)
                {
                    var nsHeader = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == idNotaSalida.Value);
                    if (nsHeader != null)
                    {
                        nsHeader.Estado = "RECIBIDO";
                        if (request.Observacion != null)
                        {
                            nsHeader.Observacion = request.Observacion;
                        }
                    }
                }

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
            public async Task ActualizarObservacion(int idNotaSalida, string observacion)
        {
            var nota = await _context.NotaSalidas.FirstOrDefaultAsync(x => x.Id == idNotaSalida);
            if (nota == null) throw new Exception("Nota de salida no encontrada.");
            nota.Observacion = observacion;
            await _context.SaveChangesAsync();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Exceptions;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.Ajuste;
using proy_back_Qbd.Models.Ajuste.request;
using proy_back_Qbd.Models.Ajuste.response;
using proy_back_Qbd.Util;
using proy_back_Qbd.Util.Familias;
using Proy_back_QBD.Data;
using Proy_back_QBD.Services;

namespace Proy_back_QBD.Service.AjusteService
{
    public class AjusteService : IAjusteService
    {
        private readonly ApiContext _context;
        private static readonly List<string> FamiliasAptas = ["MP", "ME", "PT", "ECO"];
        public AjusteService(ApiContext context)
        {
            _context = context;
        }

        public async Task<List<TablaAjustesRes>> ListaAjustes(string familia, int idSede)
        {

            List<TablaAjustesRes> Response = familia switch
            {
                "MP" => await ObtenerMateriaPrima(idSede),
                "ME" => await ObtenerMateriaEmpaques(idSede),
                "PT" => await ObtenerProductosTerminados(idSede),
                "ECO" => await ObtenerEconomatos(idSede),
                _ => throw new BadRequestException("Familia no Apta")
            };

            return Response;

        }

        public async Task RegistrarAjuste(CrearAjusteReq request)
        {

            string familia = request.Familia;
            int idCreador = request.IdCreador;
            if (FamiliasAptas.Contains(familia))
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    List<CrearAjustes> listaAjustes = request.ListaAjustes;

                    switch (familia)
                    {
                        case "MP":
                            await StrategyCrearAjusteInsumo(listaAjustes, idCreador); break;
                        case "ME":
                            await StrategyCrearAjusteEmpaque(listaAjustes, idCreador); break;
                        case "ECO":
                            await StrategyCrearAjusteEconomato(listaAjustes, idCreador); break;
                        case "PT":
                            await StrategyCrearAjusteProductoTerminado(listaAjustes, idCreador); break;
                        default: throw new BadRequestException("Familia no apta");
                    }
                    ;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    throw new ServerException("Ocurrió un error al crear el ajuste.", e);
                }
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }

        }

        public async Task<List<DetalleAjusteRes>> DetalleAjuste(int registroId, string familia)
        {
            if (FamiliasAptas.Contains(familia))
            {
                List<DetalleAjusteRes> response = new();
                if (familia == "MP")
                {
                    response = await _context.AjusteInsumos
                    .AsNoTracking()
                    .Where(w => w.IdStockInsumo == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion
                    }).ToListAsync();
                }
                if (familia == "ME")
                {
                    response = await _context.AjusteEmpaques
                    .AsNoTracking()
                    .Where(w => w.IdStockEmpaque == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion ?? ""

                    }).ToListAsync();
                }
                if (familia == "PT")
                {
                    response = await _context.AjusteProductoTerminados
                    .AsNoTracking()
                    .Where(w => w.IdStockProducto == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion ?? ""

                    }).ToListAsync();
                }
                if (familia == "ECO")
                {
                    response = await _context.AjusteEconomatos
                    .AsNoTracking()
                    .Where(w => w.IdStockEconomato == registroId)
                    .OrderByDescending(odb => odb.FechaCreacion)
                    .Select(s => new DetalleAjusteRes()
                    {
                        FechaCreacion = s.FechaCreacion,
                        Usuario = s.Creador.Persona.NombreCompleto ?? "",
                        Stock = s.StockAnterior,
                        Diferencia = s.Ajuste,
                        StockFinal = s.StockNuevo,
                        Observacion = s.Observacion ?? ""

                    }).ToListAsync();
                }
                return response;
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }
        }

        //LISTA AJUSTE PRINCIPAL
        private async Task<List<TablaAjustesRes>> ObtenerMateriaPrima(int idSede)
        {
            return await _context.StockInsumos
                .Where(w => w.IdSede == idSede)
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.CompraInsumo.IdInsumo),
                    Registro = Alfanumerico.ConvertToBase36(s.CompraInsumo.Id),
                    Descripcion = s.CompraInsumo.Insumo!.Descripcion,
                    Lote = s.CompraInsumo.Lote ?? "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = s.CompraInsumo.FechaFabricacion,
                    FechaVencimiento = s.CompraInsumo.FechaVencimiento,
                    Clasificacion = s.CompraInsumo.Insumo!.Clasificacion ?? "MP",
                    Observacion = s.AjusteInsumos!
                        .Where(a => a.IdStockInsumo == s.Id)
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => a.Observacion)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        private async Task<List<TablaAjustesRes>> ObtenerMateriaEmpaques(int idSede)
        {
            return await _context.StockEmpaques
            .Where(w => w.IdSede == idSede)
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoEmpaque(s.CompraEmpaque.IdEmpaque),
                    Registro = Alfanumerico.ConvertToBase36(s.CompraEmpaque.Id),
                    Descripcion = s.CompraEmpaque.Empaque!.Descripcion ?? "",
                    Lote = s.CompraEmpaque.Lote ?? "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = s.CompraEmpaque.FechaFabricacion,
                    FechaVencimiento = s.CompraEmpaque.FechaVencimiento,
                    Observacion = s.AjusteEmpaques!
                        .Where(a => a.IdStockEmpaque == s.Id)
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => a.Observacion)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        private async Task<List<TablaAjustesRes>> ObtenerEconomatos(int idSede)
        {
            return await _context.StockEconomatos
            .Where(w => w.IdSede == idSede)
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.CompraEconomato.IdEconomato),
                    Registro = Alfanumerico.ConvertToBase36(s.CompraEconomato.Id),
                    Descripcion = s.CompraEconomato.Economato!.Descripcion,
                    Lote = "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Observacion = s.AjusteEconomatos!
                        .Where(a => a.IdStockEconomato == s.Id)
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => a.Observacion)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        private async Task<List<TablaAjustesRes>> ObtenerProductosTerminados(int idSede)
        {
            return await _context.StockProductos
                .Where(w => w.IdSede == idSede)
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.CompraProducto.IdProducto),
                    Registro = Alfanumerico.ConvertToBase36(s.CompraProducto.Id),
                    Descripcion = s.CompraProducto.Producto!.Descripcion ?? "",
                    Lote = s.CompraProducto.Lote ?? "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = s.CompraProducto.FechaFabricacion,
                    FechaVencimiento = s.CompraProducto.FechaVencimiento,
                    Observacion = s.AjusteProductos!
                        .Where(a => a.IdStockProducto == s.Id)
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => a.Observacion)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        //REGISTRAR AJUSTES
        private async Task StrategyCrearAjusteInsumo(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteInsumo> ajusteInsumos = new AjusteMapper().CrearAjusteInsumoList(listaAjustes, idCreador);
            foreach (var item in ajusteInsumos)
            {
                AjusteInsumo ajusteInsumo = item;
                StockInsumo stockInsumo = await _context.StockInsumos
                .Where(w => w.IdCompraInsumo == ajusteInsumo.IdStockInsumo && w.IdSede == 15)
                .FirstOrDefaultAsync() ?? throw new NotFoundException("compraInsumo no encontrada");
                stockInsumo.StockDisponible += ajusteInsumo.Ajuste;
                _context.AjusteInsumos.Add(ajusteInsumo);
            }
        }
        private async Task StrategyCrearAjusteEmpaque(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteEmpaque> ajusteEmpaques = new AjusteMapper().CrearAjusteEmpaqueList(listaAjustes, idCreador);
            foreach (var item in ajusteEmpaques)
            {
                AjusteEmpaque ajusteEmpaque = item;
                StockEmpaque compraEmpaque = await _context.StockEmpaques
                .Where(w => w.IdCompraEmpaque == ajusteEmpaque.IdStockEmpaque && w.IdSede == 15)
                .FirstOrDefaultAsync() ?? throw new BadRequestException("compraEmpaques no encontrada");
                compraEmpaque.StockDisponible += ajusteEmpaque.Ajuste;
                _context.AjusteEmpaques.Add(ajusteEmpaque);
            }
        }
        private async Task StrategyCrearAjusteEconomato(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteEconomato> ajusteEconomatos = new AjusteMapper().CrearAjusteEconomatoList(listaAjustes, idCreador);
            foreach (var item in ajusteEconomatos)
            {
                AjusteEconomato ajusteEconomato = item;
                StockEconomato stockEconomato = await _context.StockEconomatos
                .Where(w => w.IdCompraEconomato == ajusteEconomato.IdStockEconomato && w.IdSede == 15)
                .FirstOrDefaultAsync() ?? throw new NotFoundException("compraEconomato no encontrada");
                stockEconomato.StockDisponible += ajusteEconomato.Ajuste;
                _context.AjusteEconomatos.Add(ajusteEconomato);
            }
        }
        private async Task StrategyCrearAjusteProductoTerminado(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteProducto> ajusteProductoTerminados = new AjusteMapper().CrearAjusteProductoTerminadoList(listaAjustes, idCreador);
            foreach (var item in ajusteProductoTerminados)
            {
                AjusteProducto ajusteProductoTerminado = item;
                StockProducto compraProductoTerminado = await _context.StockProductos
                .Where(w => w.IdCompraProducto == ajusteProductoTerminado.IdStockProducto && w.IdSede == 15)
                .FirstOrDefaultAsync() ?? throw new NotFoundException("compraProductoTerminado no encontrada");
                compraProductoTerminado.StockDisponible += ajusteProductoTerminado.Ajuste;
                _context.AjusteProductoTerminados.Add(ajusteProductoTerminado);
            }
        }
    }
}
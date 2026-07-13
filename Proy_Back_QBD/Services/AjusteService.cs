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

        public async Task<List<TablaAjustesRes>> ListaAjustes(string familia)
        {

            List<TablaAjustesRes> Response = familia switch
            {
                "MP" => await ObtenerMateriaPrima(),
                "ME" => await ObtenerMateriaEmpaques(),
                "PT" => await ObtenerProductosTerminados(),
                "ECO" => await ObtenerEconomatos(),
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
                    .Where(w => w.IdCompraInsumo == registroId)
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
                    .Where(w => w.IdCompraEmpaque == registroId)
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
                    .Where(w => w.IdCompraProducto == registroId)
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
                    .Where(w => w.IdCompraEconomato == registroId)
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
        private async Task<List<TablaAjustesRes>> ObtenerMateriaPrima()
        {
            return await _context.CompraInsumos
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdInsumo),
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Insumo!.Descripcion,
                    Lote = s.Lote ?? "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Clasificacion = s.Insumo!.Clasificacion ?? "MP",
                    Observacion = s.AjusteInsumos!
                        .Where(a => a.IdCompraInsumo == s.Id)
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => a.Observacion)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        private async Task<List<TablaAjustesRes>> ObtenerMateriaEmpaques()
        {
            return await _context.CompraEmpaques
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoEmpaque(s.IdEmpaque),
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Empaque!.Descripcion ?? "",
                    Lote = s.Lote ?? "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.AjusteEmpaques!
                        .Where(a => a.IdCompraEmpaque == s.Id)
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => a.Observacion)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        private async Task<List<TablaAjustesRes>> ObtenerEconomatos()
        {
            return await _context.CompraEconomatos
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdEconomato),
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Economato!.Descripcion,
                    Lote = "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = null,
                    FechaVencimiento = null,
                    Observacion = s.AjusteEconomatos!
                        .Where(a => a.IdCompraEconomato == s.Id)
                        .OrderByDescending(a => a.FechaCreacion)
                        .Select(a => a.Observacion)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        private async Task<List<TablaAjustesRes>> ObtenerProductosTerminados()
        {
            return await _context.CompraProductos
                .Select(s => new TablaAjustesRes
                {
                    Codigo = UtilFamilia.CodigoInsumo(s.IdProducto),
                    Registro = Alfanumerico.ConvertToBase36(s.Id),
                    Descripcion = s.Producto!.Descripcion ?? "",
                    Lote = s.Lote ?? "",
                    Saldo = s.StockDisponible,
                    FechaFabricacion = s.FechaFabricacion,
                    FechaVencimiento = s.FechaVencimiento,
                    Observacion = s.AjusteProductoTerminados!
                        .Where(a => a.IdCompraProducto == s.Id)
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
                CompraInsumos compraInsumo = await _context.CompraInsumos
                .Where(w => w.Id == ajusteInsumo.IdCompraInsumo)
                .FirstOrDefaultAsync() ?? throw new NotFoundException("compraInsumo no encontrada");
                compraInsumo.StockDisponible += ajusteInsumo.Ajuste;
                _context.AjusteInsumos.Add(ajusteInsumo);
            }
        }
        private async Task StrategyCrearAjusteEmpaque(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteEmpaque> ajusteEmpaques = new AjusteMapper().CrearAjusteEmpaqueList(listaAjustes, idCreador);
            foreach (var item in ajusteEmpaques)
            {
                AjusteEmpaque ajusteEmpaque = item;
                CompraEmpaques compraEmpaque = await _context.CompraEmpaques
                .Where(w => w.Id == ajusteEmpaque.IdCompraEmpaque)
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
                CompraEconomatos compraEconomato = await _context.CompraEconomatos
                .Where(w => w.Id == ajusteEconomato.IdCompraEconomato)
                .FirstOrDefaultAsync() ?? throw new NotFoundException("compraEconomato no encontrada");
                compraEconomato.StockDisponible += ajusteEconomato.Ajuste;
                _context.AjusteEconomatos.Add(ajusteEconomato);
            }
        }
        private async Task StrategyCrearAjusteProductoTerminado(List<CrearAjustes> listaAjustes, int idCreador)
        {
            List<AjusteProductoTerminado> ajusteProductoTerminados = new AjusteMapper().CrearAjusteProductoTerminadoList(listaAjustes, idCreador);
            foreach (var item in ajusteProductoTerminados)
            {
                AjusteProductoTerminado ajusteProductoTerminado = item;
                CompraProductos compraProductoTerminado = await _context.CompraProductos
                .Where(w => w.Id == ajusteProductoTerminado.IdCompraProducto)
                .FirstOrDefaultAsync() ?? throw new NotFoundException("compraProductoTerminado no encontrada");
                compraProductoTerminado.StockDisponible += ajusteProductoTerminado.Ajuste;
                _context.AjusteProductoTerminados.Add(ajusteProductoTerminado);
            }
        }
    }
}
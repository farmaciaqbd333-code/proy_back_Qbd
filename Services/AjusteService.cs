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
        private static readonly List<string> FamiliasAptas = ["MP", "ME"];
        public AjusteService(ApiContext context)
        {
            _context = context;
        }

        public async Task<List<TablaAjustesRes>> ListaAjustes(string familia)
        {
            if (FamiliasAptas.Contains(familia))
            {
                List<TablaAjustesRes> Response = new();
                if (familia == "MP")
                {
                    Response = await _context.CompraInsumos
                    .Select(s => new TablaAjustesRes()
                    {
                        Codigo = UtilFamilia.CodigoInsumo(s.IdInsumo),
                        Registro = Alfanumerico.ConvertToBase36(s.Id),
                        Descripcion = s.Insumo!.Descripcion,
                        Lote = s.Lote ?? "",
                        Saldo = s.StockDisponible,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                    }).ToListAsync()
                    ;

                }
                if (familia == "ME")
                {
                    Response = await _context.CompraEmpaques
                    .Select(s => new TablaAjustesRes()
                    {
                        Codigo = UtilFamilia.CodigoInsumo(s.IdEmpaque),
                        Registro = Alfanumerico.ConvertToBase36(s.Id),
                        Descripcion = s.Empaque!.Descripcion ?? "",
                        Lote = s.Lote ?? "",
                        Saldo = s.StockDisponible,
                        FechaFabricacion = s.FechaFabricacion,
                        FechaVencimiento = s.FechaVencimiento,
                    }).ToListAsync()
                    ;

                }
                return Response;
            }
            else
            {
                throw new BadRequestException("Familia no Apta");
            }
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
                    if (familia == "MP")
                    {
                        List<AjusteInsumo> ajusteInsumos = new AjusteMapper().CrearAjusteInsumoList(listaAjustes, idCreador);
                        foreach (var item in ajusteInsumos)
                        {
                            AjusteInsumo ajusteInsumo = item;
                            CompraInsumos compraInsumo = await _context.CompraInsumos
                            .Where(w => w.Id == ajusteInsumo.IdCompraInsumo)
                            .FirstOrDefaultAsync() ?? throw new NotFoundException("compraInsumo no encontrada");
                            if (compraInsumo.StockDisponible == null) throw new BadRequestException("el stock disponible es null, tiene que ser 0");
                            else
                            {
                                compraInsumo.StockDisponible += ajusteInsumo.Ajuste;
                            }
                            _context.AjusteInsumos.Add(ajusteInsumo);
                        }
                    }
                    if (familia == "ME")
                    {
                        List<AjusteEmpaque> ajusteEmpaques = new AjusteMapper().CrearAjusteEmpaqueList(listaAjustes, idCreador);
                        foreach (var item in ajusteEmpaques)
                        {
                            AjusteEmpaque ajusteEmpaque = item;
                            CompraEmpaques compraEmpaque = await _context.CompraEmpaques
                            .Where(w => w.Id == ajusteEmpaque.IdCompraEmpaque)
                            .FirstOrDefaultAsync() ?? throw new BadRequestException("compraEmpaques no encontrada");
                            if (compraEmpaque.StockDisponible == null) throw new BadRequestException("el stock disponible es null, tiene que ser 0");
                            else
                            {
                                compraEmpaque.StockDisponible += ajusteEmpaque.Ajuste;
                            }
                            _context.AjusteEmpaques.Add(ajusteEmpaque);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                throw new BadRequestException("Familia no apta");
            }
        }
    }
}
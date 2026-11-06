using proy_back_Qbd.Models.ProductoIntermedio;
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
using Proy_back_QBD.Service.AjusteService.Familias;

namespace Proy_back_QBD.Service.AjusteService
{
    /// <summary>
    /// Ajustes de inventario. El trabajo de cada familia lo hace su estrategia
    /// (IAjusteFamiliaStrategy); este servicio elige la estrategia y maneja la transacción.
    /// </summary>
    public class AjusteService : IAjusteService
    {
        private readonly ApiContext _context;
        private readonly IReadOnlyDictionary<string, IAjusteFamiliaStrategy> _estrategias;

        public AjusteService(ApiContext context, IEnumerable<IAjusteFamiliaStrategy> estrategias)
        {
            _context = context;
            _estrategias = estrategias.ToDictionary(e => e.Familia);
        }

        private IAjusteFamiliaStrategy Estrategia(string? familia, string mensaje)
            => familia != null && _estrategias.TryGetValue(familia, out var e) ? e : throw new BadRequestException(mensaje);

        public async Task<List<TablaAjustesRes>> ListaAjustes(string familia, int idSede)
        {
            if (idSede <= 0)
            {
                var centralSede = await _context.Sedes.FirstOrDefaultAsync(s => s.Nombre.ToUpper().Contains("CENTRAL") || s.Id == 15);
                idSede = centralSede?.Id ?? 15;
            }

            return await Estrategia(familia, "Familia no Apta").Listar(idSede);
        }

        public async Task RegistrarAjuste(CrearAjusteReq request)
        {
            var estrategia = Estrategia(request.Familia, "Familia no apta");
            int targetSede = request.IdSede.HasValue && request.IdSede.Value > 0 ? request.IdSede.Value : 15;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await estrategia.Registrar(request.ListaAjustes, request.IdCreador, targetSede);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                throw new ServerException($"Ocurrió un error al crear el ajuste: {e.Message}", e);
            }
        }

        public Task<List<DetalleAjusteRes>> DetalleAjuste(int registroId, string familia, int? idInsumo = null, int? idSede = null)
            => Estrategia(familia, "Familia no apta").Detalle(registroId, idInsumo, idSede);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface IMesonService
    {
        Task<List<MesonListaRes>> ListarMeson(string[] cadena);
        Task<MesonDetalleRes> ObtenerDetalleOrdenOCompra(int id);
        Task<MesonModalRes?> ObtenerDatosModal(int ordenCompraId);
        Task<MesonListaRes?> CompletarDatos(int ordenCompraId, MesonConvertirReq request);
    }
}
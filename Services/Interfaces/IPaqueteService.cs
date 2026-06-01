using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models;
using proy_back_Qbd.Util;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface IPaqueteService
    {
        Task<PaqueteInsumoDetalleRes> GetDetallePaquetes(int idCompra);
        Task<int> CrearPaqueteInsumo(PaqueteInsumoCrearReq req);
        Task<int> CrearPaqueteEmpaque(PaqueteEmpaqueCrearReq req);
        Task<string> ModificarPaqueteEmpaque(int idPaquete, PaqueteEmpaqueModificarReq req);
        Task<string> ModificarPaqueteInsumo(int idInsumo, PaqueteInsumoModificarReq req);
        Task<string> EliminarPaquete(int idPaquete);
    }
}
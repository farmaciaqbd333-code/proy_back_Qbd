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
        Task<DetallePaqueteRes?> ObtenerDetallePaquete(int compraLabId);
        Task<int> CrearPaquete(CrearPaqueteReq req);
        Task<bool> ModificarPaquete(int idPaquete, ModificarPaqueteReq req);
        Task<bool> EliminarPaquete(int idPaquete);
    }
}
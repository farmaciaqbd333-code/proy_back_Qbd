using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface ICompraLaboratorioService
    {
        Task<ObtenerCompraLabRes?> DatosCompraLaboratorio(int IdCompra);
        Task<ObtenerCompraLab2Res> DetalleCompraLaboratorio(int IdCompra);
        Task<int?> ActualizarDetalleLab(int idCompra, List<ActualizarDetCompraLabReq> request);
    }
}
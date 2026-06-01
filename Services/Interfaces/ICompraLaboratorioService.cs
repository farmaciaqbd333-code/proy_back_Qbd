using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface ICompraLaboratorioService
    {
        // Task<ObtenerCompraLabRes?> GetCompraLab(int IdCompra);
        Task<List<LabListaRes>> Listar(string[] cadena);
        Task<CompraLabIdRes> GetDetalleCompraLab(int IdCompra);
        Task<int> UpdateDetalleLab(int idCompra, List<ActualizarDetCompraLabReq> request);
        Task<EtiquetaCompra> GetEtiquetaCompraInsumo(int IdCompra);
        Task<EtiquetaCompra> GetEtiquetaCompraEmpaque(int IdCompra);
    }
}
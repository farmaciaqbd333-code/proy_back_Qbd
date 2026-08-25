using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models;
using proy_back_Qbd.Models.DetalleCompraLab;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface ICompraLaboratorioService
    {
        Task<ObtenerCompraLabRes> ModalPaquetes(int IdCompra);
        Task<List<LabListaRes>> Listar(string[] cadena, int idSede);
        Task<CompraLabDetIdRes> GetDetalleCompraLab(int IdCompra);
        Task UpdateDetalleLab(int idCompra, ActualizarDetCompraLabReq request);
        Task<EtiquetaCompra> GetEtiquetaCompraInsumo(int IdCompra);
        Task<EtiquetaCompra> GetEtiquetaCompraEmpaque(int IdCompra);
    }
}
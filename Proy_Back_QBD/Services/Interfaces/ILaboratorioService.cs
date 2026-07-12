using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Proy_back_QBD.Dto.Productos;

namespace Proy_back_QBD.Services.Interfaces
{
    public interface ILaboratorioService
    {
        Task<List<PedidoLab>> ListaLab(int sedeId);
        Task<LabFindPedIdRes?> ObtenerByCod(string cod, int sedeId);
        Task<string> EditarElaborado(int labId, int sedeId, int idElaborado);
        Task<string?> RegistrarLabIns(FormLabIns request);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface ICompraService
    {
        Task<List<OrdenesEnviadasRes>> ListaOrdenesEnviadas(string cadena);
    }
}
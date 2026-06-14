using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proy_back_Qbd.Models.Stock;

namespace proy_back_Qbd.Services.Interfaces
{
    public interface IStockService
    {
        public Task<StockGetRes> StockListaPrincipal();
    }
}
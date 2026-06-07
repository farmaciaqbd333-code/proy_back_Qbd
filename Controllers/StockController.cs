using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models.Stock;
using proy_back_Qbd.Services.Interfaces;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    public class StockController : Controller
    {
        private readonly IStockService _service;

        public StockController(IStockService _service)
        {
            this._service = _service;
        }

        /// <summary>
        /// Listar Principal
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerStock()
        {
            StockGetRes response = await _service.StockListaPrincipal();

            return Ok(response);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private readonly ApiContext _context;
        public ComprasController(ApiContext context)
        {
            _context = context;
        }

        [HttpPost()]
        public async Task<ActionResult> CrearCompra(CompraCreateReq request)
        {
            Compra compra = new()
            {
                CodFactura = request.CodFactura,
                Guia = request.Guia,
                CodFacturaQBD = request.CodFacturaQBD,
                IdOrdenCompra = request.IdOrdenCompra,
                IdUsuario = request.IdUsuario,
                FechaFactura = request.FechaFactura,
                ImgFactura = request.ImgFactura,
            };
            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();

            if (compra.IdCompra != null)
            {
                List<DetalleCompra> detallesCompra = request.Detalle.Select(s => new DetalleCompra
                {                    
                    Lote = s.Lote,
                    FechaFab = s.FechaFab,
                    FechaVec = s.FechaVec,
                    Coa = s.Coa,
                    RegistroSanitario = s.RegistroSanitario,
                    Conformidad = s.Conformidad,
                    IdCreador = s.IdCreador,

                }).ToList();
            }

            return Ok();
        }
    }
}
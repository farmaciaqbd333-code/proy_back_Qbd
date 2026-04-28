using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpPost]
        public async Task<ActionResult> CrearCompra(CompraCreateReq request)
        {
            Compra compra = new()
            {
                CodFactura = request.CodFactura,
                Guia = request.Guia,
                CodFacturaQBD = request.CodFacturaQBD,
                Id = request.IdOrdenCompra,
                IdCreador = request.IdUsuario,
                IdModificador = request.IdUsuario,
                FechaFactura = request.FechaFactura,
                ImgFactura = request.ImgFactura,
            };
            _context.Compras.Add(compra);
            await _context.SaveChangesAsync();

            if (compra.Id != null)
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
        [HttpGet]
        public async Task<ActionResult<List<CompraGetRes>>> ListarCompra()
        {
            List<CompraGetRes> lista = await _context.Compras.Select(s => new CompraGetRes
            {
                IdCompra = s.Id,
                CodCompra = "BDCO-" + s.Id,
                FechaCreacion = s.FechaCreacion.ToString(),
                CodFactura = s.CodFactura,
                Guia = s.Guia,
                CodFacturaQBD = s.CodFacturaQBD,
                RUC = s.OrdenCompra.Proveedor.CodigoProv,
                Proveedor = s.OrdenCompra.Proveedor.Datos,
                Estado = "LABORATORIO",
                OrdenCompra = "OC01-" + s.OrdenCompra.Id,
                Usuario = s.Creador.Persona.NombreCompleto,
                Familia = s.OrdenCompra.Familia.Nombre,
            }).ToListAsync();

            return lista;
        }
        // [HttpPatch]
        // public async Task<ActionResult> EditarDetalleCompra()
        // {
            
        // }
    }
}
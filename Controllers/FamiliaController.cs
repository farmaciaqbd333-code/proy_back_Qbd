using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proy_back_Qbd.Models;
using Proy_back_QBD.Data;

namespace proy_back_Qbd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FamiliaController : ControllerBase
    {
        private readonly ApiContext _context;

        public FamiliaController(ApiContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Familia>>> GetFamilias()
        {
            return await _context.Familias
                .OrderBy(f => f.Nombre)
                .ToListAsync();
        }
    }
}

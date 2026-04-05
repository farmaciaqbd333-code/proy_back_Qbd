using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        
    }
}
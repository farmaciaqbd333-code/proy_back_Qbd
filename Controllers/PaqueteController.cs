using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace proy_back_Qbd.Controllers
{
    [Route("[controller]")]
    public class PaqueteController : Controller
    {
        private readonly ILogger<PaqueteController> _logger;

        public PaqueteController(ILogger<PaqueteController> logger)
        {
            _logger = logger;
        }

    //     public IActionResult Index()
    //     {
    //         return View();
    //     }

    //     [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    //     public IActionResult Error()
    //     {
    //         return View("Error!");
    //     }
    }
}
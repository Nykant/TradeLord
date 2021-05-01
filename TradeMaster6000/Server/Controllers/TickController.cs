
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradeMaster6000.Server.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class TickController : ControllerBase
    {
        private readonly ILogger<TickController> logger;

        public TickController(ILogger<TickController> logger, IConfiguration configuration)
        {
            this.logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        //[HttpPost]

    }
}
